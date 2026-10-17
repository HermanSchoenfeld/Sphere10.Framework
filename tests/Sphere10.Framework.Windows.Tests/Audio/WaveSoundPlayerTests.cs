// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WaveSoundPlayerTests {
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

	[Test]
	public void LoadsOncePerKeyAndDisposesTheOwnedStreamWithoutStoppingAudio() {
		using var player = new RecordingWaveSoundPlayer();
		var wave = CreateWave();
		using var stream = new TrackingStream(new byte[] { 1, 2 }.Concat(wave).ToArray());
		stream.Position = 2;
		var loads = 0;
		player.Play("first", () => { loads++; return stream; });
		player.Play(new string("first".ToCharArray()), () => { loads++; return new MemoryStream(wave); });
		Assert.That(loads, Is.EqualTo(1));
		Assert.That(stream.DisposeCalls, Is.EqualTo(1), "Factory streams transfer ownership and are closed after their WAV data has been loaded.");
		Assert.That(player.Played.Count, Is.EqualTo(2));
		Assert.That(player.Played, Is.All.EqualTo(wave));
		Assert.That(player.StopCalls, Is.Zero, "Loading and cache hits must not stop process-wide audio.");
	}

	[Test]
	public void EvictsTheLeastRecentlyUsedKeyAtTheEntryLimit() {
		using var player = new RecordingWaveSoundPlayer(cacheCapacity: 2);
		var loads = new ConcurrentDictionary<string, int>();
		void Play(string key) => player.Play(key, () => {
			loads.AddOrUpdate(key, 1, (_, count) => count + 1);
			return new MemoryStream(CreateWave());
		});
		Play("a");
		Play("b");
		Play("a");
		Play("c");
		Play("a");
		Play("b");
		Assert.That(loads["a"], Is.EqualTo(1));
		Assert.That(loads["b"], Is.EqualTo(2));
		Assert.That(loads["c"], Is.EqualTo(1));
	}

	[Test]
	public void RetainedByteBudgetCanEvictBeforeTheEntryLimit() {
		var wave = CreateWave(dataLength: 20);
		using var player = new RecordingWaveSoundPlayer(maximumCachedBytes: 2 * wave.Length, maximumWaveBytes: wave.Length);
		var loads = new ConcurrentDictionary<string, int>();
		void Play(string key) => player.Play(key, () => {
			loads.AddOrUpdate(key, 1, (_, count) => count + 1);
			return new MemoryStream(wave);
		});
		Play("a");
		Play("b");
		Play("a");
		Play("c");
		Play("a");
		Play("b");
		Assert.That(loads["a"], Is.EqualTo(1));
		Assert.That(loads["b"], Is.EqualTo(2));
		Assert.That(loads["c"], Is.EqualTo(1));
	}

	[Test]
	public void KnownOversizeStreamIsRejectedBeforeCopyingAndIsDisposed() {
		using var player = new RecordingWaveSoundPlayer(maximumCachedBytes: 256, maximumWaveBytes: 100);
		using var stream = new TrackingStream(CreateWave(dataLength: 100));
		Assert.That(() => player.Play("large", () => stream), Throws.TypeOf<InvalidDataException>());
		Assert.That(stream.CopyCalls, Is.Zero);
		Assert.That(stream.DisposeCalls, Is.EqualTo(1));
		Assert.That(player.Played, Is.Empty);
	}

	[Test]
	public void NonseekableShortReadsAreBoundedAndDisposeTheFactoryStream() {
		using var player = new RecordingWaveSoundPlayer(maximumCachedBytes: 256, maximumWaveBytes: 100);
		using var stream = new ShortReadStream(CreateWave(dataLength: 1000));
		Assert.That(() => player.Play("large", () => stream), Throws.TypeOf<InvalidDataException>());
		Assert.That(stream.BytesRead, Is.LessThanOrEqualTo(117), "The read must stop after discovering one short block beyond the limit.");
		Assert.That(stream.DisposeCalls, Is.EqualTo(1));
		Assert.That(player.Played, Is.Empty);
	}

	[Test]
	public void NonseekableShortReadsLoadACompleteWave() {
		using var player = new RecordingWaveSoundPlayer();
		var wave = CreateWave();
		using var stream = new ShortReadStream(wave);
		player.Play("short", () => stream);
		Assert.That(player.Played.Single(), Is.EqualTo(wave));
		Assert.That(stream.BytesRead, Is.EqualTo(wave.Length));
		Assert.That(stream.DisposeCalls, Is.EqualTo(1));
	}

	[Test]
	public void InvalidWaveDisposesItsStreamAndDoesNotPoisonTheCacheKey() {
		using var player = new RecordingWaveSoundPlayer();
		using var invalid = new TrackingStream(new byte[44]);
		Assert.That(() => player.Play("retry", () => invalid), Throws.TypeOf<InvalidDataException>());
		Assert.That(invalid.DisposeCalls, Is.EqualTo(1));
		var valid = CreateWave();
		player.Play("retry", () => new MemoryStream(valid));
		Assert.That(player.Played.Single(), Is.EqualTo(valid));
	}

	[Test]
	public void FactoryAndPlaybackFailuresPropagateWithoutLeavingThePlayerBusy() {
		using var player = new RecordingWaveSoundPlayer();
		Assert.That(() => player.Play("retry", () => throw new IOException("Factory failed.")), Throws.TypeOf<IOException>());
		player.FailNextPlayback = true;
		Assert.That(() => player.Play("retry", () => new MemoryStream(CreateWave())), Throws.TypeOf<InvalidOperationException>());
		player.Play("retry", () => throw new IOException("A loaded key must remain cached."));
		Assert.That(player.Played.Count, Is.EqualTo(2));
		Assert.That(player.StopCalls, Is.Zero);
	}

	[Test]
	public async Task StopFromAnotherThreadReleasesPlaybackAndAllowsLaterPlayback() {
		using var player = new RecordingWaveSoundPlayer(blockPlayback: true);
		var play = Task.Run(() => player.Play("blocking", () => new MemoryStream(CreateWave())));
		await player.Started.Task.WaitAsync(Timeout);
		await Task.Run(player.Stop).WaitAsync(Timeout);
		await play.WaitAsync(Timeout);
		player.Play("blocking", () => throw new IOException("The cached sound should survive Stop."));
		Assert.That(player.Played.Count, Is.EqualTo(2));
		Assert.That(player.StopCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task DisposeStopsAndJoinsPlaybackBeforeRejectingNewPlayCalls() {
		var returned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var player = new RecordingWaveSoundPlayer(blockPlayback: true, waitForManagedReturn: returned.Task);
		var play = Task.Run(() => {
			player.Play("blocking", () => new MemoryStream(CreateWave()));
			returned.SetResult();
		});
		await player.Started.Task.WaitAsync(Timeout);
		await Task.Run(player.Dispose).WaitAsync(Timeout);
		await play.WaitAsync(Timeout);
		Assert.That(returned.Task.IsCompletedSuccessfully, Is.True, "The stop driver can wait for managed Play cleanup without a state-lock deadlock.");
		Assert.That(player.StopCalls, Is.EqualTo(1));
		player.Dispose();
		Assert.That(() => player.Play("late", () => new MemoryStream(CreateWave())), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(player.StopCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task StopDuringLoadingPreventsPlaybackFromStarting() {
		using var player = new RecordingWaveSoundPlayer();
		var loading = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var unblock = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		using var stream = new TrackingStream(CreateWave());
		var play = Task.Run(() => player.Play("loading", () => {
			loading.SetResult();
			release.Task.WaitAsync(Timeout).GetAwaiter().GetResult();
			return stream;
		}));
		await loading.Task.WaitAsync(Timeout);
		player.Stop();
		release.SetResult();
		await play.WaitAsync(Timeout);
		Assert.That(player.Played, Is.Empty);
		Assert.That(player.StopCalls, Is.Zero, "A loading stream has not opened the playback driver.");
		Assert.That(stream.DisposeCalls, Is.EqualTo(1));
		player.Play("loading", () => throw new IOException("Loaded data should survive cancellation."));
		Assert.That(player.Played.Count, Is.EqualTo(1));
	}

	[Test]
	public async Task ConcurrentPlayCallsUseOnlyOnePlaybackDriverAtATime() {
		using var player = new RecordingWaveSoundPlayer(blockPlayback: true);
		var first = Task.Run(() => player.Play("first", () => new MemoryStream(CreateWave())));
		await player.Started.Task.WaitAsync(Timeout);
		var second = Task.Run(() => player.Play("second", () => new MemoryStream(CreateWave())));
		player.AllowPlayback();
		await Task.WhenAll(first, second).WaitAsync(Timeout);
		Assert.That(player.Played.Count, Is.EqualTo(2));
		Assert.That(player.MaximumConcurrency, Is.EqualTo(1));
	}

	private static byte[] CreateWave(int dataLength = 32) {
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
		writer.Write("RIFF"u8);
		writer.Write(36 + dataLength);
		writer.Write("WAVEfmt "u8);
		writer.Write(16);
		writer.Write((ushort)1);
		writer.Write((ushort)1);
		writer.Write(44100);
		writer.Write(88200);
		writer.Write((ushort)2);
		writer.Write((ushort)16);
		writer.Write("data"u8);
		writer.Write(dataLength);
		writer.Write(new byte[dataLength]);
		return stream.ToArray();
	}

	private sealed class RecordingWaveSoundPlayer : WaveSoundPlayer {
		private readonly bool _blockPlayback;
		private readonly Task _waitForManagedReturn;
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _active;
		private int _maximumConcurrency;
		private int _stopCalls;

		public RecordingWaveSoundPlayer(int cacheCapacity = 6, long maximumCachedBytes = 4 * 1024 * 1024, int maximumWaveBytes = 1024 * 1024,
			bool blockPlayback = false, Task waitForManagedReturn = null)
			: base(cacheCapacity, maximumCachedBytes, maximumWaveBytes) {
			_blockPlayback = blockPlayback;
			_waitForManagedReturn = waitForManagedReturn;
		}

		public ConcurrentQueue<byte[]> Played { get; } = new();

		public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public bool FailNextPlayback { get; set; }

		public int StopCalls => Volatile.Read(ref _stopCalls);

		public int MaximumConcurrency => Volatile.Read(ref _maximumConcurrency);

		public void AllowPlayback() => _release.TrySetResult();

		protected override void PlayWave(byte[] waveData) {
			var active = Interlocked.Increment(ref _active);
			using var finished = Tools.Scope.ExecuteOnDispose(() => Interlocked.Decrement(ref _active));
			var maximum = Volatile.Read(ref _maximumConcurrency);
			while (active > maximum) {
				var observed = Interlocked.CompareExchange(ref _maximumConcurrency, active, maximum);
				if (observed == maximum)
					break;
				maximum = observed;
			}
			Played.Enqueue(waveData);
			Started.TrySetResult();
			if (FailNextPlayback) {
				FailNextPlayback = false;
				throw new InvalidOperationException("Playback failed.");
			}
			if (_blockPlayback)
				_release.Task.WaitAsync(Timeout).GetAwaiter().GetResult();
		}

		protected override void StopWave() {
			Interlocked.Increment(ref _stopCalls);
			AllowPlayback();
			_waitForManagedReturn?.WaitAsync(Timeout).GetAwaiter().GetResult();
		}
	}

	private sealed class TrackingStream : MemoryStream {
		public TrackingStream(byte[] data) : base(data) { }

		public int DisposeCalls { get; private set; }

		public int CopyCalls { get; private set; }

		public override void CopyTo(Stream destination, int bufferSize) {
			CopyCalls++;
			base.CopyTo(destination, bufferSize);
		}

		protected override void Dispose(bool disposing) {
			DisposeCalls++;
			base.Dispose(disposing);
		}
	}

	private sealed class ShortReadStream : Stream {
		private readonly MemoryStream _source;

		public ShortReadStream(byte[] data) => _source = new MemoryStream(data);

		public int DisposeCalls { get; private set; }

		public int BytesRead { get; private set; }

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

		public override int Read(byte[] buffer, int offset, int count) {
			var read = _source.Read(buffer, offset, Math.Min(count, 17));
			BytesRead += read;
			return read;
		}

		public override void Flush() => throw new NotSupportedException();
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

		protected override void Dispose(bool disposing) {
			DisposeCalls++;
			if (disposing)
				_source.Dispose();
			base.Dispose(disposing);
		}
	}
}
