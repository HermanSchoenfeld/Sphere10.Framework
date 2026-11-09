# Audio

Plays WAV sound data in memory through the Windows `PlaySound` API.

`WaveSoundPlayer` is the current entry point. It implements `IWaveSoundPlayer`, caches WAV bytes by key within entry-count, total-byte and per-file limits, and plays one sound at a time on the calling thread. `Play` blocks until playback ends, so call it from a worker when the caller must stay responsive. `PlaySound` is process-wide, so other users of that API in the same process can replace playback. `Stop` is best effort, and `Dispose` waits for an active `Play` call before releasing the cache. Use a stable key per WAV and a new key when the data changes.

`SoundPlayerEx` is the older wrapper. It loads one stream into a byte array and plays it synchronously, or asynchronously from a pinned buffer that stays pinned until the next play or `Dispose`. The native declarations live in `WinAPI.WINMM` in the [WinAPI](../WinAPI/README.md) domain.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`IWaveSoundPlayer`](IWaveSoundPlayer.cs) | interface | Contract for a disposable player with a bounded WAV cache: `Play(key, streamFactory)` blocks until playback ends and `Stop` cancels it |
| [`SoundPlayerEx`](SoundPlayerEx.cs) | class | Loads one WAV stream into memory and plays it with `PlaySync` or `PlayASync`, pinning the buffer for asynchronous playback |
| [`WaveSoundPlayer`](WaveSoundPlayer.cs) | class | `IWaveSoundPlayer` that caches WAV data by key under count and byte limits and serializes synchronous playback, with overridable `PlayWave` and `StopWave` hooks for tests |
