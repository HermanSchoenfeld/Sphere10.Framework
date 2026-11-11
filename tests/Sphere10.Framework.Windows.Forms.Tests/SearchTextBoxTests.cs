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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises the native search/clear action and its grid query integration.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class SearchTextBoxTests {
	private sealed record SearchRow(string Name);

	[Test]
	public void TypingShowsTheRedCrossAndNativeClickClearsThroughTextChanged() {
		RunWithMessageLoop(owner => {
			using var search = new SearchTextBox { Location = new Point(15, 15), Width = 220 };
			owner.Controls.Add(search);
			var changes = 0;
			var tooltip = (ToolTip)typeof(SearchTextBox).GetField("_actionToolTip", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(search);
			search.TextChanged += (_, _) => changes++;
			Assert.That(Pixels(search.PictureBox.Image), Is.EqualTo(Pixels(Resources.SmallSearchIcon)));
			Assert.That(tooltip.GetToolTip(search.PictureBox), Is.EqualTo("Enter search text."));
			search.Text = "Active filter";
			Assert.That(Pixels(search.PictureBox.Image), Is.EqualTo(Pixels(Resources.Cross)));
			Assert.That(search.PictureBox.Cursor, Is.SameAs(Cursors.Hand));
			Assert.That(search.PictureBox.AccessibleName, Is.EqualTo("Clear search"));
			Assert.That(tooltip.GetToolTip(search.PictureBox), Is.EqualTo("Clear the current search text."));
			ClickNative(search.PictureBox);
			Assert.That(search.Text, Is.Empty);
			Assert.That(changes, Is.EqualTo(2), "The clear action must use the same single text notification as ordinary edits.");
			Assert.That(search.Focused, Is.True);
			Assert.That(Pixels(search.PictureBox.Image), Is.EqualTo(Pixels(Resources.SmallSearchIcon)));
			Assert.That(search.PictureBox.Cursor, Is.SameAs(Cursors.Default));
			Assert.That(tooltip.GetToolTip(search.PictureBox), Is.EqualTo("Enter search text."));
			ClickNative(search.PictureBox);
			Assert.That(changes, Is.EqualTo(2), "Clicking the empty search icon must not invent a query change.");
			return Task.CompletedTask;
		});
	}

	[TestCase(9F, 80)]
	[TestCase(9F, 260)]
	[TestCase(12F, 80)]
	[TestCase(12F, 260)]
	[TestCase(16F, 80)]
	[TestCase(16F, 260)]
	public void ActionIsCenteredInsideTheNativeClientAreaAndReservesItsTextMargin(float fontSize, int width) {
		RunWithMessageLoop(owner => {
			using var font = new Font("Segoe UI", fontSize);
			using var search = new SearchTextBox { Font = font, Location = new Point(15, 15), Width = width };
			owner.Controls.Add(search);
			foreach (var text in new[] { string.Empty, "Search text that fills the available editor width" }) {
				search.Text = text;
				Assert.That(search.PictureBox.SizeMode, Is.EqualTo(PictureBoxSizeMode.CenterImage));
				Assert.That(search.PictureBox.Top, Is.Zero);
				Assert.That(search.PictureBox.Height, Is.EqualTo(search.ClientSize.Height));
				Assert.That(search.PictureBox.Right, Is.EqualTo(search.ClientSize.Width));
				Assert.That(search.ClientRectangle.Contains(search.PictureBox.Bounds), Is.True, "The action must not use negative offsets or extend past the editor client area.");
				Assert.That(RightMargin(search), Is.EqualTo(search.PictureBox.Width), "Native text must stop before the inline action.");
				Capture(search, $"search-{fontSize}-{width}-{(text.Length == 0 ? "empty" : "active")}");
			}
			search.Width += 70;
			Assert.That(search.PictureBox.Right, Is.EqualTo(search.ClientSize.Width));
			Assert.That(RightMargin(search), Is.EqualTo(search.PictureBox.Width));
			return Task.CompletedTask;
		});
	}

	[Test]
	public void RecreatedNativeHandleRetainsTheCurrentActionAndReservedTextMargin() {
		RunWithMessageLoop(owner => {
			using var search = new RecreatedSearchTextBox { Location = new Point(15, 15), Text = "Current query", Width = 220 };
			owner.Controls.Add(search);
			search.RecreateNativeHandle();
			Assert.That(Pixels(search.PictureBox.Image), Is.EqualTo(Pixels(Resources.Cross)));
			Assert.That(RightMargin(search), Is.EqualTo(search.PictureBox.Width));
			ClickNative(search.PictureBox);
			Assert.That(search.Text, Is.Empty);
			return Task.CompletedTask;
		});
	}

	[Test]
	public void DisposingTheSearchEditorDoesNotDisposeItsAssignedResourceImages() {
		RunWithMessageLoop(owner => {
			Image searchImage;
			Image clearImage;
			using (var first = new SearchTextBox()) {
				owner.Controls.Add(first);
				searchImage = first.PictureBox.Image;
				first.Text = "First filter";
				clearImage = first.PictureBox.Image;
			}
			using var second = new SearchTextBox();
			Assert.That(Pixels(second.PictureBox.Image), Is.EqualTo(Pixels(searchImage)));
			second.Text = "Second filter";
			Assert.That(Pixels(second.PictureBox.Image), Is.EqualTo(Pixels(clearImage)));
			return Task.CompletedTask;
		});
	}

	[Test]
	public void NativeClearActionRemovesTheVisualGridFilterAndRestoresAllRows() {
		RunWithMessageLoop(async owner => {
			using var grid = new VisualGrid {
				Dock = DockStyle.Fill,
				SuppressLoadingIndicator = true,
				GridBindings = new[] { new VisualGridColumn<SearchRow> { ColumnName = "Record", PropertyValue = value => value.Name, DataType = typeof(string) } }
			};
			owner.Controls.Add(grid);
			var source = new ArrayDataSource<SearchRow>();
			source.ReplaceData(new[] { new SearchRow("Keep one"), new SearchRow("Keep two"), new SearchRow("Other") });
			await grid.SetDataSource(source);
			await grid.RefreshGrid();
			var search = grid.Controls.Find("_searchTextBox", true).OfType<SearchTextBox>().Single();
			await ObserveBinding(grid, 2, () => search.Text = "Keep");
			Assert.That(grid.VisibleEntities.Cast<SearchRow>().Select(row => row.Name), Is.EqualTo(new[] { "Keep one", "Keep two" }));
			await grid.RefreshGrid();
			Assert.That(search.Enabled, Is.True);
			Assert.That(search.Visible, Is.True);
			Assert.That(search.PictureBox.Visible, Is.True);
			Assert.That(search.ClientRectangle.Contains(search.PictureBox.Bounds), Is.True, "The grid's parent autoscaling must leave the clear action inside its editor.");
			Assert.That(RightMargin(search), Is.EqualTo(search.PictureBox.Width));
			Capture(search, "grid-search-active");
			await ObserveBinding(grid, 3, () => {
				ClickNative(search.PictureBox);
				Assert.That(search.Text, Is.Empty, "The live grid's native clear action must remove the entered query.");
			});
			Assert.That(search.Text, Is.Empty);
			Assert.That(grid.VisibleEntities.Cast<SearchRow>().Select(row => row.Name), Is.EqualTo(new[] { "Keep one", "Keep two", "Other" }));
			Capture(search, "grid-search-empty");
		});
	}

	// Wait for the existing grid queries started by text changes or the native clear action.
	private static async Task ObserveBinding(VisualGrid grid, int expectedTotal, Action change) {
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		EventHandler onComplete = (_, _) => {
			if (grid.TotalRecords == expectedTotal)
				completion.TrySetResult();
		};
		grid.DataBindingComplete += onComplete;
		try {
			change();
			await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
		} finally {
			grid.DataBindingComplete -= onComplete;
		}
	}

	// Deliver real child-window mouse messages without moving the user's desktop pointer.
	private static void ClickNative(Control control) {
		var point = (IntPtr)(((control.Height / 2) << 16) | (control.Width / 2));
		var handle = new HandleRef(control, control.Handle);
		WinAPI.USER32.SendMessage(handle, 0x0200, IntPtr.Zero, point);
		WinAPI.USER32.SendMessage(handle, WinAPI.USER32.WM_LBUTTONDOWN, (IntPtr)1, point);
		WinAPI.USER32.SendMessage(handle, WinAPI.USER32.WM_LBUTTONUP, IntPtr.Zero, point);
	}

	// Inspect the actual native editor margin after layout or handle recreation.
	private static int RightMargin(SearchTextBox search) => (int)((WinAPI.USER32.SendMessage(search.Handle, 0x00D4, IntPtr.Zero, IntPtr.Zero).ToInt64() >> 16) & 0xFFFF);

	// Resources may materialize separate bitmap objects; compare their displayed pixels instead of object identity.
	private static int[] Pixels(Image image) {
		using var bitmap = new Bitmap(image);
		return Enumerable.Range(0, bitmap.Width * bitmap.Height).Select(index => bitmap.GetPixel(index % bitmap.Width, index / bitmap.Width).ToArgb()).ToArray();
	}

	// Save optional previews of the owned control, including both resource-image states.
	private static void Capture(SearchTextBox search, string name) {
		var directory = Environment.GetEnvironmentVariable("SPHERE10_UI_CAPTURE_DIRECTORY");
		if (string.IsNullOrWhiteSpace(directory))
			return;

		Directory.CreateDirectory(directory);
		using var bitmap = new Bitmap(search.Width, search.Height);
		search.DrawToBitmap(bitmap, new Rectangle(Point.Empty, search.Size));
		bitmap.Save(Path.Combine(directory, name + ".png"), ImageFormat.Png);
	}

	// Keep construction, native input, grid continuations and disposal on one owned Forms dispatcher.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form {
			ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual,
			Location = new Point(-20000, -20000),
			ClientSize = new Size(700, 350)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The native search action did not finish.");
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

	#region Inner Types

	// Expose the protected native lifecycle only for the handle-recreation regression.
	private sealed class RecreatedSearchTextBox : SearchTextBox {
		public void RecreateNativeHandle() => RecreateHandle();
	}

	#endregion
}
