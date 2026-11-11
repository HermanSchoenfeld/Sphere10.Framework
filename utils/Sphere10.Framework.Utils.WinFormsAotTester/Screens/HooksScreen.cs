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

/// <summary>Explicitly starts and stops owned keyboard/mouse observation with bounded native output.</summary>
public partial class HooksScreen : ApplicationScreen {
	private readonly WindowsKeyboardHook _keyHook;
	private readonly WindowsMouseHook _mouseHook;
	private readonly System.Windows.Forms.Timer _refreshTimer;
	private readonly ConcurrentQueue<string> _pendingText = new();
	private volatile bool _acceptHookEvents;
	private bool _loaded;
	private bool _monitorRequested;
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

	/// <summary>Creates the demonstration without installing global input hooks.</summary>
	public HooksScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		_refreshTimer = new System.Windows.Forms.Timer { Interval = 50 };
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

	/// <summary>Creates the native counters without installing a global hook.</summary>
	protected override void OnLoad(EventArgs Event) {
		base.OnLoad(Event);
		_loaded = true;
		_status.Text = "Monitoring is off. Start explicitly to observe keyboard and mouse activity.";
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
		if (_acceptHookEvents || !_monitorRequested || !_loaded || _hooksDisposed || Disposing || IsDisposed || !IsHandleCreated || DesignMode)
			return;

		var started = false;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			if (!started)
				StopHooks();
		});
		_keyHook.StartHook();
		_mouseHook.StartHook();
		_acceptHookEvents = true;
		_refreshTimer.Start();
		started = true;
	}

	private void StopHooks() {
		_acceptHookEvents = false;
		_refreshTimer?.Stop();
		using var stopMouse = Tools.Scope.ExecuteOnDispose(() => StopHook(_mouseHook));
		StopHook(_keyHook);
	}

	private static void StopHook(IDeviceHook hook) {
		if (hook == null || hook.Status == DeviceHookStatus.Uninstalled)
			return;

		hook.StopHook();
		hook.UninstallHook();
	}

	private void DisposeHooks() {
		if (_hooksDisposed)
			return;

		_hooksDisposed = true;
		_acceptHookEvents = false;
		_refreshTimer.Stop();
		_refreshTimer.Tick -= RefreshTimer_Tick;
		_refreshTimer.Dispose();
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
		using var disposeMouse = Tools.Scope.ExecuteOnDispose(_mouseHook.Dispose);
		_keyHook.Dispose();
	}

	private void _mouseHook_Scroll(object sender, MouseWheelEvent hookEvent) {
		if (!_acceptHookEvents)
			return;

		Interlocked.Increment(ref _mouseWheel);
		QueueText($"Wheel Delta = {hookEvent.Delta}{Environment.NewLine}");
	}

	private void _mouseHook_Click(object sender, MouseClickEvent hookEvent) {
		if (!_acceptHookEvents)
			return;

		switch (hookEvent.ButtonState) {
			case MouseButtonState.Down:
				Interlocked.Increment(ref _mouseClickDown);
				break;
			case MouseButtonState.Up:
				Interlocked.Increment(ref _mouseClickUp);
				break;
		}
		QueueText($"Clicked {hookEvent.Buttons} {hookEvent.ButtonState} ({hookEvent.ClickType}){Environment.NewLine}");
	}

	private void _mouseHook_Activity(object sender, MouseEvent hookEvent) {
		if (_acceptHookEvents)
			Interlocked.Increment(ref _mouseActivity);
	}

	private void _mouseHook_MotionStop(object sender, MouseMoveEvent hookEvent) {
		if (_acceptHookEvents)
			Interlocked.Increment(ref _mouseMotionStop);
	}

	private void _mouseHook_MotionStart(object sender, MouseMoveEvent hookEvent) {
		if (_acceptHookEvents)
			Interlocked.Increment(ref _mouseMotionStart);
	}

	private void _mouseHook_Motion(object sender, MouseMoveEvent hookEvent) {
		if (_acceptHookEvents)
			Interlocked.Increment(ref _mouseMotion);
	}

	private void _keyHook_KeyUp(object sender, KeyEvent hookEvent) {
		if (!_acceptHookEvents)
			return;

		Interlocked.Increment(ref _keyUp);
		QueueText($"Key Up = {hookEvent.Key}{Environment.NewLine}");
	}

	private void _keyHook_KeyDown(object sender, KeyEvent hookEvent) {
		if (!_acceptHookEvents)
			return;

		Interlocked.Increment(ref _keyDown);
		QueueText($"Key Down = {hookEvent.Key}{Environment.NewLine}");
	}

	private void _keyHook_KeyActivity(object sender, KeyEvent hookEvent) {
		if (_acceptHookEvents)
			Interlocked.Increment(ref _keyActivity);
	}

	private void RefreshTimer_Tick(object sender, EventArgs hookEvent) {
		if (!_acceptHookEvents || Disposing || IsDisposed || !IsHandleCreated)
			return;

		RefreshUI();
	}

	private void RefreshUI() {
		// Hook callbacks only collect data; the UI timer owns all control updates.
		var output = new StringBuilder();
		while (_pendingText.TryDequeue(out var line))
			output.Append(line);
		if (output.Length > 0) {
			textBox1.AppendText(output.ToString());
			if (textBox1.TextLength > 16000)
				textBox1.Text = textBox1.Text[^12000..];
		}

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

	// The user starts observation deliberately; construction and screen activation stay resource-free.
	private void StartClicked(object sender, EventArgs e) {
		try {
			_monitorRequested = true;
			StartHooks();
			_status.Text = "Monitoring keyboard and mouse events. Stop ends global observation.";
		} catch (Exception error) {
			_monitorRequested = false;
			StopHooks();
			_status.Text = "Could not start monitoring: " + error.Message;
		}
	}

	private void StopClicked(object sender, EventArgs e) {
		_monitorRequested = false;
		StopHooks();
		_status.Text = "Monitoring stopped. Recorded counters remain available.";
	}

	private void ClearClicked(object sender, EventArgs e) {
		_pendingText.Clear();
		textBox1.Clear();
		Interlocked.Exchange(ref _keyActivity, 0);
		Interlocked.Exchange(ref _keyDown, 0);
		Interlocked.Exchange(ref _keyUp, 0);
		Interlocked.Exchange(ref _mouseActivity, 0);
		Interlocked.Exchange(ref _mouseMotion, 0);
		Interlocked.Exchange(ref _mouseMotionStart, 0);
		Interlocked.Exchange(ref _mouseMotionStop, 0);
		Interlocked.Exchange(ref _mouseClickDown, 0);
		Interlocked.Exchange(ref _mouseClickUp, 0);
		Interlocked.Exchange(ref _mouseWheel, 0);
		RefreshUI();
	}

	// Bound callback backlog even when the UI is temporarily busy.
	private void QueueText(string text) {
		if (_pendingText.Count < 2000)
			_pendingText.Enqueue(text);
	}
}
