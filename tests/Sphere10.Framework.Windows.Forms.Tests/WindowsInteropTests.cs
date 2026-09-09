// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.SourceGrid.DevAgeControls;
using LegacyDes = Sphere10.Framework.Windows.Forms.SourceGrid.Security.Cryptography.Utilities.DES;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WindowsInteropTests {
	[Test]
	public void NativeLayoutsMatchWindowsStructures() {
		Assert.That(Marshal.SizeOf<WinAPI.RICHEDIT.CHARFORMAT>(), Is.EqualTo(116), "CHARFORMAT2W includes a 32-character Unicode face name");
		Assert.That(Marshal.SizeOf<WinAPI.RICHEDIT.FORMATRANGE>(), Is.EqualTo(IntPtr.Size == 8 ? 56 : 48));
		Assert.That(Marshal.SizeOf<WinAPI.USER32.ICONINFO>(), Is.EqualTo(IntPtr.Size == 8 ? 32 : 20));
	}

	[Test]
	public void RichEditCharacterFormattingRoundTripsThroughWindowsInterop() {
		using var editor = new DevAgeRichTextBox { Text = "Formatted text" };
		_ = editor.Handle;
		editor.SelectAll();
		editor.SelectionUnderlineStyle = UnderlineStyle.Wave;
		editor.SelectionUnderlineColor = UnderlineColor.Red;
		Assert.That(editor.SelectionUnderlineStyle, Is.EqualTo(UnderlineStyle.Wave));
		Assert.That(editor.SelectionUnderlineColor, Is.EqualTo(UnderlineColor.Red));
		editor.BeginUpdate();
		editor.EndUpdate();
		Assert.That(editor.InternalUpdating, Is.False);
	}

	[Test]
	public void RichEditFormattingMeasuresAllTextAndReleasesItsCache() {
		using var editor = new DevAgeRichTextBox { Text = "Native rich edit rendering" };
		using var bitmap = new Bitmap(500, 100);
		_ = editor.Handle;
		using var formatScope = Tools.Scope.ExecuteOnDispose(() => Tools.Windows.Win32.ReleaseRichTextFormat(editor.Handle));
		var lastCharacter = Tools.Windows.Win32.FormatRichText(editor.Handle, bitmap, false, 0, editor.Text.Length);
		Assert.That(lastCharacter, Is.GreaterThanOrEqualTo(editor.Text.Length));
	}

	[Test]
	public void CursorCreationPreservesItsHotspot() {
		using var bitmap = new Bitmap(16, 16);
		var cursorHandle = Tools.Windows.Win32.CreateCursorHandle(bitmap, 3, 5);
		using var cursorScope = Tools.Scope.ExecuteOnDispose(() => WinAPI.USER32.DestroyCursor(cursorHandle));
		var iconInfo = new WinAPI.USER32.ICONINFO();
		Assert.That(WinAPI.USER32.GetIconInfo(cursorHandle, ref iconInfo), Is.True);
		using var maskScope = Tools.Scope.ExecuteOnDispose(() => WinAPI.GDI32.DeleteObject(iconInfo.hbmMask));
		using var colorScope = Tools.Scope.ExecuteOnDispose(() => WinAPI.GDI32.DeleteObject(iconInfo.hbmColor));
		Assert.That(iconInfo.fIcon, Is.False);
		Assert.That(iconInfo.xHotspot, Is.EqualTo(3));
		Assert.That(iconInfo.yHotspot, Is.EqualTo(5));
	}

	[Test]
	public void DeviceHooksRaiseDisposedOnceWithoutInstallingHooks() {
		var keyboard = new WindowsKeyboardHook();
		var mouse = new WindowsMouseHook();
		var keyboardNotifications = 0;
		var mouseNotifications = 0;
		keyboard.Disposed += (_, _) => keyboardNotifications++;
		mouse.Disposed += (_, _) => mouseNotifications++;
		keyboard.Dispose();
		keyboard.Dispose();
		mouse.Dispose();
		mouse.Dispose();
		Assert.That(keyboardNotifications, Is.EqualTo(1));
		Assert.That(mouseNotifications, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DeviceHookCleanupRunsOnceAndOnlyExplicitDisposalInvokesThePublicOverride(bool explicitlyDispose) {
		using var hook = new TestDeviceHook();
		hook.InstallHook();
		if (explicitlyDispose) {
			hook.Dispose();
			hook.Dispose();
		} else {
			hook.ReleaseFromFinalizer();
			hook.ReleaseFromFinalizer();
		}
		Assert.That(hook.UninstallCalls, Is.EqualTo(1));
		Assert.That(hook.Status, Is.EqualTo(DeviceHookStatus.Uninstalled));
		Assert.That(hook.PublicDisposeCalls, Is.EqualTo(explicitlyDispose ? 1 : 0));
	}

	[Test]
	public void DeviceHookFinalizationReleasesTheHookWithoutInvokingPublicDisposal() {
		var probe = new HookDisposalProbe();
		var reference = CreateCollectableHook(probe);
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		Assert.That(reference.IsAlive, Is.False);
		Assert.That(probe.UninstallCalls, Is.EqualTo(1));
		Assert.That(probe.PublicDisposeCalls, Is.Zero);
	}

	[Test]
	public void SoundLoadingReadsFromTheCurrentPositionAndLeavesTheStreamOpen() {
		using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
		stream.Position = 2;
		using (var player = new SoundPlayerEx(stream)) {
			Assert.That(player.BytesToPlay, Is.EqualTo(new byte[] { 3, 4 }));
			Assert.That(stream.Position, Is.EqualTo(4));
		}
		Assert.That(stream.CanRead, Is.True);
		stream.Position = 0;
		Assert.That(stream.ReadByte(), Is.EqualTo(1));
	}

	[Test]
	public void SoundLoadingCompletesShortReadsWithoutRequiringASeekableStream() {
		var data = new byte[] { 1, 2, 3, 4, 5 };
		using var stream = new NonSeekableShortReadStream(data);
		using (var player = new SoundPlayerEx(stream)) {
			Assert.That(stream.CanSeek, Is.False);
			Assert.That(player.BytesToPlay, Is.EqualTo(data));
			Assert.That(stream.ReadCalls, Is.GreaterThanOrEqualTo(3));
		}
		Assert.That(stream.CanRead, Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void LegacyDesRetainsTheZeroFilledTailForPositionedPlaintext(bool shortReads) {
		var data = new byte[] { 1, 2, 3, 4 };
		using Stream input = shortReads ? new ShortReadStream(data) : new MemoryStream(data);
		input.Position = 2;
		using var output = new MemoryStream();
		LegacyDes.EncryptStream(input, output, "12345678");
		// Legacy input allocation uses Length: remaining bytes 3,4 are followed by two zero bytes.
		Assert.That(Convert.ToBase64String(output.ToArray()), Is.EqualTo("cp7w7NOhw2E="));
	}

	[Test]
	public void LegacyDesCompletesShortPlaintextReadsWithoutChangingTheCiphertextFormat() {
		using var input = new ShortReadStream(new byte[] { 1, 2, 3, 4, 5 });
		using var output = new MemoryStream();
		LegacyDes.EncryptStream(input, output, "12345678");
		Assert.That(Convert.ToBase64String(output.ToArray()), Is.EqualTo("xXrjv4y/1Yo="));
		Assert.That(input.ReadCalls, Is.GreaterThanOrEqualTo(3));
	}

	[Test]
	public void LegacyDesCompletesShortCiphertextReads() {
		using var input = new ShortReadStream(Convert.FromBase64String("xXrjv4y/1Yo="));
		using var output = new MemoryStream();
		using var outputStream = new NonClosingStream(output);
		LegacyDes.DecryptStream(input, outputStream, "12345678");
		Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
		Assert.That(input.ReadCalls, Is.GreaterThanOrEqualTo(4));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateCollectableHook(HookDisposalProbe probe) {
		var hook = new TestDeviceHook(probe);
		hook.InstallHook();
		return new WeakReference(hook);
	}

	private sealed class HookDisposalProbe {
		public int PublicDisposeCalls { get; set; }

		public int UninstallCalls { get; set; }
	}

	private sealed class TestDeviceHook : BaseDeviceHook {
		private readonly HookDisposalProbe _probe;

		public TestDeviceHook(HookDisposalProbe probe = null) {
			_probe = probe ?? new HookDisposalProbe();
		}

		public int PublicDisposeCalls => _probe.PublicDisposeCalls;

		public int UninstallCalls => _probe.UninstallCalls;

		public override void InstallHook() => Status = DeviceHookStatus.Active;

		public override void UninstallHook() {
			_probe.UninstallCalls++;
			Status = DeviceHookStatus.Uninstalled;
		}

		public override void Dispose() {
			if (Disposed)
				return;
			_probe.PublicDisposeCalls++;
			base.Dispose();
		}

		public void ReleaseFromFinalizer() => Dispose(false);
	}

	private class ShortReadStream : StreamDecorator<MemoryStream> {
		public ShortReadStream(byte[] data)
			: base(new MemoryStream(data)) {
		}

		public int ReadCalls { get; private set; }

		public override int Read(byte[] buffer, int offset, int count) {
			ReadCalls++;
			return base.Read(buffer, offset, Math.Min(count, 2));
		}

		public override int Read(Span<byte> buffer) {
			ReadCalls++;
			return base.Read(buffer[..Math.Min(buffer.Length, 2)]);
		}
	}

	private sealed class NonSeekableShortReadStream : ShortReadStream {
		public NonSeekableShortReadStream(byte[] data)
			: base(data) {
		}

		public override bool CanSeek => false;

		public override long Length => throw new NotSupportedException();

		public override long Position {
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
	}
}
