// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;

namespace Sphere10.Framework.Windows;

/// <summary>Loads owned WAV streams silently, caches their data, and plays one sound at a time on the calling thread.</summary>
/// <remarks>
/// Invoke Play from a worker when the caller must remain responsive. Windows PlaySound is process-wide;
/// other users of that native API can replace playback. Stop is best effort once native playback is being
/// admitted; Dispose always waits for the active Play call to return before releasing cached data.
/// Use stable immutable keys and a new key when the corresponding WAV changes.
/// </remarks>
public class WaveSoundPlayer : IWaveSoundPlayer {
	private readonly SynchronizedObject _playSync = new();
	private readonly SynchronizedObject _stateSync = new();
	private readonly ActionCache<object, byte[]> _waves;
	private readonly int _cacheCapacity;
	private readonly int _maximumWaveBytes;
	private Func<Stream> _loadingFactory;
	private long _stopGeneration;
	private bool _playing;
	private bool _disposed;
	private bool _cacheDisposed;

	/// <summary>Creates a cache bounded by entry count, retained WAV bytes, and the size of each individual WAV.</summary>
	public WaveSoundPlayer(int cacheCapacity = 6, long maximumCachedBytes = 4 * 1024 * 1024, int maximumWaveBytes = 1024 * 1024) {
		Guard.ArgumentGT(cacheCapacity, 0, nameof(cacheCapacity));
		Guard.ArgumentGT(maximumCachedBytes, 0, nameof(maximumCachedBytes));
		Guard.ArgumentGT(maximumWaveBytes, 0, nameof(maximumWaveBytes));
		Guard.Argument(maximumWaveBytes <= maximumCachedBytes, nameof(maximumWaveBytes), "An individual WAV must fit inside the cache byte budget.");
		_cacheCapacity = cacheCapacity;
		_maximumWaveBytes = maximumWaveBytes;
		_waves = new ActionCache<object, byte[]>(
			_ => LoadWave(),
			sizeEstimator: wave => wave.LongLength,
			reapStrategy: CacheReapPolicy.LongestIdle,
			maxCapacity: maximumCachedBytes);
	}

	public void Play(object key, Func<Stream> streamFactory) {
		Guard.ArgumentNotNull(key, nameof(key));
		Guard.ArgumentNotNull(streamFactory, nameof(streamFactory));
		using var playScope = _playSync.EnterWriteScope();
		long generation;
		using (_stateSync.EnterReadScope()) {
			ObjectDisposedException.ThrowIf(_disposed, this);
			generation = _stopGeneration;
		}
		byte[] wave;
		_loadingFactory = streamFactory;
		using (Tools.Scope.ExecuteOnDispose(() => _loadingFactory = null))
			wave = _waves[key];
		using (_stateSync.EnterWriteScope()) {
			if (_disposed || generation != _stopGeneration)
				return;
			_playing = true;
		}
		using var endPlayback = Tools.Scope.ExecuteOnDispose(() => {
			using (_stateSync.EnterWriteScope())
				_playing = false;
		});
		// Keep state scopes out of blocking native playback so another thread can stop it.
		PlayWave(wave);
	}

	public void Stop() {
		bool stop;
		using (_stateSync.EnterWriteScope()) {
			_stopGeneration++;
			stop = _playing;
		}
		if (stop)
			StopWave();
	}

	public void Dispose() {
		using var releaseCache = Tools.Scope.ExecuteOnDispose(ReleaseCache);
		bool stop;
		using (_stateSync.EnterWriteScope()) {
			if (_disposed)
				return;
			_disposed = true;
			_stopGeneration++;
			stop = _playing;
		}
		if (stop)
			StopWave();
	}

	/// <summary>Executes synchronous native playback. Overrides can supply a controlled driver without opening an audio device.</summary>
	protected virtual void PlayWave(byte[] waveData) {
		var flags = WinAPI.WINMM.SoundFlags.SND_SYNC | WinAPI.WINMM.SoundFlags.SND_MEMORY | WinAPI.WINMM.SoundFlags.SND_NODEFAULT;
		Guard.Ensure(WinAPI.WINMM.PlaySound(waveData, UIntPtr.Zero, (uint)flags), "Windows could not play the WAV sound.");
	}

	/// <summary>Stops the native playback driver. It must be callable from a different thread than PlayWave.</summary>
	protected virtual void StopWave()
		=> WinAPI.WINMM.PlaySound((byte[])null, UIntPtr.Zero, 0);

	private byte[] LoadWave() {
		using var stream = _loadingFactory();
		Guard.Ensure(stream != null, "The WAV stream factory returned no stream.");
		if (stream.CanSeek && stream.Length - stream.Position > _maximumWaveBytes)
			throw new InvalidDataException("The WAV sound exceeds the configured per-sound byte limit.");
		using var contents = new MemoryStream();
		using var boundedContents = new BoundedStream(contents, 0, _maximumWaveBytes) { AllowInnerResize = false };
		try {
			stream.CopyTo(boundedContents, 8192);
		} catch (StreamOutOfBoundsException error) {
			throw new InvalidDataException("The WAV sound exceeds the configured per-sound byte limit.", error);
		}
		var wave = contents.ToArray();
		ValidateWave(wave);
		if (_waves.ItemCount >= _cacheCapacity)
			_waves.Reap(1);
		return wave;
	}

	private void ReleaseCache() {
		// Every disposing caller joins playback, but the owned cache is released only once.
		using (_playSync.EnterWriteScope()) {
			if (_cacheDisposed)
				return;
			_cacheDisposed = true;
			_waves.Dispose();
		}
	}

	private static void ValidateWave(byte[] wave) {
		if (wave.Length < 12 || !wave.AsSpan(0, 4).SequenceEqual("RIFF"u8) || !wave.AsSpan(8, 4).SequenceEqual("WAVE"u8))
			throw new InvalidDataException("The sound must contain a RIFF/WAVE header.");
		var end = 8L + EndianBitConverter.Little.ToUInt32(wave, 4);
		if (end < 12 || end > wave.Length)
			throw new InvalidDataException("The WAV sound contains a truncated RIFF payload.");
		var hasFormat = false;
		var hasData = false;
		for (var position = 12L; position + 8 <= end;) {
			var offset = checked((int)position);
			var length = EndianBitConverter.Little.ToUInt32(wave, offset + 4);
			var chunkEnd = position + 8 + length;
			if (chunkEnd > end)
				throw new InvalidDataException("The WAV sound contains a truncated chunk.");
			if (wave.AsSpan(offset, 4).SequenceEqual("fmt "u8))
				hasFormat |= length >= 16;
			else if (wave.AsSpan(offset, 4).SequenceEqual("data"u8))
				hasData |= length != 0;
			position = chunkEnd + (length & 1);
		}
		if (!hasFormat || !hasData)
			throw new InvalidDataException("The WAV sound requires a format chunk and nonempty audio data.");
	}
}
