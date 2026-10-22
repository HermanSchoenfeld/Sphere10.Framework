// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ComboBoxSelectionAppearanceTests {
	[TestCase(1f)]
	[TestCase(1.5f)]
	[TestCase(2f)]
	public void FocusedToolbarSelectionStaysGreyAndRetainsNativeKeyboardSelection(float fontScale) => RunWithMessageLoop(async owner => {
		using var font = new Font("Segoe UI", 9f * fontScale);
		using var toolbar = new ToolStrip { Font = font, Dock = DockStyle.Top };
		var selector = new ToolStripComboBox { AutoSize = false, Width = (int)(140 * fontScale), Font = font };
		selector.ComboBox.UseSelectionOnlyAppearance();
		selector.Items.AddRange(new object[] { "1 sec", "2 sec", "5 sec" });
		selector.SelectedIndex = 0;
		toolbar.Items.Add(new ToolStripLabel("Every"));
		toolbar.Items.Add(selector);
		owner.Controls.Add(toolbar);
		owner.PerformLayout();
		var combo = selector.ComboBox;
		await owner.InvokeAsync(() => { });
		Assert.That(combo.Focus(), Is.True);
		var handle = new HandleRef(combo, combo.Handle);
		WinAPI.USER32.SendMessage(handle, WinAPI.USER32.WM_KEYDOWN, (IntPtr)Keys.Down, (IntPtr)1);
		WinAPI.USER32.SendMessage(handle, 0x0101, (IntPtr)Keys.Down, (IntPtr)1); // WM_KEYUP
		Assert.That(combo.SelectedItem, Is.EqualTo("2 sec"), "The selection-only field must keep ordinary native keyboard navigation.");
		WinAPI.USER32.SendMessage(handle, 0x0102, (IntPtr)'X', (IntPtr)1); // WM_CHAR
		Assert.That(combo.Text, Is.EqualTo("2 sec"), "Typing must not create editable interval text.");
		Assert.That(combo.DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDownList));
		Assert.That(combo.Focused, Is.True);
		Assert.That(combo.DroppedDown, Is.False);
		using var image = new Bitmap(combo.Width, combo.Height);
		combo.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
		Assert.That(image.GetPixel(image.Width / 2, image.Height / 2).ToArgb(), Is.EqualTo(SystemColors.Control.ToArgb()),
			"A focused collapsed selector must retain its grey field instead of a blue text-selection background.");
		var directory = Environment.GetEnvironmentVariable("SPHERE10_WINFORMS_SCREENSHOTS");
		if (!string.IsNullOrWhiteSpace(directory)) {
			Directory.CreateDirectory(directory);
			using var screenshot = new Bitmap(owner.Width, owner.Height);
			owner.DrawToBitmap(screenshot, new Rectangle(Point.Empty, screenshot.Size));
			screenshot.Save(Path.Combine(directory, "selection-only-" + fontScale.ToString("0.0", CultureInfo.InvariantCulture) + ".png"), ImageFormat.Png);
		}
	});

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form {
			ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(600, 100)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("Selection-only toolbar test timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			await Task.Yield();
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
			completed = true;
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}
}
