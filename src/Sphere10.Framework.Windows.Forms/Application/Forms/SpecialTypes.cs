// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Reports whether the native window closed and whether its shutdown callback failed.</summary>
internal sealed class ApplicationCloseResult {

	/// <summary>Captures the completed native close and shutdown outcome.</summary>
	public ApplicationCloseResult(bool closed, Exception shutdownError) {
		Closed = closed;
		ShutdownError = shutdownError;
	}

	/// <summary>Gets whether the native window was disposed by the close request.</summary>
	public bool Closed { get; }

	/// <summary>Gets the shutdown failure, or null when shutdown completed successfully.</summary>
	public Exception ShutdownError { get; }
}
