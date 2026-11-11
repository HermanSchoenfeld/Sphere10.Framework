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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises the image combo's choices, commits, metrics, painting and ownership, and the drop-down timing of the CustomComboBox it
/// builds on, without opening a popup or moving the pointer.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ImageComboBoxTests {
	private readonly record struct ColorArea(int Count, Rectangle Bounds);

	[Test]
	public void SetChoicesKeepsTheSelectionByValueAndClearsItWhenTheValueIsGone() {
		using var combo = new ImageComboBox();
		combo.SetChoices(new[] { Choice("Invert", 1), Choice("Sepia", 2), Choice("Grey", 3) });
		combo.SelectedChoice = combo.Choices[1];

		// Fresh choices with the same value keep the selection, even at another position, and the native items mirror them.
		var refreshed = new[] { Choice("Grey", 3), Choice("Warm sepia", 2) };
		combo.SetChoices(refreshed);
		Assert.That(combo.SelectedChoice, Is.SameAs(refreshed[1]));
		Assert.That(combo.Choices, Is.EqualTo(refreshed));
		Assert.That(combo.Items.Cast<object>(), Is.EqualTo(refreshed));

		// Without the selected value nothing stays selected.
		combo.SetChoices(new[] { Choice("Grey", 3), Choice("Invert", 1) });
		Assert.That(combo.SelectedChoice, Is.Null);
		Assert.That(combo.SelectedIndex, Is.EqualTo(-1));

		// Choices without a value are matched by their text.
		var untitled = new[] { Choice("Off", null), Choice("On", null) };
		combo.SetChoices(untitled);
		combo.SelectedChoice = untitled[1];
		var replacement = Choice("On", null);
		combo.SetChoices(new[] { Choice("Off", 0), replacement });
		Assert.That(combo.SelectedChoice, Is.SameAs(replacement));
	}

	[Test]
	public void SelectingInCodeRaisesNoCommit() {
		using var combo = new ImageComboBox();
		combo.CreateControl();
		var commits = 0;
		combo.SelectedChoiceCommitted += (_, _) => commits++;
		var choices = new[] { Choice("Invert", 1), Choice("Sepia", 2) };
		combo.SetChoices(choices);
		combo.SelectedChoice = choices[1];
		Assert.That(combo.SelectedChoice, Is.SameAs(choices[1]));

		// An equivalent choice selects the offered one; a choice that is not offered is rejected.
		combo.SelectedChoice = Choice("Invert", 1);
		Assert.That(combo.SelectedChoice, Is.SameAs(choices[0]));
		Assert.That(() => combo.SelectedChoice = Choice("Unknown", 9), Throws.ArgumentException);
		combo.SelectedChoice = null;
		combo.SetChoices(new[] { choices[1], choices[0] });
		Assert.That(commits, Is.Zero);
	}

	[Test]
	public void EnterOnATilePicksItClosesTheDropDownAndCommitsOnce() {
		using var combo = new RecordingImageComboBox();
		var choices = new[] { Choice("Invert", 1), Choice("Sepia", 2), Choice("Grey", 3) };
		combo.SetChoices(choices);
		var commits = 0;
		var nativeCommits = 0;
		ImageComboBoxItem committed = null;
		combo.SelectedChoiceCommitted += (_, _) => {
			commits++;
			committed = combo.SelectedChoice;
		};
		combo.SelectionChangeCommitted += (_, _) => nativeCommits++;

		// Enter on the focused tile of the owned picker takes the pick path a click takes, without the popup being shown.
		var picker = (ListView)combo.DropDownControl;
		FocusTile(picker, 2);
		SendKey(picker, Keys.Enter);
		Assert.That(combo.SelectedChoice, Is.SameAs(choices[2]));
		Assert.That(committed, Is.SameAs(choices[2]));
		Assert.That(commits, Is.EqualTo(1));
		Assert.That(nativeCommits, Is.EqualTo(1));
		Assert.That(combo.HideRequests, Is.EqualTo(1));
		Assert.That(combo.IsDroppedDown, Is.False);

		// Esc closes the drop-down without changing or committing the selection.
		FocusTile(picker, 0);
		SendKey(picker, Keys.Escape);
		Assert.That(combo.SelectedChoice, Is.SameAs(choices[2]));
		Assert.That(commits, Is.EqualTo(1));
		Assert.That(combo.HideRequests, Is.EqualTo(2));
	}

	[Test]
	public void DownInTheClosedBoxSelectsTheNextChoiceNativelyAndCommitsIt() {
		using var parent = new Panel { Size = new Size(300, 80) };
		var combo = new ImageComboBox { Width = 240 };
		parent.Controls.Add(combo);
		var choices = new[] { Choice("Invert", 1), Choice("Sepia", 2), Choice("Grey", 3) };
		combo.SetChoices(choices);
		combo.SelectedChoice = choices[0];
		var commits = 0;
		ImageComboBoxItem committed = null;
		combo.SelectedChoiceCommitted += (_, _) => {
			commits++;
			committed = combo.SelectedChoice;
		};

		// The native combo moves the selection itself and reports the change as committed by the user.
		SendKey(combo, Keys.Down);
		Assert.That(combo.SelectedChoice, Is.SameAs(choices[1]));
		Assert.That(committed, Is.SameAs(choices[1]));
		Assert.That(commits, Is.EqualTo(1));
	}

	[Test]
	public void TilesMirrorTheChoicesWithTheirToolTipsAndGroups() {
		using var combo = new ImageComboBox();
		var picker = (ListView)combo.DropDownControl;
		combo.SetChoices(new[] { Choice("Invert", 1), Choice("Sepia", 2) });
		Assert.That(picker.Items.Cast<ListViewItem>().Select(tile => tile.Text), Is.EqualTo(new[] { "Invert", "Sepia" }));
		Assert.That(picker.ShowGroups, Is.False);

		// Any group caption groups the tiles in first-appearance order.
		combo.SetChoices(new[] {
			new ImageComboBoxItem("Invert", null, 1) { Group = "Colour", ToolTipText = "Inverts every colour" },
			new ImageComboBoxItem("Sepia", null, 2) { Group = "Tone" },
			new ImageComboBoxItem("Grey", null, 3) { Group = "Colour" }
		});
		Assert.That(picker.ShowGroups, Is.True);
		Assert.That(picker.Groups.Cast<ListViewGroup>().Select(group => group.Header), Is.EqualTo(new[] { "Colour", "Tone" }));
		Assert.That(picker.Items[2].Group.Header, Is.EqualTo("Colour"));
		Assert.That(picker.Items[0].ToolTipText, Is.EqualTo("Inverts every colour"));
	}

	[Test]
	public void ItemHeightFollowsTheImageSizeAndTheFont() {
		using var font = new Font(FontFamily.GenericSansSerif, 32f, GraphicsUnit.Pixel);
		using var combo = new DpiImageComboBox();
		combo.SimulateDpi(96);
		const int padding = 2 * 2;
		Assert.That(combo.ItemHeight, Is.EqualTo(Math.Max(20, combo.Font.Height) + padding));
		combo.ImageSize = new Size(48, 36);
		Assert.That(combo.ItemHeight, Is.EqualTo(Math.Max(36, combo.Font.Height) + padding));

		// A font taller than the image sets the height, and the DPI scales the image and the padding.
		combo.Font = font;
		Assert.That(font.Height, Is.GreaterThan(36));
		Assert.That(combo.ItemHeight, Is.EqualTo(font.Height + padding));
		combo.SimulateDpi(192);
		Assert.That(combo.ItemHeight, Is.EqualTo(Math.Max(72, font.Height) + 2 * padding));
	}

	[Test]
	public void DeviceTileImageSizeAndTheDropDownFollowTheDpiAndTheTileGrid() {
		using var combo = new DpiImageComboBox();
		combo.SimulateDpi(96);
		Assert.That(combo.DeviceTileImageSize, Is.EqualTo(new Size(128, 80)));
		var dropAt96 = combo.DropSize;
		combo.SimulateDpi(144);
		Assert.That(combo.DeviceTileImageSize, Is.EqualTo(new Size(192, 120)));
		combo.SimulateDpi(192);
		Assert.That(combo.DeviceTileImageSize, Is.EqualTo(new Size(256, 160)));
		Assert.That(combo.DropSize.Width, Is.GreaterThan(dropAt96.Width * 3 / 2));
		Assert.That(combo.DropSize.Height, Is.GreaterThan(dropAt96.Height));

		// The drop-down holds TileColumns tiles across and VisibleTileRows rows down.
		combo.SimulateDpi(96);
		combo.TileColumns = 2;
		Assert.That(combo.DropSize.Width, Is.LessThan(dropAt96.Width * 2 / 3));
		combo.VisibleTileRows = 6;
		Assert.That(combo.DropSize.Height, Is.GreaterThan(dropAt96.Height * 3 / 2));
		Assert.That(combo.DropDownControl.Size, Is.EqualTo(combo.DropSize));
	}

	[Test]
	public void ClosedBoxDrawsTheSelectedChoiceImageInTheImageArea() {
		var red = Color.FromArgb(220, 30, 30);
		var blue = Color.FromArgb(30, 60, 220);
		using var redImage = SolidImage(red, 64, 40);
		using var blueImage = SolidImage(blue, 64, 40);
		using var host = new OffscreenHostForm();
		var combo = new ImageComboBox { Location = new Point(8, 8), Width = 260 };
		host.Controls.Add(combo);
		var choices = new[] { new ImageComboBoxItem("Red", redImage, 1), new ImageComboBoxItem("Blue", blueImage, 2) };
		combo.SetChoices(choices);
		combo.SelectedChoice = choices[0];

		// The native combo draws its owner-drawn box only while its window is visible, so the host is shown off screen without activation.
		host.Show();

		// The selected image is fitted into the image box at the leading edge of the closed box; the other image is not drawn.
		using (var bitmap = Render(combo)) {
			var redArea = FindColor(bitmap, red);
			Assert.That(redArea.Count, Is.GreaterThanOrEqualTo(32 * 20 / 2));
			Assert.That(redArea.Bounds.Right, Is.LessThanOrEqualTo(combo.Width / 3));
			Assert.That(Math.Abs(redArea.Bounds.Top + redArea.Bounds.Bottom - combo.Height), Is.LessThanOrEqualTo(4), "The image is centred vertically.");
			Assert.That(FindColor(bitmap, blue).Count, Is.Zero);
		}
		combo.SelectedChoice = choices[1];
		using (var bitmap = Render(combo)) {
			Assert.That(FindColor(bitmap, blue).Count, Is.GreaterThanOrEqualTo(32 * 20 / 2));
			Assert.That(FindColor(bitmap, red).Count, Is.Zero);
		}
	}

	[Test]
	public void DisposingTheComboDisposesItsPickerButNotTheChoiceImages() {
		using var image = SolidImage(Color.Orange, 16, 10);
		var combo = new ImageComboBox();
		combo.SetChoices(new[] { new ImageComboBoxItem("Orange", image, 1) });
		combo.SelectedIndex = 0;
		var picker = combo.DropDownControl;
		Assert.That(picker, Is.InstanceOf<ListView>());
		combo.Dispose();
		Assert.That(picker.IsDisposed, Is.True);
		Assert.That(image.GetPixel(0, 0).ToArgb(), Is.EqualTo(Color.Orange.ToArgb()), "The borrowed image remains usable.");
		Assert.That(() => combo.Dispose(), Throws.Nothing);
	}

	// The popup rescales its content when it opens on a display with another DPI; the picker must keep its owner's already scaled font.
	[Test]
	public void ThePickerKeepsItsOwnersFontWhenItsHostRescalesIt() {
		using var font = new Font(FontFamily.GenericSansSerif, 14f, GraphicsUnit.Pixel);
		using var rescaled = new Font(FontFamily.GenericSansSerif, 9f, GraphicsUnit.Pixel);
		using var combo = new ImageComboBox { Font = font };
		var picker = combo.DropDownControl;

		picker.Font = rescaled;

		Assert.That(picker.Font, Is.SameAs(combo.Font));
	}

	// CustomComboBox compared only the millisecond component, so a click 3.020 s after the close was taken for the closing click itself.
	[Test]
	public void ClickShowsTheDropDownOnceTheClosingClickHasPassed() {
		using var combo = new RecordingImageComboBox();
		var closedAt = new DateTime(2026, 11, 9, 10, 0, 0, DateTimeKind.Local);
		SetLastHideTime(combo, closedAt);
		DecideClick(combo, closedAt.AddMilliseconds(3020));
		Assert.That(combo.ShowRequests, Is.EqualTo(1), "A click 3.020 s after the close shows the drop-down.");
		DecideClick(combo, closedAt.AddMilliseconds(20));
		Assert.That(combo.ShowRequests, Is.EqualTo(1), "A click 20 ms after the close is the click that closed it.");

		// A real button-down message reaches the same decision.
		SetLastHideTime(combo, DateTime.Now.AddMilliseconds(-3020));
		WinAPI.USER32.SendMessage(combo.Handle, (uint)WinAPI.USER32.WM_LBUTTONDOWN, IntPtr.Zero, IntPtr.Zero);
		Assert.That(combo.ShowRequests, Is.EqualTo(2));
	}

	// The native close-up check compared only the seconds component, so a close-up 61 s after opening was taken for the opening one.
	[Test]
	public void NativeCloseUpHidesTheDropDownOnlyAfterTheOpeningSecond() {
		using var combo = new RecordingImageComboBox();
		var showTime = typeof(CustomComboBox).GetField("_sShowTime", BindingFlags.Static | BindingFlags.NonPublic);
		var previous = showTime.GetValue(null);
		using var restore = Tools.Scope.ExecuteOnDispose(() => showTime.SetValue(null, previous));
		showTime.SetValue(null, DateTime.Now);
		SendCloseUp(combo);
		Assert.That(combo.HideRequests, Is.Zero, "The close-up right after opening keeps the drop-down.");
		showTime.SetValue(null, DateTime.Now.AddSeconds(-61));
		SendCloseUp(combo);
		Assert.That(combo.HideRequests, Is.EqualTo(1));
	}

	private static ImageComboBoxItem Choice(string text, object value) => new ImageComboBoxItem(text, null, value);

	private static Bitmap SolidImage(Color color, int width, int height) {
		var image = new Bitmap(width, height, PixelFormat.Format32bppArgb);
		using var graphics = Graphics.FromImage(image);
		graphics.Clear(color);
		return image;
	}

	// An opaque surface keeps GDI fills, which leave no alpha on an ARGB bitmap.
	private static Bitmap Render(Control control) {
		var bitmap = new Bitmap(control.Width, control.Height, PixelFormat.Format32bppRgb);
		control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
		return bitmap;
	}

	private static ColorArea FindColor(Bitmap bitmap, Color color) {
		var count = 0;
		var bounds = Rectangle.Empty;
		for (var y = 0; y < bitmap.Height; y++)
			for (var x = 0; x < bitmap.Width; x++) {
				if (bitmap.GetPixel(x, y).ToArgb() != color.ToArgb())
					continue;
				count++;
				bounds = bounds.IsEmpty ? new Rectangle(x, y, 1, 1) : Rectangle.Union(bounds, new Rectangle(x, y, 1, 1));
			}
		return new ColorArea(count, bounds);
	}

	private static void FocusTile(ListView picker, int index) {
		picker.CreateControl();
		picker.Items[index].Selected = true;
		picker.Items[index].Focused = true;
	}

	// A key message sent to the window runs the control's key handling without the real keyboard or pointer.
	private static void SendKey(Control control, Keys key) => WinAPI.USER32.SendMessage(control.Handle, (uint)WinAPI.USER32.WM_KEYDOWN, (IntPtr)(int)key, IntPtr.Zero);

	private static void SendCloseUp(Control combo)
		=> WinAPI.USER32.SendMessage(combo.Handle, (uint)(WinAPI.USER32.WM_REFLECT + WinAPI.USER32.WM_COMMAND), (IntPtr)(WinAPI.USER32.CBN_CLOSEUP << 16), IntPtr.Zero);

	// Reflection is limited to the private drop-down timing of CustomComboBox, which has no public seam.
	private static void SetLastHideTime(CustomComboBox combo, DateTime closedAt)
		=> typeof(CustomComboBox).GetField("_lastHideTime", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(combo, closedAt);

	private static void DecideClick(CustomComboBox combo, DateTime clickedAt) {
		var decide = typeof(CustomComboBox).GetMethod("AutoDropDown", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(DateTime) }, null);
		decide.Invoke(combo, new object[] { clickedAt });
	}

	#region Inner Types

	// Records drop-down requests instead of opening the popup.
	private sealed class RecordingImageComboBox : ImageComboBox {
		public int ShowRequests { get; private set; }

		public int HideRequests { get; private set; }

		public override void ShowDropDown() => ShowRequests++;

		public override void HideDropDown() {
			HideRequests++;
			base.HideDropDown();
		}
	}

	// A shown window far off screen that never activates, so it neither takes the focus nor sits under the pointer.
	private sealed class OffscreenHostForm : Form {
		public OffscreenHostForm() {
			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			Bounds = new Rectangle(-30000, -30000, 400, 120);
		}

		protected override bool ShowWithoutActivation => true;

		protected override CreateParams CreateParams {
			get {
				var parameters = base.CreateParams;
				parameters.ExStyle |= WinAPI.USER32.WS_EX_TOOLWINDOW | WinAPI.USER32.WS_EX_NOACTIVATE;
				return parameters;
			}
		}
	}

	// Drives the DPI rescaling a monitor change performs, without moving a window between displays.
	private sealed class DpiImageComboBox : ImageComboBox {
		private int _dpi;

		public DpiImageComboBox() => _dpi = DeviceDpi;

		public void SimulateDpi(int dpi) {
			RescaleConstantsForDpi(_dpi, dpi);
			_dpi = dpi;
		}
	}

	#endregion
}
