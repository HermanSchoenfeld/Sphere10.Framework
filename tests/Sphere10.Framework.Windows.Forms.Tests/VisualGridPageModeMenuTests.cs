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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class VisualGridPageModeMenuTests {
	[TestCase(9F)]
	[TestCase(16F)]
	public void NativeMenuClicksSelectSingleAutoAndManualAndRetainTheManualSize(float points) {
		RunWithMessageLoop(async owner => {
			using var font = new Font("Segoe UI", points);
			using var grid = new VisualGrid {
				Dock = DockStyle.Fill,
				Font = font,
				GridBindings = new[] {
					new VisualGridColumn<string> { ColumnName = "Record", PropertyValue = value => value, DataType = typeof(string), ExpandsToFit = true }
				}
			};
			owner.Controls.Add(grid);
			await grid.SetDataSource(new ListDataSource<string>(new ExtendedList<string>(Enumerable.Range(1, 120).Select(number => "Record " + number).ToArray())));
			var manualSize = Find<NumericUpDown>(grid, "_pageSizeUpDown");
			manualSize.Value = 23;
			await RefreshAsync(grid);
			var toolbar = Find<ToolStrip>(grid, "_pageModeToolStrip");
			var selector = toolbar.Items.OfType<ToolStripChoiceSelector>().Single();
			Assert.That(selector.DisplayStyle, Is.EqualTo(ToolStripItemDisplayStyle.Image));
			Assert.That(selector.Image, Is.Not.Null);
			Assert.That(selector.AccessibleName, Is.EqualTo("Page mode"));
			Assert.That(selector.DropDownItems.Cast<ToolStripMenuItem>().Select(item => item.Text), Is.EqualTo(new[] { "Single", "Auto", "Manual" }));
			Assert.That(toolbar.DisplayRectangle.Contains(selector.Bounds), Is.True, "The compact button must fit its actual toolbar.");
			Assert.That(toolbar.Width, Is.LessThan(TextRenderer.MeasureText("Manual", font).Width + SystemInformation.VerticalScrollBarWidth + 24));

			foreach (var mode in new[] { VisualGridPageMode.Single, VisualGridPageMode.Auto, VisualGridPageMode.Paged }) {
				ClickNative(selector);
				Assert.That(selector.DropDown.Visible, Is.True, "A native button click must open its checked choice menu.");
				var option = selector.DropDownItems.Cast<ToolStripMenuItem>().Single(item => Equals(item.Tag, mode));
				ClickNative(option);
				await RefreshAsync(grid);

				Assert.That(grid.PageMode, Is.EqualTo(mode));
				Assert.That(selector.SelectedItem, Is.EqualTo(mode));
				Assert.That(selector.DropDownItems.Cast<ToolStripMenuItem>().Where(item => item.Checked).Select(item => item.Tag), Is.EqualTo(new object[] { mode }));
				Assert.That(selector.ToolTipText, Is.EqualTo("Page mode: " + (mode == VisualGridPageMode.Paged ? "Manual" : mode.ToString())));
				Assert.That(manualSize.Value, Is.EqualTo(23));
				Assert.That(manualSize.Visible, Is.EqualTo(mode == VisualGridPageMode.Paged));
				Assert.That(Find<Control>(grid, "_pageNavigationPanel").Visible, Is.EqualTo(mode != VisualGridPageMode.Single));
				if (mode == VisualGridPageMode.Single)
					Assert.That(grid.VisibleEntities.Count(), Is.EqualTo(120));
				else if (mode == VisualGridPageMode.Paged)
					Assert.That(grid.VisibleEntities.Count(), Is.EqualTo(23));

				if (mode == VisualGridPageMode.Auto) {
					ClickNative(selector);
					CaptureMenu(owner, selector, "grid-page-mode-auto-" + points + "pt");
					selector.HideDropDown();
				}
			}
		});
	}

	// Route actual native mouse messages through the existing toolbar/menu HWND, rather than invoking grid handlers.
	private static void ClickNative(ToolStripItem item) {
		var owner = item.Owner;
		var location = new Point(item.Bounds.Left + item.Width / 2, item.Bounds.Top + item.Height / 2);
		var point = (IntPtr)((location.Y << 16) | (location.X & 0xFFFF));
		WinAPI.USER32.SendMessage(new HandleRef(owner, owner.Handle), 0x0200, IntPtr.Zero, point);
		WinAPI.USER32.SendMessage(new HandleRef(owner, owner.Handle), WinAPI.USER32.WM_LBUTTONDOWN, (IntPtr)1, point);
		WinAPI.USER32.SendMessage(new HandleRef(owner, owner.Handle), WinAPI.USER32.WM_LBUTTONUP, IntPtr.Zero, point);
	}

	// Drain the grid's existing refresh throttle before checking visible query/navigation behavior.
	private static async Task RefreshAsync(VisualGrid grid) {
		await grid.RefreshGrid();
		await Task.Delay(300);
		await grid.RefreshGrid();
	}

	// Capture only owned native surfaces; unrelated foreground windows cannot occlude these previews.
	private static void CaptureMenu(Form form, ToolStripChoiceSelector selector, string name) {
		var directory = Environment.GetEnvironmentVariable("SPHERE10_UI_CAPTURE_DIRECTORY");
		if (string.IsNullOrWhiteSpace(directory))
			return;
		Directory.CreateDirectory(directory);
		using var surface = new Bitmap(form.Width, form.Height);
		form.DrawToBitmap(surface, new Rectangle(Point.Empty, form.Size));
		surface.Save(Path.Combine(directory, name + "-footer.png"), ImageFormat.Png);
		using var menu = new Bitmap(selector.DropDown.Width, selector.DropDown.Height);
		selector.DropDown.DrawToBitmap(menu, new Rectangle(Point.Empty, menu.Size));
		menu.Save(Path.Combine(directory, name + "-open-menu.png"), ImageFormat.Png);
	}

	// Named designer controls remain the same query/navigation owners as before the selector presentation changed.
	private static T Find<T>(Control owner, string name)
		where T : Control {
		return (T)owner.Controls.Find(name, true).Single();
	}

	// Keep construction, native input, asynchronous binding and disposal on one real Forms dispatcher.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form {
			Text = "CRUD grid page modes",
			ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual,
			Location = new Point(-20000, -20000),
			ClientSize = new Size(850, 400)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The native page-mode menu did not finish.");
			owner.Close();
		};
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
			completed = true;
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}
}
