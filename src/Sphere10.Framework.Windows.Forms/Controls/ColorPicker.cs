// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Displays a selected color and opens the native color-selection dialog.</summary>
public partial class ColorPicker : EditableControl {
	public event EventHandler SelectedColorChanged;

	public ColorPicker() {
		// This call is required by the Windows.Forms Form Designer.
		InitializeComponent();

		SelectedColor = Color.Black;
	}

	public virtual Color SelectedColor {
		get {
			return _colorPanel.BackColor;
		}
		set {
			_colorPanel.BackColor = value;
		}
	}

	public new Color ForeColor {
		get {
			return _colorLabel.ForeColor;
		}
		set {
			_colorLabel.ForeColor = value;
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

	private void PanelColorBackColorChanged(object sender, System.EventArgs args) {
		_colorLabel.Text = _colorPanel.BackColor.Name;

		if (SelectedColorChanged != null)
			SelectedColorChanged(this, args);
		NotifyStateChanged();
	}

	private void BrowseClick(object sender, System.EventArgs args) {
		try {
			using (var dialog = new ColorDialog()) {
				dialog.Color = SelectedColor;
				if (dialog.ShowDialog(this) == DialogResult.OK)
					SelectedColor = dialog.Color;
			}
		} catch (Exception) {
		}
	}
}
