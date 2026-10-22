// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// Allows the the serial execution of Actions in the order they are added. The background execution thread is not released until all the queued actions are processed.
/// </summary>
public class SerialThreadPool {

	private readonly SynchronizedQueue<Action> _inputQueue;
	private readonly Queue<Action> _executionQueue;
	private readonly Action<Exception> _errorHandler;
	private readonly Action<Action> _queueAction;
	private bool _processing;
	public SerialThreadPool(Action<Exception> errorHandler = null) : this(new QueueAdapter<Action>(), errorHandler) {
	}

	public SerialThreadPool(IQueue<Action> inputQueueImpl, Action<Exception> errorHandler = null)
		: this(inputQueueImpl, errorHandler, Tools.Threads.QueueAction) {
	}

	internal SerialThreadPool(IQueue<Action> inputQueueImpl, Action<Exception> errorHandler, Action<Action> queueAction) {
		Guard.ArgumentNotNull(queueAction, nameof(queueAction));
		_inputQueue = new SynchronizedQueue<Action>(inputQueueImpl);
		_executionQueue = new Queue<Action>();
		_errorHandler = errorHandler ?? (_ => { });
		_queueAction = queueAction;
	}

	public SerialThreadPoolPolicy Policy { get; init; } = SerialThreadPoolPolicy.Burst;

	public static SerialThreadPool Global { get; } = new(error => SystemLog.Exception(error));


	public void QueueUserWorkItem(Action action) {
		using (_inputQueue.EnterWriteScope()) {
			// Enqueue and claim the sole drainer atomically, including while its current callback is running.
			_inputQueue.Add(action);
			if (!_processing)
				ScheduleProcessing();
		}
	}

	private void ProcessInBurst() {
		while (true) {
			// Publish the idle transition under the same scope as enqueue, so the next producer cannot lose its scheduling credit.
			using (_inputQueue.EnterWriteScope()) {
				if (_executionQueue.Count == 0) {
					if (_inputQueue.Count == 0) {
						_processing = false;
						return;
					}
					foreach (var action in _inputQueue)
						_executionQueue.Enqueue(action);
					_inputQueue.Clear();
				}
			}

			// Callbacks run outside the input scope; producers only append to the next batch.
			while (_executionQueue.Count > 0) {
				var action = _executionQueue.Dequeue();
				try {
					action();
				} catch (Exception error) {
					try {
						_errorHandler(error);
					} catch {
						// Preserve the handler failure without stranding the remaining batch or retaining a false running state.
						using (_inputQueue.EnterWriteScope()) {
							_processing = false;
							if (_executionQueue.Count > 0 || _inputQueue.Count > 0)
								ScheduleProcessing();
						}
						throw;
					}
				}
			}
		}
	}

	private void ScheduleProcessing() {
		_processing = true;
		try {
			_queueAction(
				Policy switch {
					SerialThreadPoolPolicy.Burst => ProcessInBurst,
					_ => throw new NotImplementedException($"{Policy}")
				}
			);
		} catch {
			_processing = false;
			throw;
		}
	}
}

