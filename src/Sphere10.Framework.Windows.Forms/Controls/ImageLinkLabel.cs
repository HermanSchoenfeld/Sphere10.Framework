// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A ImageLinkLabel with Image support and round border support.
/// </summary>
[DefaultEvent("Click")]
public partial class ImageLinkLabel : UserControlEx {
	private Image _image;
	private Image _mouseOverImage;
	private Image _disabledImage;
	private System.Drawing.ContentAlignment _imageAlignment = System.Drawing.ContentAlignment.MiddleLeft;
	private StringFormat _stringFormat = new StringFormat(StringFormat.GenericDefault);
	private bool _alignTextToImage = true;
	private bool _imageStretch = false;
	private bool _enableMouseEffect = false;

	// Border
	private int _borderWidth = 0;
	private double _borderRound = 0;
	private Color _borderColor = Color.Black;
	private Color _backColor = Color.FromKnownColor(KnownColor.Control);
	private bool _isMouseOver = false;

	public ImageLinkLabel() {
		// This call is required by the Windows.Forms Form Designer.
		InitializeComponent();

		SetStyle(ControlStyles.SupportsTransparentBackColor, true);
		SetStyle(ControlStyles.StandardClick, true);
		SetStyle(ControlStyles.StandardDoubleClick, true);
		SetStyle(ControlStyles.ResizeRedraw, true);
		SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		SetStyle(ControlStyles.UserPaint, true);

		base.BackColor = Color.Transparent;
	}

	/// <summary>
	/// The default image to draw
	/// </summary>
	[DefaultValue(null)]
	public Image Image {
		get {
			return _image;
		}
		set {
			_image = value;
			Invalidate(true);
		}
	}

	/// <summary>
	/// Specifies the image to display when the mouse is inside the link area. If null is used the normal image.
	/// </summary>
	[DefaultValue(null)]
	public Image MouseOverImage {
		get {
			return _mouseOverImage;
		}
		set {
			_mouseOverImage = value;
		}
	}

	/// <summary>
	/// Specifies the image to display when the link is disabled.
	/// </summary>
	[DefaultValue(null)]
	public Image DisabledImage {
		get {
			return _disabledImage;
		}
		set {
			_disabledImage = value;
		}
	}

	[DefaultValue(System.Drawing.ContentAlignment.MiddleLeft)]
	public System.Drawing.ContentAlignment ImageAlignment {
		get {
			return _imageAlignment;
		}
		set {
			_imageAlignment = value;
			Invalidate(true);
		}
	}

	public System.Drawing.ContentAlignment TextAlignment {
		get {
			return Tools.Drawing.StringFormatToContentAlignment(_stringFormat);
		}
		set {
			Tools.Drawing.ApplyContentAlignmentToStringFormat(value, _stringFormat);
			Invalidate(true);
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public StringFormat StringFormat {
		get {
			return _stringFormat;
		}
		set {
			_stringFormat = value;
			Invalidate(true);
		}
	}

	[DefaultValue(true)]
	public bool AlignTextToImage {
		get {
			return _alignTextToImage;
		}
		set {
			_alignTextToImage = value;
			Invalidate(true);
		}
	}

	[DefaultValue(false)]
	public bool ImageStretch {
		get {
			return _imageStretch;
		}
		set {
			_imageStretch = value;
			Invalidate(true);
		}
	}

	[DefaultValue(false)]
	public bool EnableMouseEffect {
		get {
			return _enableMouseEffect;
		}
		set {
			_enableMouseEffect = value;
		}
	}

	[Browsable(true), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public override string Text {
		get {
			return base.Text;
		}
		set {
			base.Text = value;
		}
	}

	/// <summary>
	/// Gets or sets the width of the border. If 0 no border is drawed.
	/// </summary>
	[DefaultValue(0)]
	public int BorderWidth {
		get {
			return _borderWidth;
		}
		set {
			_borderWidth = value;
			Invalidate(true);
		}
	}

	/// <summary>
	/// Round amount. If 0 the border is a not rounded.
	/// </summary>
	[DefaultValue(0)]
	public double BorderRound {
		get {
			return _borderRound;
		}
		set {
			_borderRound = value;
			Invalidate(true);
		}
	}

	/// <summary>
	/// Border color.
	/// </summary>
	public Color BorderColor {
		get {
			return _borderColor;
		}
		set {
			_borderColor = value;
			Invalidate(true);
		}
	}

	public new Color BackColor {
		get {
			return _backColor;
		}
		set {
			_backColor = value;
			Invalidate();
		}
	}

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (_components != null)
				_components.Dispose();
		}
		base.Dispose(disposing);
	}

	protected override void OnMouseEnter(EventArgs args) {
		base.OnMouseEnter(args);
		_isMouseOver = true;
		Invalidate();
	}

	protected override void OnMouseLeave(EventArgs args) {
		base.OnMouseLeave(args);
		_isMouseOver = false;
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs args) {
		base.OnPaint(args);

		if (BorderRound > 0)
			args.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

		DrawBorderAndFill(args.Graphics);

		// Select the image for the current enabled and pointer state.
		var disabledImage = false;
		var image = _image;
		if (Enabled == false) {
			if (_disabledImage != null)
				image = _disabledImage;
			else
				disabledImage = true;
		} else if (_isMouseOver && _mouseOverImage != null)
			image = _mouseOverImage;

		// Lay out the text and image inside the visible border.
		var contentRect = ClientRectangle;
		if (BorderWidth > 0)
			contentRect = new Rectangle(contentRect.X + BorderWidth,
				contentRect.Y + BorderWidth,
				contentRect.Width - BorderWidth * 2,
				contentRect.Height - BorderWidth * 2);

		var container = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.Container();
		var textElement = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.TextGDI(Text);
		var imageElement = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.Image(image);
		imageElement.AnchorArea = new Sphere10.Framework.Drawing.AnchorArea(_imageAlignment, _imageStretch);
		imageElement.Enabled = !disabledImage;
		textElement.AnchorArea = new Sphere10.Framework.Drawing.AnchorArea(Tools.Drawing.StringFormatToContentAlignment(_stringFormat), false);
		textElement.StringFormat = _stringFormat;
		textElement.Font = Font;
		textElement.ForeColor = ForeColor;
		textElement.Enabled = Enabled;
		container.Elements.Add(imageElement);
		container.Elements.Add(textElement);

		// Draw both elements through the shared graphics cache.
		using (var cache = new Sphere10.Framework.Drawing.GraphicsCache(args.Graphics, args.ClipRectangle)) {
			container.Draw(cache, contentRect);
		}
	}

	protected override void OnTextChanged(EventArgs args) {
		base.OnTextChanged(args);

		Invalidate(true);
	}

	protected virtual void DrawBorderAndFill(Graphics graphics) {
		var borderWidth = BorderWidth;
		var borderRound = BorderRound;
		var borderColor = BorderColor;
		var fillColor = BackColor;

		// Apply the pointer highlight before drawing the fill and optional border.
		if (_enableMouseEffect && _isMouseOver && Enabled) {
			var highLight = Color.FromKnownColor(KnownColor.Highlight);
			fillColor = Color.FromArgb(75, highLight);
			if (borderWidth == 0)
				borderWidth = 1;
			borderColor = highLight;
		}

		var roundedRect = new Sphere10.Framework.Drawing.RoundedRectangle(ClientRectangle, borderRound);
		using (var brush = new SolidBrush(fillColor)) {
			Sphere10.Framework.GraphicsExtensions.FillRoundedRectangle(graphics, roundedRect, brush);
		}

		if (borderWidth > 0) {
			using (var pen = new Pen(borderColor, borderWidth)) {
				Sphere10.Framework.GraphicsExtensions.DrawRoundedRectangle(graphics, roundedRect, pen);
			}
		}
	}
}
