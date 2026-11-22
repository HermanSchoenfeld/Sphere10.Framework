// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Discovers physical desktop areas that should retain their original appearance.</summary>
/// <remarks>
/// Start, Stop, Dispose and notifications run on the consuming UI thread. Start and Stop are idempotent.
/// Stop cancels pending discovery and invalidates its publications immediately; it must not wait for UI callbacks.
/// Dispose joins owned work without awaiting UI dispatch. Results are detached values, never provider or COM references.
/// </remarks>
public interface IDesktopRegionSource : IDisposable {
	/// <summary>Replaces the complete set of discovered areas; an empty array clears the previous result.</summary>
	event EventHandlerEx<WindowsWindowRegion[]> Changed;

	/// <summary>Reports a discovery failure on the consuming UI thread.</summary>
	event EventHandlerEx<Exception> Failed;

	void Start();

	void Stop();

	/// <summary>Raises <see cref="Changed"/> now if the areas moved since it was last raised, rather than when the source's own notification is dispatched.</summary>
	/// <remarks>A consumer that holds the UI thread for a display frame at a time calls this before it uses the areas. A stopped source does nothing.</remarks>
	void Synchronize();
}
