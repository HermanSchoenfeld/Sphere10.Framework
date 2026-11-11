// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A drop-down list of image choices: the closed box shows the selected choice's image and text, and the drop-down is a tile picker.</summary>
/// <remarks>
/// The drop-down is the <see cref="CustomComboBox"/> popup hosting an owner-drawn tile view that this combo creates and disposes; choice images are
/// borrowed and never disposed. The native items mirror <see cref="Choices"/>, so Up/Down in the closed box move the selection natively. Sizes are
/// logical pixels scaled to the control's DPI; render thumbnails at <see cref="DeviceTileImageSize"/> so the tiles draw them unscaled.
/// </remarks>
public class ImageComboBox : CustomComboBox {
	/// <summary>Raised when the user commits a choice: a tile picked by click or Enter, or a keyboard selection in the closed box.</summary>
	/// <remarks>Never raised by <see cref="SelectedChoice"/> or <see cref="SetChoices"/> in code.</remarks>
	[Category("Behavior")]
	[Description("Raised when the user commits a choice by picking a tile or by selecting it with the keyboard.")]
	public event EventHandler SelectedChoiceCommitted;

	private const int LogicalDpi = 96;
	private const int LogicalItemPadding = 2;
	private const int LogicalTilePadding = 4;
	private const int LogicalGroupHeaderPadding = 8;
	private const int LogicalPickerSlack = 4;
	private const int MaximumNativeItemHeight = 255;
	private const TextFormatFlags CaptionFormat = TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

	// The list view draws each tile inset within its cell by this many logical pixels.
	private static readonly Size LogicalTileCellInset = new(4, 2);

	// Disabled images are drawn grey at half opacity.
	private static readonly ColorMatrix DisabledImageMatrix = ColorMatrices.FromArray(new float[,] {
		{ 0.299f, 0.299f, 0.299f, 0, 0 },
		{ 0.587f, 0.587f, 0.587f, 0, 0 },
		{ 0.114f, 0.114f, 0.114f, 0, 0 },
		{ 0, 0, 0, 0.5f, 0 },
		{ 0, 0, 0, 0, 1 }
	});

	private readonly TilePicker _tilePicker;
	private ImageComboBoxItem[] _choices = Array.Empty<ImageComboBoxItem>();
	private int _deviceDpi = LogicalDpi;

	/// <summary>Creates an empty combo with its owned tile picker; no popup opens during construction.</summary>
	public ImageComboBox() {
		_deviceDpi = DeviceDpi;
		_tilePicker = new TilePicker(this);

		// The item height is stored before owner drawing starts, so the native combo that owner drawing recreates measures its items at it.
		UpdateItemHeight();
		base.DrawMode = DrawMode.OwnerDrawFixed;
		DropDownControl = _tilePicker;
		DropDownSizeMode = SizeMode.UseDropDownSize;
		AllowResizeDropDown = false;
		UpdateDropDownSize();
	}

	/// <summary>The choices in display order; replace them with <see cref="SetChoices"/>.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IReadOnlyList<ImageComboBoxItem> Choices => Array.AsReadOnly(_choices);

	/// <summary>The selected choice, or null. Setting it in code never raises <see cref="SelectedChoiceCommitted"/>.</summary>
	/// <remarks>
	/// A choice that is not one of <see cref="Choices"/> selects the choice with an equal value, or equal text when its value is null; a choice
	/// with no such equivalent throws <see cref="ArgumentException"/>.
	/// </remarks>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ImageComboBoxItem SelectedChoice {
		get => SelectedItem as ImageComboBoxItem;
		set {
			var index = IndexOfChoice(value);
			Guard.Argument(value == null || index >= 0, nameof(value), "The choice is not one of the combo's choices.");
			SelectedIndex = index;
		}
	}

	/// <summary>The size of the selected choice's image in the closed box, in logical pixels.</summary>
	[Category("Appearance")]
	[DefaultValue(typeof(Size), "32, 20")]
	[Description("The size of the selected choice's image in the closed box, in logical pixels.")]
	public Size ImageSize {
		get => field;
		set {
			Guard.Argument(value.Width > 0 && value.Height > 0, nameof(value), "The image size must be positive.");
			if (field == value)
				return;
			field = value;
			UpdateItemHeight();
			Invalidate();
		}
	} = new(32, 20);

	/// <summary>The size of each tile's image in the drop-down, in logical pixels.</summary>
	[Category("Custom Drop-Down")]
	[DefaultValue(typeof(Size), "128, 80")]
	[Description("The size of each tile's image in the drop-down, in logical pixels.")]
	public Size TileImageSize {
		get => field;
		set {
			Guard.Argument(value.Width > 0 && value.Height > 0, nameof(value), "The tile image size must be positive.");
			if (field == value)
				return;
			field = value;
			UpdateDropDownSize();
		}
	} = new(128, 80);

	/// <summary><see cref="TileImageSize"/> at the control's DPI; render choice thumbnails at this size so the tiles draw them unscaled.</summary>
	[Browsable(false)]
	public Size DeviceTileImageSize => ScaleLogical(TileImageSize);

	/// <summary>The number of tiles in each row of the drop-down.</summary>
	[Category("Custom Drop-Down")]
	[DefaultValue(4)]
	[Description("The number of tiles in each row of the drop-down.")]
	public int TileColumns {
		get => field;
		set {
			Guard.ArgumentGTE(value, 1, nameof(value));
			if (field == value)
				return;
			field = value;
			UpdateDropDownSize();
		}
	} = 4;

	/// <summary>The number of tile rows the drop-down shows; further rows scroll.</summary>
	[Category("Custom Drop-Down")]
	[DefaultValue(3)]
	[Description("The number of tile rows the drop-down shows; further rows scroll.")]
	public int VisibleTileRows {
		get => field;
		set {
			Guard.ArgumentGTE(value, 1, nameof(value));
			if (field == value)
				return;
			field = value;
			UpdateDropDownSize();
		}
	} = 3;

	/// <summary>Always <see cref="System.Windows.Forms.DrawMode.OwnerDrawFixed"/>: the combo draws its closed box itself.</summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Browsable(false), ReadOnly(true)]
	public new DrawMode DrawMode {
		get => base.DrawMode;
		set {
		}
	}

	// The closed box's image size at the control's DPI.
	private Size DeviceImageSize => ScaleLogical(ImageSize);

	/// <summary>Replaces the choices, keeping the selection when a choice with an equal value, or equal text when its value is null, remains.</summary>
	/// <remarks>Otherwise nothing is selected. No <see cref="SelectedChoiceCommitted"/> is raised; the native index events follow the items.</remarks>
	public void SetChoices(IEnumerable<ImageComboBoxItem> choices) {
		Guard.ArgumentNotNull(choices, nameof(choices));
		var replacement = choices.ToArray();
		Guard.Argument(Array.TrueForAll(replacement, choice => choice != null), nameof(choices), "Choices cannot contain null.");
		var previous = SelectedChoice;
		var selectedIndex = previous == null ? -1 : Array.FindIndex(replacement, choice => IsEquivalentChoice(choice, previous));
		_choices = replacement;

		// The native items mirror the choices so Up/Down in the closed box move the selection natively.
		BeginUpdate();
		using (Tools.Scope.ExecuteOnDispose(static combo => combo.EndUpdate(), this)) {
			Items.Clear();
			Items.AddRange(replacement);
			SelectedIndex = selectedIndex;
		}

		// The picker's tiles follow, and groups change the drop-down's height.
		_tilePicker.SetTiles(replacement);
		UpdateDropDownSize();
		if (IsDroppedDown)
			_tilePicker.RevealTile(selectedIndex);
	}

	/// <summary>Opens the tile picker with the selected choice's tile selected, focused and scrolled into view.</summary>
	public override void ShowDropDown() {
		if (IsDroppedDown || IsDisposed)
			return;
		UpdateDropDownSize();
		base.ShowDropDown();
		if (IsDroppedDown)
			_tilePicker.RevealTile(SelectedIndex);
	}

	/// <summary>Draws the closed box: the selected choice's image fitted to <see cref="ImageSize"/>, then its text with an ellipsis.</summary>
	protected override void OnDrawItem(DrawItemEventArgs e) {
		e.DrawBackground();
		var choice = e.Index >= 0 && e.Index < Items.Count ? Items[e.Index] as ImageComboBoxItem : null;
		if (choice != null) {
			var enabled = (e.State & DrawItemState.Disabled) == 0;
			var padding = ScaleLogical(LogicalItemPadding);
			var imageSize = DeviceImageSize;
			var imageBox = new Rectangle(e.Bounds.Left + padding, e.Bounds.Top + (e.Bounds.Height - imageSize.Height) / 2, imageSize.Width, imageSize.Height);
			DrawFittedImage(e.Graphics, choice.Image, imageBox, enabled);
			var textBounds = Rectangle.FromLTRB(imageBox.Right + 2 * padding, e.Bounds.Top, e.Bounds.Right - padding, e.Bounds.Bottom);
			var textColor = enabled ? e.ForeColor : SystemColors.GrayText;
			TextRenderer.DrawText(e.Graphics, choice.Text, e.Font, textBounds, textColor, CaptionFormat | TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
		}
		e.DrawFocusRectangle();
		base.OnDrawItem(e);
	}

	/// <summary>Raises the native commit, then <see cref="SelectedChoiceCommitted"/>; the native combo raises it only for user selections.</summary>
	protected override void OnSelectionChangeCommitted(EventArgs e) {
		base.OnSelectionChangeCommitted(e);
		SelectedChoiceCommitted?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>Refits the closed box and the tiles to the new font.</summary>
	protected override void OnFontChanged(EventArgs e) {
		base.OnFontChanged(e);
		if (_tilePicker == null)
			return;
		UpdateItemHeight();
		UpdateDropDownSize();
	}

	/// <summary>Rescales the closed box, the tiles and the drop-down when the combo moves to a display with another DPI.</summary>
	protected override void RescaleConstantsForDpi(int deviceDpiOld, int deviceDpiNew) {
		base.RescaleConstantsForDpi(deviceDpiOld, deviceDpiNew);
		_deviceDpi = deviceDpiNew;
		if (_tilePicker == null)
			return;
		UpdateItemHeight();
		UpdateDropDownSize();
		Invalidate();
	}

	/// <summary>Releases the popup through the base combo, then disposes the owned tile picker; choice images stay with their owner.</summary>
	protected override void Dispose(bool disposing) {
		// CustomComboBox borrows its drop-down content; this combo created the picker, so it is disposed even when the base disposal fails.
		using var disposePicker = Tools.Scope.ExecuteOnDispose(() => {
			if (disposing)
				_tilePicker?.Dispose();
		});
		base.Dispose(disposing);
	}

	// A tile picked by click or Enter becomes the selection, closes the drop-down and is committed once, as a native user selection is.
	private void PickChoice(ImageComboBoxItem choice) {
		var index = Array.IndexOf(_choices, choice);
		if (index < 0)
			return;
		SelectedIndex = index;
		HideDropDown();
		OnSelectionChangeCommitted(EventArgs.Empty);
	}

	// The closed box fits the scaled image or a text line, whichever is taller, within the native limit. CustomComboBox hides ItemHeight,
	// so the native owner-draw height is set through the ComboBox property.
	private void UpdateItemHeight() {
		var comboBox = (ComboBox)this;
		var height = Math.Min(MaximumNativeItemHeight, Math.Max(DeviceImageSize.Height, Font.Height) + 2 * ScaleLogical(LogicalItemPadding));
		if (comboBox.ItemHeight != height)
			comboBox.ItemHeight = height;
	}

	// The picker is sized for TileColumns tiles across and VisibleTileRows rows down; further rows scroll.
	private void UpdateDropDownSize() {
		if (_tilePicker == null || _tilePicker.IsDisposed)
			return;
		var tileSize = GetTileSize();
		var slack = ScaleLogical(LogicalPickerSlack);
		var groupHeader = _tilePicker.ShowGroups ? Font.Height + ScaleLogical(LogicalGroupHeaderPadding) : 0;
		var scrollBar = SystemInformation.GetVerticalScrollBarWidthForDpi(_deviceDpi);
		_tilePicker.FollowOwnerFont();
		_tilePicker.TileSize = tileSize;
		DropSize = new Size(TileColumns * tileSize.Width + scrollBar + slack, VisibleTileRows * tileSize.Height + groupHeader + slack);
	}

	// A tile holds its image above a caption line, within padding; the list view insets each tile within its cell.
	private Size GetTileSize() {
		var image = DeviceTileImageSize;
		var padding = ScaleLogical(LogicalTilePadding);
		var inset = ScaleLogical(LogicalTileCellInset);
		return new Size(image.Width + 2 * padding + inset.Width, image.Height + Font.Height + 3 * padding + inset.Height);
	}

	// Code may select a choice by identity, or through an equivalent choice with the same value or text.
	private int IndexOfChoice(ImageComboBoxItem choice) {
		if (choice == null)
			return -1;
		var index = Array.IndexOf(_choices, choice);
		return index >= 0 ? index : Array.FindIndex(_choices, candidate => IsEquivalentChoice(candidate, choice));
	}

	private int ScaleLogical(int logical) => (int)Math.Round(logical * (double)_deviceDpi / LogicalDpi);

	private Size ScaleLogical(Size logical) => new Size(ScaleLogical(logical.Width), ScaleLogical(logical.Height));

	// Choices are identified by an equal value, or by equal text when the value is null.
	private static bool IsEquivalentChoice(ImageComboBoxItem candidate, ImageComboBoxItem choice)
		=> choice.Value != null ? Equals(candidate.Value, choice.Value) : candidate.Value == null && string.Equals(candidate.Text, choice.Text, StringComparison.Ordinal);

	// Draws a borrowed image centred in a box, keeping its aspect ratio; disabled images are drawn grey and faded.
	private static void DrawFittedImage(Graphics graphics, Image image, Rectangle box, bool enabled) {
		if (image == null || box.Width <= 0 || box.Height <= 0 || image.Width <= 0 || image.Height <= 0)
			return;
		var scale = Math.Min((double)box.Width / image.Width, (double)box.Height / image.Height);
		var width = Math.Max(1, (int)Math.Round(image.Width * scale));
		var height = Math.Max(1, (int)Math.Round(image.Height * scale));
		var target = new Rectangle(box.Left + (box.Width - width) / 2, box.Top + (box.Height - height) / 2, width, height);
		var state = graphics.Save();
		using var restore = Tools.Scope.ExecuteOnDispose(() => graphics.Restore(state));
		graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
		graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
		using var attributes = new ImageAttributes();

		// Mirrored edges keep bicubic filtering from blending the image border with transparency.
		attributes.SetWrapMode(WrapMode.TileFlipXY);
		if (!enabled)
			attributes.SetColorMatrix(DisabledImageMatrix);
		graphics.DrawImage(image, target, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
	}

	#region Inner Types

	/// <summary>The owner-drawn tile view hosted in the combo's drop-down: a click or Enter picks a tile and Esc closes the drop-down.</summary>
	private sealed class TilePicker : ListView {
		private readonly ImageComboBox _owner;
		private int _hoveredIndex = -1;

		public TilePicker(ImageComboBox owner) {
			_owner = owner;
			View = View.Tile;
			OwnerDraw = true;
			DoubleBuffered = true;
			MultiSelect = false;
			HideSelection = false;
			ShowItemToolTips = true;
			BorderStyle = BorderStyle.None;
		}

		// The picker always shows its owner's font, which is already scaled for the owner's display. The popup hosting the picker rescales
		// its content when it opens on a display with another DPI, which would shrink or grow that font a second time, so fonts assigned
		// to the picker are ignored.
		public override Font Font {
			get => _owner?.Font ?? base.Font;
			set {
			}
		}

		// Applies the owner's current font to the native list after the owner's font or DPI changed.
		public void FollowOwnerFont() => OnFontChanged(EventArgs.Empty);

		// Rebuilds the tiles, grouping them by caption in first-appearance order when any choice names a group.
		public void SetTiles(ImageComboBoxItem[] choices) {
			BeginUpdate();
			using var endUpdate = Tools.Scope.ExecuteOnDispose(static picker => picker.EndUpdate(), this);
			_hoveredIndex = -1;
			Items.Clear();
			Groups.Clear();
			ShowGroups = Array.Exists(choices, choice => !string.IsNullOrEmpty(choice.Group));
			var groups = new Dictionary<string, ListViewGroup>(StringComparer.Ordinal);
			foreach (var choice in choices) {
				var tile = new ListViewItem(choice.Text ?? string.Empty) { Tag = choice, ToolTipText = choice.ToolTipText ?? string.Empty };
				if (ShowGroups) {
					var caption = choice.Group ?? string.Empty;
					if (!groups.TryGetValue(caption, out var group)) {
						group = new ListViewGroup(caption);
						groups.Add(caption, group);
						Groups.Add(group);
					}
					tile.Group = group;
				}
				Items.Add(tile);
			}
		}

		// The selected choice's tile is selected, focused and scrolled into view when the drop-down opens.
		public void RevealTile(int index) {
			SelectedIndices.Clear();
			if (index < 0 || index >= Items.Count)
				return;
			var tile = Items[index];
			tile.Selected = true;
			tile.Focused = true;
			tile.EnsureVisible();
		}

		// Enter and Esc reach the picker instead of acting as dialog keys of the popup.
		protected override bool IsInputKey(Keys keyData) => keyData is Keys.Enter or Keys.Escape || base.IsInputKey(keyData);

		protected override void OnKeyDown(KeyEventArgs e) {
			base.OnKeyDown(e);
			if (e.Handled || e.Modifiers != Keys.None)
				return;
			var tile = FocusedItem ?? (SelectedItems.Count > 0 ? SelectedItems[0] : null);
			if (e.KeyCode == Keys.Enter && tile != null) {
				e.SuppressKeyPress = true;
				Pick(tile);
			} else if (e.KeyCode == Keys.Escape) {
				e.SuppressKeyPress = true;
				_owner.HideDropDown();
			}
		}

		protected override void OnMouseClick(MouseEventArgs e) {
			base.OnMouseClick(e);
			if (e.Button != MouseButtons.Left)
				return;
			var tile = GetItemAt(e.X, e.Y);
			if (tile != null)
				Pick(tile);
		}

		protected override void OnMouseMove(MouseEventArgs e) {
			base.OnMouseMove(e);
			SetHoveredIndex(GetItemAt(e.X, e.Y)?.Index ?? -1);
		}

		protected override void OnMouseLeave(EventArgs e) {
			base.OnMouseLeave(e);
			SetHoveredIndex(-1);
		}

		// Each tile shows its image at the combo's tile image size with its caption beneath, tinted while hovered or selected.
		protected override void OnDrawItem(DrawListViewItemEventArgs e) {
			base.OnDrawItem(e);
			if (e.Item.Tag is not ImageComboBoxItem choice)
				return;
			var graphics = e.Graphics;
			var bounds = e.Bounds;
			var selected = e.Item.Selected;
			using (var background = new SolidBrush(BackColor))
				graphics.FillRectangle(background, bounds);
			if (selected || e.ItemIndex == _hoveredIndex) {
				using var tint = new SolidBrush(Color.FromArgb(selected ? 72 : 32, SystemColors.Highlight));
				graphics.FillRectangle(tint, bounds);
			}
			if (selected) {
				using var frame = new Pen(SystemColors.Highlight);
				graphics.DrawRectangle(frame, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
			}

			// The image box keeps the configured size within the tile, and the caption line sits beneath it.
			var padding = _owner.ScaleLogical(LogicalTilePadding);
			var image = _owner.DeviceTileImageSize;
			var imageWidth = Math.Max(0, Math.Min(image.Width, bounds.Width - 2 * padding));
			var imageHeight = Math.Max(0, Math.Min(image.Height, bounds.Height - Font.Height - 3 * padding));
			var imageBox = new Rectangle(bounds.Left + (bounds.Width - imageWidth) / 2, bounds.Top + padding, imageWidth, imageHeight);
			DrawFittedImage(graphics, choice.Image, imageBox, Enabled);
			var caption = Rectangle.FromLTRB(bounds.Left + padding, imageBox.Bottom + padding, bounds.Right - padding, bounds.Bottom - padding);
			var captionColor = Enabled ? ForeColor : SystemColors.GrayText;
			TextRenderer.DrawText(graphics, choice.Text, Font, caption, captionColor, CaptionFormat | TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
			if (e.Item.Focused && ShowFocusCues)
				ControlPaint.DrawFocusRectangle(graphics, Rectangle.Inflate(bounds, -2, -2));
		}

		// Clicks and Enter share this path: the owner selects the tile's choice, closes the drop-down and commits.
		private void Pick(ListViewItem tile) {
			if (tile.Tag is ImageComboBoxItem choice)
				_owner.PickChoice(choice);
		}

		private void SetHoveredIndex(int index) {
			if (index == _hoveredIndex)
				return;
			InvalidateTile(_hoveredIndex);
			_hoveredIndex = index;
			InvalidateTile(index);
		}

		private void InvalidateTile(int index) {
			if (index >= 0 && index < Items.Count)
				Invalidate(Items[index].Bounds);
		}
	}

	#endregion
}
