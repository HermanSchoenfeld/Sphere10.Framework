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
using System.Windows.Forms;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Receives global shortcut messages on a private message-only window without installing a keyboard hook.</summary>
public sealed class GlobalHotKey : WinFormsSyncDisposableBase, IGlobalHotKey {
	public event EventHandler Pressed;

	private readonly int _ownerThreadId;
	private HotKeyWindow _window;
	private int _registeredId;
	private Keys _registeredShortcut;
	private bool _disposed;

	public GlobalHotKey() {
		_ownerThreadId = Environment.CurrentManagedThreadId;
	}

	public bool TryRegister(Keys shortcut, out string error) {
		EnsureUsable();
		error = null;
		if (shortcut == _registeredShortcut)
			return true;
		if (shortcut == Keys.None) {
			Unregister();
			return true;
		}

		// Reject modifier-only and reserved keys before changing an existing registration.
		var key = shortcut & Keys.KeyCode;
		if ((int)key <= 0 || (int)key > 0xFE || !Enum.IsDefined(key) || (shortcut & ~(Keys.KeyCode | Keys.Control | Keys.Alt | Keys.Shift)) != 0 ||
			key.IsIn(Keys.ShiftKey, Keys.ControlKey, Keys.Menu, Keys.LShiftKey, Keys.RShiftKey, Keys.LControlKey, Keys.RControlKey, Keys.LMenu, Keys.RMenu, Keys.LWin, Keys.RWin, Keys.F12)) {
			error = "Choose a regular key with optional Ctrl, Alt or Shift modifiers. F12 is reserved by Windows.";
			return false;
		}
		var modifiers = WinAPI.USER32.MOD_NOREPEAT;
		if ((shortcut & Keys.Control) != 0)
			modifiers |= WinAPI.USER32.MOD_CONTROL;
		if ((shortcut & Keys.Alt) != 0)
			modifiers |= WinAPI.USER32.MOD_ALT;
		if ((shortcut & Keys.Shift) != 0)
			modifiers |= WinAPI.USER32.MOD_SHIFT;
		_window ??= new HotKeyWindow(this);

		// Each instance owns a distinct HWND. Alternate its local IDs to bind the replacement before releasing the old shortcut.
		var nextId = _registeredId == 1 ? 2 : 1;
		if (!WinAPI.USER32.RegisterHotKey(_window.Handle, nextId, modifiers, (uint)key)) {
			var errorCode = Marshal.GetLastWin32Error();
			error = $"The shortcut is unavailable or already used by another application. {WindowsException.ErrorCodeToDescription(errorCode)}";
			return false;
		}
		if (_registeredId != 0 && !WinAPI.USER32.UnregisterHotKey(_window.Handle, _registeredId)) {
			var errorCode = Marshal.GetLastWin32Error();
			WinAPI.USER32.UnregisterHotKey(_window.Handle, nextId);
			error = $"The previous shortcut could not be released. {WindowsException.ErrorCodeToDescription(errorCode)}";
			return false;
		}
		_registeredId = nextId;
		_registeredShortcut = shortcut;
		return true;
	}

	public void Unregister() {
		EnsureUsable();
		if (_registeredId == 0)
			return;
		if (!WinAPI.USER32.UnregisterHotKey(_window.Handle, _registeredId))
			throw new Win32Exception(Marshal.GetLastWin32Error(), "The global shortcut could not be released.");
		_registeredId = 0;
		_registeredShortcut = Keys.None;
	}

	protected override void FreeManagedResources() {
		if (_disposed)
			return;
		EnsureUsable();
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			_window?.DestroyHandle();
			_window = null;
			_disposed = true;
			Pressed = null;
		});
		Unregister();
	}

	private void EnsureUsable() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThreadId, "Global shortcuts must be registered and disposed on their creating UI thread.");
	}

	private void OnPressed(int id) {
		if (_disposed || id != _registeredId)
			return;
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			Pressed?.Invoke(this, EventArgs.Empty);
		}, out var error);
		if (error != null) {
			// Subscriber failures must not cross the native window-procedure boundary.
			Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Exception(error));
		}
	}

	private sealed class HotKeyWindow : NativeWindow {
		private readonly GlobalHotKey _owner;

		public HotKeyWindow(GlobalHotKey owner) {
			_owner = owner;
			CreateHandle(new CreateParams { Caption = "Sphere10.GlobalHotKey", Parent = WinAPI.USER32.HWND_MESSAGE });
		}

		protected override void WndProc(ref Message message) {
			if (message.Msg == WinAPI.USER32.WM_HOTKEY) {
				_owner.OnPressed(message.WParam.ToInt32());
				return;
			}
			base.WndProc(ref message);
		}
	}
}
