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

/// <summary>Displays borrowed images with bounded previous and next navigation.</summary>
public partial class ImageNavigator : UserControlEx {
	private string _statusFormat = "{0} of {1}";
	private Image[] _images;
	private int _currentImageIndex = -1;

	public ImageNavigator() {
		// This call is required by the Windows.Forms Form Designer.
		InitializeComponent();

		SetStyle(ControlStyles.Selectable, true);
	}

	public string StatusFormat {
		get {
			return _statusFormat;
		}
		set {
			_statusFormat = value;
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Image[] Images {
		get {
			return _images;
		}
		set {
			_images = value;
			using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
				RefreshImageList();
			}
		}
	}

	public int CurrentImageIndex {
		get {
			return _currentImageIndex;
		}
		set {
			var changed = _currentImageIndex != value;
			_currentImageIndex = value;
			RefreshImage();
			if (changed)
				NotifyStateChanged();
		}
	}

	public Size ImageAreaSize {
		get {
			return _pictureBox.Size;
		}
		set {
			Width += value.Width - _pictureBox.Width;
			Height += value.Height - _pictureBox.Height;
		}
	}

	public System.Windows.Forms.BorderStyle ImageAreaBorderStyle {
		get {
			return _pictureBox.BorderStyle;
		}
		set {
			_pictureBox.BorderStyle = value;
		}
	}

	public PictureBoxSizeMode ImageAreaSizeMode {
		get {
			return _pictureBox.SizeMode;
		}
		set {
			_pictureBox.SizeMode = value;
		}
	}

	public void PreviousImage() {
		if (Images != null && _currentImageIndex > 0)
			CurrentImageIndex--;
	}

	public void NextImage() {
		if (Images != null && _currentImageIndex < (Images.Length - 1))
			CurrentImageIndex++;
	}

	protected override void OnLoad(EventArgs args) {
		base.OnLoad(args);

		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			RefreshImageList();
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

	private void RefreshImageList() {
		if (Images != null) {
			if (Images.Length > 0)
				CurrentImageIndex = 0;
			else
				CurrentImageIndex = -1;
		} else
			CurrentImageIndex = -1;

		RefreshImage();
	}

	private void RefreshImage() {
		if (_currentImageIndex < 0 || Images == null || _currentImageIndex >= Images.Length) {
			_pictureBox.Image = null;
			_statusLabel.Text = "";
			_nextButton.Enabled = false;
			_previousButton.Enabled = false;
		} else {
			_pictureBox.Image = Images[_currentImageIndex];
			_statusLabel.Text = string.Format(StatusFormat, _currentImageIndex + 1, Images.Length);

			_nextButton.Enabled = true;
			_previousButton.Enabled = true;
			if (_currentImageIndex == 0)
				_previousButton.Enabled = false;
			if (_currentImageIndex >= Images.Length - 1)
				_nextButton.Enabled = false;
		}
	}

	//		protected override void OnKeyDown(KeyEventArgs e)
	//		{
	//			if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Down || e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Space)
	//			{
	//				NextImage();
	//				e.Handled = true;
	//			}
	//			else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Down || e.KeyCode == Keys.PageDown || e.KeyCode == Keys.Back)
	//			{
	//				PreviousImage();
	//				e.Handled = true;
	//			}
	//
	//			base.OnKeyDown (e);
	//		}

	private void PreviousClick(object sender, System.EventArgs args) {
		PreviousImage();
	}

	private void NextClick(object sender, System.EventArgs args) {
		NextImage();
	}
}
