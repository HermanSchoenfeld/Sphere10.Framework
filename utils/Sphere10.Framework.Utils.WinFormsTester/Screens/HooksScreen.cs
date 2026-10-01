// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text;
using System.Threading;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

public partial class HooksScreen : WinFormsApplicationScreen {
	private readonly WindowsKeyboardHook _keyHook;
	private readonly WindowsMouseHook _mouseHook;
	private readonly System.Windows.Forms.Timer _refreshTimer;
	private readonly ConcurrentQueue<string> _pendingText = new();
	private volatile bool _acceptHookEvents;
	private bool _loaded;
	private bool _hooksDisposed;
	private long _keyActivity;
	private long _keyDown;
	private long _keyUp;
	private long _mouseActivity;
	private long _mouseMotion;
	private long _mouseMotionStart;
	private long _mouseMotionStop;
	private long _mouseClickDown;
	private long _mouseClickUp;
	private long _mouseWheel;

	public HooksScreen() {
		InitializeComponent();
		_refreshTimer = new System.Windows.Forms.Timer(components ??= new Container()) { Interval = 50 };
		_refreshTimer.Tick += RefreshTimer_Tick;
		_keyHook = new WindowsKeyboardHook();
		_keyHook.KeyActivity += _keyHook_KeyActivity;
		_keyHook.KeyDown += _keyHook_KeyDown;
		_keyHook.KeyUp += _keyHook_KeyUp;
		_mouseHook = new WindowsMouseHook();
		_mouseHook.Activity += _mouseHook_Activity;
		_mouseHook.Motion += _mouseHook_Motion;
		_mouseHook.MotionStart += _mouseHook_MotionStart;
		_mouseHook.MotionStop += _mouseHook_MotionStop;
		_mouseHook.Click += _mouseHook_Click;
		_mouseHook.Scroll += _mouseHook_Scroll;
		RefreshUI();
	}

	protected override void OnLoad(EventArgs Event) {
		base.OnLoad(Event);
		_loaded = true;
		StartHooks();
	}

	protected override void OnHandleCreated(EventArgs Event) {
		base.OnHandleCreated(Event);
		StartHooks();
	}

	protected override void OnHandleDestroyed(EventArgs Event) {
		StopHooks();
		base.OnHandleDestroyed(Event);
	}

	protected override void OnDestroyScreen() {
		DisposeHooks();
		base.OnDestroyScreen();
	}

	private void StartHooks() {
		if (!_loaded || _hooksDisposed || Disposing || IsDisposed || !IsHandleCreated || DesignMode)
			return;

		var Started = false;
		using var Cleanup = Tools.Scope.ExecuteOnDispose(() => {
			if (!Started)
				StopHooks();
		});
		_keyHook.StartHook();
		_mouseHook.StartHook();
		_acceptHookEvents = true;
		_refreshTimer.Start();
		Started = true;
	}

	private void StopHooks() {
		_acceptHookEvents = false;
		_refreshTimer?.Stop();
		using var StopMouse = Tools.Scope.ExecuteOnDispose(() => StopHook(_mouseHook));
		StopHook(_keyHook);
	}

	private static void StopHook(IDeviceHook Hook) {
		if (Hook == null || Hook.Status == DeviceHookStatus.Uninstalled)
			return;

		Hook.StopHook();
		Hook.UninstallHook();
	}

	private void DisposeHooks() {
		if (_hooksDisposed)
			return;

		_hooksDisposed = true;
		_acceptHookEvents = false;
		_refreshTimer.Stop();
		_refreshTimer.Tick -= RefreshTimer_Tick;
		_keyHook.KeyActivity -= _keyHook_KeyActivity;
		_keyHook.KeyDown -= _keyHook_KeyDown;
		_keyHook.KeyUp -= _keyHook_KeyUp;
		_mouseHook.Activity -= _mouseHook_Activity;
		_mouseHook.Motion -= _mouseHook_Motion;
		_mouseHook.MotionStart -= _mouseHook_MotionStart;
		_mouseHook.MotionStop -= _mouseHook_MotionStop;
		_mouseHook.Click -= _mouseHook_Click;
		_mouseHook.Scroll -= _mouseHook_Scroll;
		_pendingText.Clear();
		using var DisposeMouse = Tools.Scope.ExecuteOnDispose(_mouseHook.Dispose);
		_keyHook.Dispose();
	}

	private void _mouseHook_Scroll(object Sender, MouseWheelEvent Event) {
		if (!_acceptHookEvents)
			return;

		Interlocked.Increment(ref _mouseWheel);
		_pendingText.Enqueue($"Wheel Delta = {Event.Delta}{Environment.NewLine}");
	}

	private void _mouseHook_Click(object Sender, MouseClickEvent Event) {
		if (!_acceptHookEvents)
			return;

		switch (Event.ButtonState) {
			case MouseButtonState.Down:
				Interlocked.Increment(ref _mouseClickDown);
				break;
			case MouseButtonState.Up:
				Interlocked.Increment(ref _mouseClickUp);
				break;
		}
		_pendingText.Enqueue($"Clicked {Event.Buttons} {Event.ButtonState} ({Event.ClickType}){Environment.NewLine}");
	}

	private void _mouseHook_Activity(object Sender, MouseEvent Event) {
		if (_acceptHookEvents)
			Interlocked.Increment(ref _mouseActivity);
	}

	private void _mouseHook_MotionStop(object Sender, MouseMoveEvent Event) {
		if (_acceptHookEvents)
			Interlocked.Increment(ref _mouseMotionStop);
	}

	private void _mouseHook_MotionStart(object Sender, MouseMoveEvent Event) {
		if (_acceptHookEvents)
			Interlocked.Increment(ref _mouseMotionStart);
	}

	private void _mouseHook_Motion(object Sender, MouseMoveEvent Event) {
		if (_acceptHookEvents)
			Interlocked.Increment(ref _mouseMotion);
	}

	private void _keyHook_KeyUp(object Sender, KeyEvent Event) {
		if (!_acceptHookEvents)
			return;

		Interlocked.Increment(ref _keyUp);
		_pendingText.Enqueue($"Key Up = {Event.Key}{Environment.NewLine}");
	}

	private void _keyHook_KeyDown(object Sender, KeyEvent Event) {
		if (!_acceptHookEvents)
			return;

		Interlocked.Increment(ref _keyDown);
		_pendingText.Enqueue($"Key Down = {Event.Key}{Environment.NewLine}");
	}

	private void _keyHook_KeyActivity(object Sender, KeyEvent Event) {
		if (_acceptHookEvents)
			Interlocked.Increment(ref _keyActivity);
	}

	private void RefreshTimer_Tick(object Sender, EventArgs Event) {
		if (!_acceptHookEvents || Disposing || IsDisposed || !IsHandleCreated)
			return;

		RefreshUI();
	}

	private void RefreshUI() {
		// Hook callbacks only collect data; the UI timer owns all control updates.
		var Text = new StringBuilder();
		while (_pendingText.TryDequeue(out var Line))
			Text.Append(Line);
		if (Text.Length > 0)
			textBox1.AppendText(Text.ToString());

		_keyActivityEventsLabel.Text = Interlocked.Read(ref _keyActivity).ToString();
		_keyDownEventsLabel.Text = Interlocked.Read(ref _keyDown).ToString();
		_keyUpEventsLabel.Text = Interlocked.Read(ref _keyUp).ToString();
		_mouseActivityEventsLabel.Text = Interlocked.Read(ref _mouseActivity).ToString();
		_mouseMoveEventsLabel.Text = Interlocked.Read(ref _mouseMotion).ToString();
		_mouseStartEventsLabel.Text = Interlocked.Read(ref _mouseMotionStart).ToString();
		_mouseStopEventsLabel.Text = Interlocked.Read(ref _mouseMotionStop).ToString();
		_clickDownEventsLabel.Text = Interlocked.Read(ref _mouseClickDown).ToString();
		_clickUpEventLabel.Text = Interlocked.Read(ref _mouseClickUp).ToString();
		_wheelEvents.Text = Interlocked.Read(ref _mouseWheel).ToString();
	}
}
