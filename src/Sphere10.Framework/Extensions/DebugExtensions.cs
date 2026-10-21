// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

// Keep assertions available to Debug consumers of Release-built libraries; Conditional controls the caller.
#define DEBUG

using System.Diagnostics;
using System.Threading;

namespace Sphere10.Framework;

public static class DebugExtensions {
	private static volatile Thread _mainThread;

	extension(Debug) {
		/// <summary>The application's main thread, assigned at its entry point. Null leaves thread assertions unconfigured.</summary>
		public static Thread MainThread {
			get => _mainThread;
			set => _mainThread = value;
		}

		[Conditional("DEBUG")]
		public static void AssertNotMainThread()
			=> Debug.AssertNotMainThread("This operation must not run on the main thread.");

		[Conditional("DEBUG")]
		public static void AssertNotMainThread(string message)
			=> Debug.AssertNotMainThread(message, null);

		[Conditional("DEBUG")]
		public static void AssertNotMainThread(string message, string detailMessage)
			=> Debug.Assert(Thread.CurrentThread != _mainThread, message, detailMessage);
	}
}
