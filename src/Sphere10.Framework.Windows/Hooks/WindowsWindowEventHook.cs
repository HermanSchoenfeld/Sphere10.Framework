// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

/// <summary>Hook over one range of the accessible events (WinEvents) that every window on the desktop raises.</summary>
/// <remarks>
/// The hook is out of context: Windows queues each event to the thread that installed it, so install, uninstall and dispose
/// on one thread with a message loop, and <see cref="EventRaised"/> is raised there. It sees every process's events in the
/// range, so a handler rejects the ones it does not want before making any native call. Created uninstalled; the caller
/// installs and disposes it.
/// </remarks>
public sealed class WindowsWindowEventHook : SyncDisposable {
	public event EventHandlerEx<WindowsWindowEvent> EventRaised;

	// Windows holds only the function pointer, so the delegate lives as long as the hook object.
	private readonly WinAPI.USER32.WinEventProc _callback;
	private IntPtr _hook;

	public WindowsWindowEventHook(uint firstEvent, uint lastEvent) {
		Guard.Argument(firstEvent <= lastEvent, nameof(lastEvent), "The last event cannot precede the first.");
		FirstEvent = firstEvent;
		LastEvent = lastEvent;
		_callback = Callback;
	}

	public uint FirstEvent { get; }

	public uint LastEvent { get; }

	public bool IsInstalled => _hook != IntPtr.Zero;

	public void InstallHook() {
		Guard.Ensure(!IsInstalled, "The window-event hook is already installed.");
		_hook = WinAPI.USER32.SetWinEventHook(FirstEvent, LastEvent, IntPtr.Zero, _callback, 0, 0, WinAPI.USER32.WINEVENT_OUTOFCONTEXT);
		if (_hook == IntPtr.Zero)
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not subscribe to window events.");
	}

	/// <summary>Removes the hook; does nothing when it is not installed. Events already queued to the thread are still raised.</summary>
	public void UninstallHook() {
		if (!IsInstalled)
			return;
		WinAPI.USER32.UnhookWinEvent(_hook);
		_hook = IntPtr.Zero;
	}

	protected override void DisposeInternal() => UninstallHook();

	private void Callback(IntPtr hook, uint eventType, IntPtr window, int objectId, int childId, uint eventThread, uint eventTime)
		=> EventRaised?.Invoke(new WindowsWindowEvent(eventType, window, objectId, childId, eventThread, eventTime));
}
