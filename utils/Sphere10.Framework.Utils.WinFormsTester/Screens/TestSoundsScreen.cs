// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Media;
using SharpDX.DirectSound;
using SharpDX.Multimedia;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;


/// <summary>Compares explicit sound playback while owning every audio device, stream and buffer.</summary>
public partial class TestSoundsScreen : ApplicationScreen {
	private DirectSound _device;
	private SecondarySoundBuffer _buffer;
	private SoundPlayer _player;
	private MemoryStream _playerStream;

	/// <summary>Creates controls without opening an audio device or playing a sound.</summary>
	public TestSoundsScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		_output.Text = "DirectSound buttons play the embedded mouse-button WAV resources." + Environment.NewLine
			+ "The SoundPlayer example uses the same left-button-down resource." + Environment.NewLine
			+ "Stop or close this screen to release the active playback resources.";
	}

	protected override void OnDestroyScreen() {
		StopPlayback();
		base.OnDestroyScreen();
	}

	// Read the actual WAV format instead of treating the file header as hard-coded PCM samples.
	private void PlaySound(Stream sound) {
		StopPlayback();
		try {
			using (sound)
			using (var wave = new SoundStream(sound)) {
				_device = new DirectSound();
				_device.SetCooperativeLevel(Handle, CooperativeLevel.Priority);
				var description = new SoundBufferDescription {
					Format = wave.Format, BufferBytes = checked((int)wave.Length), Flags = BufferFlags.GlobalFocus
				};
				_buffer = new SecondarySoundBuffer(_device, description);
				using var data = wave.ToDataStream();
				_buffer.Write(data.ReadAll(), 0, LockFlags.None);
				_buffer.Play(0, PlayFlags.None);
			}
			_status.Text = "Playing the selected sound through DirectSound.";
		} catch (Exception error) {
			StopPlayback();
			_status.Text = "Playback failed: " + error.Message;
		}
	}

	private void button1_Click(object sender, EventArgs e) => PlaySound(Resources.LeftClickDown);

	private void button2_Click(object sender, EventArgs e) => PlaySound(Resources.LeftClickUp);

	private void button3_Click(object sender, EventArgs e) => PlaySound(Resources.RightClickDown);

	private void button4_Click(object sender, EventArgs e) => PlaySound(Resources.RightClickUp);

	private void _systemPlayerButton_Click(object sender, EventArgs e) {
		StopPlayback();
		try {
			using var resource = Resources.LeftClickDown;
			_playerStream = new MemoryStream(resource.ReadAll());
			_player = new SoundPlayer(_playerStream);
			_player.Load();
			_player.Play();
			_status.Text = "Playing the left-button-down sound through SoundPlayer.";
		} catch (Exception error) {
			StopPlayback();
			_status.Text = "Playback failed: " + error.Message;
		}
	}

	private void StopClicked(object sender, EventArgs e) {
		StopPlayback();
		_status.Text = "Playback stopped and owned audio resources released.";
	}

	private void StopPlayback() {
		_buffer?.Stop();
		_buffer?.Dispose();
		_buffer = null;
		_device?.Dispose();
		_device = null;
		_player?.Stop();
		_player?.Dispose();
		_player = null;
		_playerStream?.Dispose();
		_playerStream = null;
	}
}
