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

/// <summary>Owns a bounded cache of WAV data and serializes synchronous Windows playback.</summary>
public interface IWaveSoundPlayer : IDisposable {

	/// <summary>Plays a cached WAV, loading a missing key from an owned factory stream. This method blocks until playback ends.</summary>
	void Play(object key, Func<Stream> streamFactory);

	/// <summary>Requests that current playback stop and cancels playback still loading. A native playback admission race can finish naturally.</summary>
	void Stop();
}
