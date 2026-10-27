// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Sphere10.Framework;

/// <summary>Connects synchronous stream filters through blocking intermediate streams.</summary>
/// <remarks>
/// Earlier filters run on background threads; the final filter runs on the thread calling <see cref="Run"/>.
/// Intermediate streams belong to the pipeline. The pipeline itself does not close the caller's input or output.
/// Filters must finish their writes and finalize any buffering wrappers before returning.
/// </remarks>
public class StreamPipeline : IDisposable {
	private readonly ManualResetEvent _lastStagedFinished;
	private readonly Action<Stream, Stream>[] _filters;
	private List<BlockingStream> _blockingStreams;

	/// <summary>Creates the channels connecting an ordered sequence of stream filters.</summary>
	/// <param name="filters">Filters that read their first stream and write to their second stream.</param>
	/// <exception cref="ArgumentNullException"><paramref name="filters"/> is null.</exception>
	/// <exception cref="ArgumentException">The filter array is empty or contains a null delegate.</exception>
	/// <remarks>The array is retained rather than copied; do not change its entries while the pipeline is in use.</remarks>
	public StreamPipeline(params Action<Stream, Stream>[] filters) {
		if (filters == null) throw new ArgumentNullException(nameof(filters));
		if (filters.Length == 0 || Array.IndexOf(filters, null) >= 0)
			throw new ArgumentException(nameof(filters));

		_filters = filters;

		// One owned channel connects each adjacent pair; the first input and final output are supplied to Run.
		_blockingStreams = new List<BlockingStream>(_filters.Length - 1);
		for (var i = 0; i < filters.Length - 1; i++) {
			_blockingStreams.Add(new BlockingStream());
		}
		_lastStagedFinished = new ManualResetEvent(false);
	}

	/// <summary>Starts the preceding filters and invokes the final filter on the calling thread.</summary>
	/// <param name="input">The readable stream supplied to the first filter.</param>
	/// <param name="output">The writable stream supplied to the final filter.</param>
	/// <remarks>
	/// <para>
	/// Filters receive the streams at their current positions. The pipeline does not seek, reset their positions,
	/// truncate the output or flush either endpoint. Each filter is responsible for completing its own buffered output.
	/// Intermediate <see cref="BlockingStream"/> channels do not support seeking, length or position operations;
	/// their <see cref="Stream.Flush"/> operation does nothing.
	/// </para>
	/// <para>
	/// Returning from a filter signals end-of-stream on its output when that output is a <see cref="BlockingStream"/>,
	/// including a caller-supplied final output. A failed filter also signals completion so downstream readers can finish.
	/// Stage exceptions are collected separately and the errors observed at the final check are aggregated.
	/// </para>
	/// <para>
	/// Background threads are not joined, and a filter that returns without draining its input can leave earlier stages running.
	/// The channels created by the constructor are not reset by this method. There is no cancellation or concurrent-run coordination.
	/// </para>
	/// </remarks>
	/// <exception cref="ObjectDisposedException">The pipeline has been disposed.</exception>
	/// <exception cref="ArgumentNullException"><paramref name="input"/> or <paramref name="output"/> is null.</exception>
	/// <exception cref="ArgumentException"><paramref name="input"/> is not readable or <paramref name="output"/> is not writable.</exception>
	/// <exception cref="AggregateException">One or more stage errors have been observed.</exception>
	public void Run(Stream input, Stream output) {
		ObjectDisposedException.ThrowIf(_blockingStreams == null, this);
		if (input == null) throw new ArgumentNullException(nameof(input));
		if (!input.CanRead) throw new ArgumentException(nameof(input));
		if (output == null) throw new ArgumentNullException(nameof(output));
		if (!output.CanWrite) throw new ArgumentException(nameof(output));

		var errors = new SynchronizedList<Exception>();
		ThreadStart lastStage = null;
		for (var i = 0; i < _filters.Length; i++) {
			var stageInput = i == 0 ? input : _blockingStreams[i - 1];
			var stageOutput =
				i == _filters.Length - 1 ? output : _blockingStreams[i];
			var filter = _filters[i];

			void Stage() {
				try {
					filter(stageInput, stageOutput);
					// Readers drain the queued bytes before seeing end-of-stream after the producer returns.
					if (stageOutput is BlockingStream blockingStream)
						blockingStream.SetEndOfStream();
				} catch (Exception error) {
					// Publish the error separately and wake downstream readers even when this producer failed.
					errors.Add(error);
					if (stageOutput is BlockingStream blockingStream)
						blockingStream.SetEndOfStreamDueToFailure();
				}
			}

			// Only preceding stages get worker threads; the caller executes the final stage below.
			if (i < _filters.Length - 1) {
				var t = new Thread(Stage) { IsBackground = true };
				t.Start();
			} else lastStage = Stage;
		}
		// A worker may already have failed during startup; in that case the final stage is skipped.
		if (!errors.Any()) {
			try {
				lastStage();
			} catch (Exception error) {
				errors.Add(error);
			}
		}
		// This is an error snapshot, not a join of background stages that have not finished.
		if (errors.Any()) {
			throw new AggregateException(errors);
		}
	}

	/// <summary>Disposes the intermediate channels and prevents subsequent calls to <see cref="Run"/>.</summary>
	/// <remarks>
	/// Caller-supplied endpoints are not disposed. This method does not join filter threads; callers must arrange
	/// for filters to finish using the channels before disposing the pipeline. Repeated calls after successful disposal do nothing.
	/// </remarks>
	public void Dispose() {
		if (_blockingStreams == null) return;
		// Release the owned connectors only; filter delegates and caller streams retain their own lifetimes.
		foreach (var stream in _blockingStreams) {
			stream.Dispose();
		}
		_blockingStreams = null;
	}
}

