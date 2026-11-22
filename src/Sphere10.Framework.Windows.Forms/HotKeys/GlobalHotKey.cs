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
using System.Threading;
using System.Windows.Forms;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Receives global shortcut messages on a private message-only window without installing a keyboard hook.</summary>
public sealed class GlobalHotKey : WinFormsSyncDisposableBase, IGlobalHotKey {
	/// <summary>A modifier flag for the Windows key, which <see cref="Keys"/> lacks; combine it like <see cref="Keys.Control"/> (for example WindowsKey | Keys.Alt | Keys.L).</summary>
	/// <remarks>The value occupies the unused modifier bit above <see cref="Keys.Alt"/>, so it stays inside <see cref="Keys.Modifiers"/> and outside <see cref="Keys.KeyCode"/>.</remarks>
	public const Keys WindowsKey = (Keys)0x00080000;

	public event EventHandlerEx Pressed;

	/// <summary>Raised after a held shortcut loses its key or a required modifier, or its registration is released.</summary>
	/// <remarks>Release checks run only while a shortcut with a release subscriber is held; ordinary shortcuts do not poll.</remarks>
	public event EventHandlerEx Released;

	private readonly int _ownerThreadId;
	private HotKeyWindow _window;
	private int _registeredId;
	private Keys _registeredShortcut;
	private SynchronizationContext _context;
	private Scheduler<BaseJob> _releaseScheduler;
	private BaseJob _releaseJob;
	private int _pressVersion;
	private bool _held;
	private bool _disposed;

	public GlobalHotKey() => _ownerThreadId = Environment.CurrentManagedThreadId;

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
		if ((int)key <= 0 || (int)key > 0xFE || !Enum.IsDefined(key) || (shortcut & ~(Keys.KeyCode | Keys.Control | Keys.Alt | Keys.Shift | WindowsKey)) != 0 ||
			key.IsIn(Keys.ShiftKey, Keys.ControlKey, Keys.Menu, Keys.LShiftKey, Keys.RShiftKey, Keys.LControlKey, Keys.RControlKey, Keys.LMenu, Keys.RMenu, Keys.LWin, Keys.RWin, Keys.F12)) {
			error = "Choose a regular key with optional Win, Ctrl, Alt or Shift modifiers. F12 is reserved by Windows.";
			return false;
		}
		var modifiers = WinAPI.USER32.MOD_NOREPEAT;
		if ((shortcut & Keys.Control) != 0)
			modifiers |= WinAPI.USER32.MOD_CONTROL;
		if ((shortcut & Keys.Alt) != 0)
			modifiers |= WinAPI.USER32.MOD_ALT;
		if ((shortcut & Keys.Shift) != 0)
			modifiers |= WinAPI.USER32.MOD_SHIFT;
		if ((shortcut & WindowsKey) != 0)
			modifiers |= WinAPI.USER32.MOD_WIN;
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
		EndPress();
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
		EndPress();
	}

	protected override void DisposeInternal() {
		if (_disposed)
			return;
		EnsureUsable();
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			EndPress();
			_releaseScheduler?.Dispose();
			_releaseScheduler = null;
			_window?.DestroyHandle();
			_window = null;
			_disposed = true;
			Pressed = null;
			Released = null;
		});
		Unregister();
	}

	private void EnsureUsable() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThreadId, "Global shortcuts must be registered and disposed on their creating UI thread.");
	}

	private void OnPressed(int id) {
		if (_disposed || id != _registeredId || _held)
			return;
		_held = Released != null;
		NotifyPressed();
		if (_held && !_disposed)
			StartReleaseCheck();
	}

	private void StartReleaseCheck() {
		_context ??= SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
		_releaseScheduler ??= new Scheduler<BaseJob>();
		var version = ++_pressVersion;
		var shortcut = _registeredShortcut;
		var posted = 0;
		_releaseJob = JobBuilder.For(() => {
			if (IsShortcutDown(shortcut) || Interlocked.Exchange(ref posted, 1) != 0)
				return;
			_context.Post(_ => {
				if (!_disposed && _held && version == _pressVersion)
					EndPress();
			}, null);
		}).Called("Wait for shortcut release").Repeat.OnInterval(TimeSpan.FromMilliseconds(16)).Build();
		_releaseScheduler.AddJob(_releaseJob);
		_releaseScheduler.Start();
	}

	private void EndPress() {
		++_pressVersion;
		if (_releaseJob != null) {
			_releaseScheduler.RemoveJob(_releaseJob);
			_releaseJob = null;
		}
		if (!_held)
			return;
		_held = false;
		NotifyReleased();
	}

	private static bool IsShortcutDown(Keys shortcut)
		=> IsKeyDown(shortcut & Keys.KeyCode)
			&& ((shortcut & Keys.Control) == 0 || IsKeyDown(Keys.ControlKey))
			&& ((shortcut & Keys.Alt) == 0 || IsKeyDown(Keys.Menu))
			&& ((shortcut & Keys.Shift) == 0 || IsKeyDown(Keys.ShiftKey))
			&& ((shortcut & WindowsKey) == 0 || IsKeyDown(Keys.LWin) || IsKeyDown(Keys.RWin));

	private static bool IsKeyDown(Keys key) => (WinAPI.USER32.GetAsyncKeyState((int)key) & 0x8000) != 0;

	private void NotifyPressed() => Notify(Pressed);

	private void NotifyReleased() => Notify(Released);

	private static void Notify(EventHandlerEx notification) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => notification?.Invoke(), out var error);
		if (error != null) {
			// Subscriber failures must not cross the native window-procedure boundary.
			Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Exception(error));
		}
	}

	#region Inner Types
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
	#endregion
}
