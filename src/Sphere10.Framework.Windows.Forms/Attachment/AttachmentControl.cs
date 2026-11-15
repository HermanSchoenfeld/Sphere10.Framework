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
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A compact attachment button and filename. Changes in its picker are accepted only with OK.</summary>
public class AttachmentControl : UserControlEx {
	public event EventHandler AttachmentChanged;

	private readonly Button _button;
	private readonly LinkLabel _fileName;
	private AttachmentSelection _selection;
	private bool _editing;

	public AttachmentControl() {
		_button = new AttachmentButton { Name = "AttachmentButton", AccessibleName = "Add Attachment", TabIndex = 0, UseVisualStyleBackColor = true };
		_fileName = new LinkLabel { Name = "AttachmentFileName", AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft, TabIndex = 1 };
		Controls.Add(_button);
		Controls.Add(_fileName);
		_button.Click += EditAttachment;
		_fileName.LinkClicked += EditAttachment;
		AutoScaleMode = AutoScaleMode.Dpi;
		Size = new Size(300, 30);
		UpdateSelection();
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool HasAttachment => _selection != null;

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string FileName => _selection?.FileName ?? string.Empty;

	/// <summary>Copies the file contents; later changes to the source or clipboard cannot change the attachment.</summary>
	public void SetAttachment(string fileName, byte[] data) {
		_selection = new AttachmentSelection(fileName, data);
		UpdateSelection();
		AttachmentChanged?.Invoke(this, EventArgs.Empty);
	}

	public void ClearAttachment() {
		_selection = null;
		UpdateSelection();
		AttachmentChanged?.Invoke(this, EventArgs.Empty);
	}

	public bool TryGetAttachment(out string fileName, out byte[] data) {
		fileName = _selection?.FileName;
		data = _selection?.CopyBytes();
		return _selection != null;
	}

	public async Task EditAsync() {
		if (_editing)
			return;
		_editing = true;
		using var editing = Tools.Scope.ExecuteOnDispose(() => _editing = false);
		using var dialog = new AttachmentPickerDialog(_selection);
		if (await dialog.ShowDialogAsync(FindForm()) != DialogResult.OK)
			return;
		_selection = dialog.Selection;
		UpdateSelection();
		AttachmentChanged?.Invoke(this, EventArgs.Empty);
	}

	public override Size GetPreferredSize(Size proposedSize) => new(proposedSize.Width > 0 ? proposedSize.Width : Width, LogicalToDeviceUnits(30));

	protected override void OnHandleCreated(EventArgs e) {
		base.OnHandleCreated(e);
		Height = LogicalToDeviceUnits(30);
	}

	protected override void OnDpiChangedAfterParent(EventArgs e) {
		base.OnDpiChangedAfterParent(e);
		Height = LogicalToDeviceUnits(30);
	}

	protected override void OnLayout(LayoutEventArgs e) {
		base.OnLayout(e);
		if (_button == null)
			return;
		var side = LogicalToDeviceUnits(28);
		_button.Bounds = new Rectangle(0, Math.Max(0, (Height - side) / 2), side, side);
		var left = _button.Right + LogicalToDeviceUnits(8);
		_fileName.Bounds = new Rectangle(left, 0, Math.Max(0, Width - left), Height);
	}

	private async void EditAttachment(object sender, EventArgs e) => await EditAsync();

	private void UpdateSelection() {
		_button.AccessibleName = HasAttachment ? "Change Attachment" : "Add Attachment";
		_fileName.Text = FileName;
		_fileName.AccessibleName = HasAttachment ? $"Change Attachment: {FileName}" : "No Attachment";
		_fileName.TabStop = HasAttachment;
	}

	#region Inner Types

	private sealed class AttachmentButton : Button {
		protected override void OnPaint(PaintEventArgs e) {
			base.OnPaint(e);
			var state = e.Graphics.Save();
			using var restore = Tools.Scope.ExecuteOnDispose(() => e.Graphics.Restore(state));
			var side = Math.Min(Width, Height) * .58F;
			e.Graphics.TranslateTransform((Width - side) / 2, (Height - side) / 2);
			e.Graphics.ScaleTransform(side / 20, side / 20);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			using var pen = new Pen(Enabled ? ForeColor : SystemColors.GrayText, 1.6F) { StartCap = LineCap.Round, EndCap = LineCap.Round };
			using var path = new GraphicsPath();
			path.AddBezier(13, 4, 18, -1, 23, 5, 17, 10);
			path.AddLine(17, 10, 9, 18);
			path.AddBezier(9, 18, 3, 24, -2, 17, 3, 12);
			path.AddLine(3, 12, 12, 3);
			path.AddBezier(12, 3, 15, 0, 19, 4, 16, 7);
			path.AddLine(16, 7, 7, 16);
			path.AddBezier(7, 16, 5, 18, 3, 15, 5, 13);
			path.AddLine(5, 13, 12, 6);
			e.Graphics.DrawPath(pen, path);
		}
	}

	#endregion
}
