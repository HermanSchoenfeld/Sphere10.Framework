// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>Discovers existing input operations and creates caller-owned hooks, preserving each constructor's monitor and motion-timing behavior.</summary>
	public static class Hooks {
		public static bool IsAnyMouseButtonDown() => HooksTool.IsAnyMouseButtonDown();

		public static void SimulateKeyboardDeviceAction(ScanCode scanCode, KeyState keyState) => HooksTool.SimulateKeyboardDeviceAction(scanCode, keyState);

		public static void SimulateMouseDeviceAction(MouseButton button, MouseButtonState state, int x, int y) => HooksTool.SimulateMouseDeviceAction(button, state, x, y);

		public static void SimulateMouseWheel(int delta) => HooksTool.SimulateMouseWheel(delta);

		/// <summary>Creates a fresh, uninstalled hook with the existing default monitor. The caller must dispose the hook.</summary>
		public static WindowsMouseHook CreateMouseHook() => new WindowsMouseHook();

		public static WindowsMouseHook CreateMouseHook(TimeSpan movingStoppedInterval) => new WindowsMouseHook(movingStoppedInterval);

		/// <summary>Creates a fresh, uninstalled hook borrowing the supplied monitor. Disposing the hook does not dispose the monitor.</summary>
		public static WindowsMouseHook CreateMouseHook(IActiveApplicationMonitor activeApplicationMonitor) => new WindowsMouseHook(activeApplicationMonitor);

		public static WindowsMouseHook CreateMouseHook(IActiveApplicationMonitor activeApplicationMonitor, TimeSpan movingStoppedInterval)
			=> new WindowsMouseHook(activeApplicationMonitor, movingStoppedInterval);

		/// <summary>Creates a fresh, uninstalled hook with the existing default monitor. The caller must dispose the hook.</summary>
		public static WindowsKeyboardHook CreateKeyboardHook() => new WindowsKeyboardHook();

		/// <summary>Creates a fresh, uninstalled hook borrowing the supplied monitor. Disposing the hook does not dispose the monitor.</summary>
		public static WindowsKeyboardHook CreateKeyboardHook(IActiveApplicationMonitor activeApplicationMonitor) => new WindowsKeyboardHook(activeApplicationMonitor);
	}
}
