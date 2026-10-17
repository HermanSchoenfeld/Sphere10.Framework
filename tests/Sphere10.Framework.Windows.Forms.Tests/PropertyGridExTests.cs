// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class PropertyGridExTests {
	[Test]
	public void CaptionSizingLeavesMostWidthForValues([Values(450, 900, 1600)] int width, [Values(9f, 18f)] float fontSize) => RunWithMessageLoop(async owner => {
		owner.ClientSize = new Size(width, 400);
		using var font = new Font(owner.Font.FontFamily, fontSize);
		using var grid = NewGrid(owner);
		grid.Font = font;
		var model = new LongCaptionSample();
		grid.SelectedObject = model;
		await owner.InvokeAsync(() => grid.PerformLayout());
		var native = NativeView(grid);
		var label = LabelWidth(native);
		Assert.That(label, Is.GreaterThan(30).And.LessThanOrEqualTo(Math.Min(320 * grid.DeviceDpi / 96.0, native.ClientSize.Width * 0.4) + 2));
		Assert.That(native.ClientSize.Width - label, Is.GreaterThan(native.ClientSize.Width * 0.58));
		Assert.That(grid.SelectedObject, Is.SameAs(model));
		Assert.That(grid.SelectedGridItem?.GridItemType, Is.EqualTo(GridItemType.Property), "Automatic fitting must preserve a usable native property selection.");
	});

	[Test]
	public void FittingTracksCaptionAndFontChangesWithoutRecreatingTheNativeView() => RunWithMessageLoop(async owner => {
		using var grid = NewGrid(owner);
		grid.SelectedObject = new ShortCaptionSample();
		await owner.InvokeAsync(() => grid.PerformLayout());
		var native = NativeView(grid);
		var handle = native.Handle;
		var shortWidth = LabelWidth(native);
		grid.SelectedObject = new LongCaptionSample();
		await owner.InvokeAsync(() => grid.PerformLayout());
		var longWidth = LabelWidth(native);
		Assert.That(longWidth, Is.GreaterThan(shortWidth + 30));
		using var largeFont = new Font(grid.Font.FontFamily, 18f);
		grid.Font = largeFont;
		await owner.InvokeAsync(() => grid.PerformLayout());
		Assert.That(LabelWidth(native), Is.GreaterThan(longWidth + 20));
		Assert.That(NativeView(grid), Is.SameAs(native));
		Assert.That(native.Handle, Is.EqualTo(handle));
	});

	[Test]
	public void NativeManualSplitterDragSurvivesResizeAndSnapshotReplacementUntilReset() => RunWithMessageLoop(async owner => {
		using var grid = NewGrid(owner);
		grid.SelectedObject = new ShortCaptionSample();
		await owner.InvokeAsync(() => grid.PerformLayout());
		var native = NativeView(grid);
		var initial = LabelWidth(native);
		var handle = native.Handle;
		var split = new Point(initial, 15);
		SendMouse(native, WinAPI.USER32.WM_LBUTTONDOWN, split, true);
		SendMouse(native, (int)WinAPI.WindowMessageFlags.WM_MOUSEMOVE, new Point(initial + 120, 15), true);
		SendMouse(native, WinAPI.USER32.WM_LBUTTONUP, new Point(initial + 120, 15), false);
		var manual = LabelWidth(native);
		Assert.That(manual, Is.GreaterThan(initial + 80), "The fixture must move the real native splitter before checking preservation.");
		var ratio = native.GetType().GetField("_labelRatio", BindingFlags.Public | BindingFlags.Instance).GetValue(native);
		owner.Width += 180;
		var replacement = new ShortCaptionSample();
		grid.SelectedObject = replacement;
		await owner.InvokeAsync(() => grid.PerformLayout());
		Assert.That(native.GetType().GetField("_labelRatio", BindingFlags.Public | BindingFlags.Instance).GetValue(native), Is.EqualTo(ratio));
		Assert.That(LabelWidth(native), Is.GreaterThan(initial + 80), "Automatic caption fitting must leave the user's splitter policy alone.");
		grid.ResetLabelColumnWidth();
		Assert.That(LabelWidth(native), Is.LessThan(manual - 40));
		Assert.That(LabelWidth(native), Is.LessThanOrEqualTo(Math.Max(initial + 5, native.ClientSize.Width / 9.0 + 5)), "The native PropertyGrid retains a minimum label fraction even for short captions.");
		Assert.That(grid.SelectedObject, Is.SameAs(replacement));
		Assert.That(native.Handle, Is.EqualTo(handle));
	});

	[Test]
	public void DefaultSizingAndLegacyReadonlySelectionPolicyRemainExplicit([Values(false, true)] bool readOnly) => RunWithMessageLoop(async owner => {
		using var grid = new SelectionGrid { Dock = DockStyle.Fill, ToolbarVisible = false, PropertySort = PropertySort.NoSort, Readonly = readOnly };
		owner.Controls.Add(grid);
		grid.SelectedObject = new ShortCaptionSample();
		await owner.InvokeAsync(() => grid.PerformLayout());
		var native = NativeView(grid);
		Assert.That(grid.AutoSizeLabelColumn, Is.False);
		using var ordinary = new PropertyGrid { Dock = DockStyle.Fill, ToolbarVisible = false, PropertySort = PropertySort.NoSort };
		owner.Controls.Add(ordinary);
		ordinary.SelectedObject = new ShortCaptionSample();
		await owner.InvokeAsync(() => ordinary.PerformLayout());
		Assert.That(NativeView(ordinary).ClientSize, Is.EqualTo(native.ClientSize), "The compatibility oracle must use the same native viewport geometry.");
		Assert.That(LabelWidth(native), Is.EqualTo(LabelWidth(NativeView(ordinary))), "Opting out must preserve the native PropertyGrid's actual default split, including its layout history.");
		var root = grid.SelectedGridItem;
		while (root.Parent != null)
			root = root.Parent;
		var property = root.GridItems.Cast<GridItem>().Last(item => item.GridItemType == GridItemType.Property);
		var notifications = 0;
		grid.SelectedGridItemChanged += (_, _) => notifications++;
		grid.SelectedGridItem = property;
		Assert.That(notifications, Is.GreaterThan(0), "Normal PropertyGrid selection events must reach consumers.");
		Assert.That(grid.SelectedGridItem, Is.SameAs(property), "The legacy marker must not clear native selection or reject its own property entries.");
		Assert.That(() => grid.NotifyEmptySelection(), Throws.Nothing);
	});

	[TestCase("DialogButton")]
	[TestCase("DropDownListBox")]
	public void NativeChildForwardedSplitterShortcutSurvivesReplacementAndResize(string editorProperty) => RunWithMessageLoop(async owner => {
		using var grid = NewGrid(owner);
		grid.SelectedObject = new ShortCaptionSample();
		await owner.InvokeAsync(() => grid.PerformLayout());
		var native = NativeView(grid);
		var initialWidth = LabelWidth(native);
		// Resolve the real lazily-created native editor, including its existing forwarding handler.
		var child = (Control)native.GetType().GetProperty(editorProperty, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(native);
		using var releaseDetachedList = editorProperty == "DropDownListBox" ? Tools.Scope.ExecuteOnDispose(child.Dispose) : null;
		if (editorProperty == "DropDownListBox") {
			// The native popup reparents the list outside the view. Losing editor focus is its discovery boundary.
			typeof(Control).GetMethod("OnLostFocus", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(native, new object[] { EventArgs.Empty });
		}
		var key = new KeyEventArgs(Keys.Control | Keys.Shift | Keys.Right);
		typeof(Control).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(child, new object[] { key });
		Assert.That(key.Handled, Is.True, "The fixture must exercise the native child forwarding handler.");
		Assert.That(LabelWidth(native), Is.GreaterThan(initialWidth), "The native child must move the splitter before preservation can be checked.");
		var ratio = native.GetType().GetField("_labelRatio", BindingFlags.Public | BindingFlags.Instance).GetValue(native);
		owner.Width += 180;
		grid.SelectedObject = new LongCaptionSample();
		await owner.InvokeAsync(() => grid.PerformLayout());
		Assert.That(native.GetType().GetField("_labelRatio", BindingFlags.Public | BindingFlags.Instance).GetValue(native), Is.EqualTo(ratio));
		var manual = LabelWidth(native);
		grid.ResetLabelColumnWidth();
		Assert.That(LabelWidth(native), Is.GreaterThan(manual + 20), "Reset must distinguish automatic fitting of longer captions from the retained native user split.");
	});

	private static PropertyGridEx NewGrid(Form owner) {
		var grid = new PropertyGridEx { Dock = DockStyle.Fill, AutoSizeLabelColumn = true, ToolbarVisible = false, PropertySort = PropertySort.NoSort };
		owner.Controls.Add(grid);
		return grid;
	}

	private static Control NativeView(PropertyGrid grid) => grid.Controls.Cast<Control>().Single(control => control.GetType().FullName == "System.Windows.Forms.PropertyGridInternal.PropertyGridView");

	private static int LabelWidth(Control native) => (int)native.GetType().GetProperty("LabelWidth", BindingFlags.Instance | BindingFlags.Public).GetValue(native);

	private static void SendMouse(Control view, int message, Point location, bool leftDown) => WinAPI.USER32.SendMessage(new HandleRef(view, view.Handle), (uint)message,
		leftDown ? (IntPtr)1 : IntPtr.Zero, (IntPtr)((location.Y << 16) | (location.X & 0xFFFF)));

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(900, 500) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 30000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("The native property-grid fixture timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try { await test(owner); completed = true; }
			catch (Exception error) { failure = error; }
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	private sealed class ShortCaptionSample {
		[DisplayName("Name")]
		public string Name => "Example";
		[DisplayName("Path")]
		public string Path => @"C:\Example\A Long Executable Path\Example.exe";
	}

	private sealed class LongCaptionSample {
		[DisplayName("Executable Description")]
		public string Description => "Example Program";
		[DisplayName("Command Line")]
		public string CommandLine => new('x', 400);
	}

	private sealed class SelectionGrid : PropertyGridEx {
		public void NotifyEmptySelection() => OnSelectedGridItemChanged(new SelectedGridItemChangedEventArgs(null, null));
	}
}
