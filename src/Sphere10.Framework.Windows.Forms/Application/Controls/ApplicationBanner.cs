// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;


namespace Sphere10.Framework.Windows.Forms;

public partial class ApplicationBanner : ApplicationControl {
	private string _companyName;
	private string _version;
	private string _title;
	private Image _iconImage;

	public ApplicationBanner()
		: base() {
		InitializeComponent();

		// Download child control defaults as defaults at construction
		CompanyName = _companyNameLabel.Text;
		Version = _versionLabel.Text;
		Title = _productNameLabel.Text;
		ApplicationIcon = Tools.Values.Future.LazyLoad(() => Sphere10Framework.Instance.ServiceProvider.GetService<IApplicationIconProvider>()?.ApplicationIcon);
		Disposables.Add(() => {
			_iconImage?.Dispose();
			_iconImage = null;
		});
	}

	protected IFuture<Icon> ApplicationIcon { get; }
	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
	}

	protected override void InitializeUIPrimingData() {
		base.InitializeUIPrimingData();
		RenderApplicationIcon();
		// Upload to child controls the processed strings. Visual inheritance can be used to change these strings.
		_companyNameLabel.Text = Sphere10.Framework.StringFormatter.FormatEx(CompanyName);
		_productNameLabel.Text = Sphere10.Framework.StringFormatter.FormatEx(Title);
		_versionLabel.Text = Sphere10.Framework.StringFormatter.FormatEx(Version);
	}


	[Category("Appearance")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public new string CompanyName {
		get { return _companyName; }
		set {
			_companyName = value;
			if (Tools.Runtime.IsDesignMode) {
				_companyNameLabel.Text = _companyName;
			}
			if (Loaded) {
				_companyNameLabel.Text = Sphere10.Framework.StringFormatter.FormatEx(_companyName);
			}
			Invalidate();
		}
	}

	[Category("Appearance")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public string Version {
		get { return _version; }
		set {
			_version = value;
			if (Tools.Runtime.IsDesignMode) {
				_versionLabel.Text = _version;
			}
			if (Loaded) {
				_versionLabel.Text = Sphere10.Framework.StringFormatter.FormatEx(_version);
			}
			Invalidate();
		}
	}

	[Category("Appearance")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public string Title {
		get { return _title; }
		set {
			_title = value;
			if (Tools.Runtime.IsDesignMode) {
				_productNameLabel.Text = _title;
			}
			if (Loaded) {
				_productNameLabel.Text = Sphere10.Framework.StringFormatter.FormatEx(_title);
			}
			Invalidate();
		}
	}

	[Category("Appearance")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Color FromColor {
		get { return _gradientPanel.FromColor; }
		set {
			_gradientPanel.FromColor = value;
			Invalidate();
		}
	}

	[Category("Appearance")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public Color ToColor {
		get { return _gradientPanel.ToColor; }
		set {
			_gradientPanel.ToColor = value;
			Invalidate();
		}
	}

	private void _iconPanel_SizeChanged(object sender, EventArgs e) {
		if (Loaded && !Disposing && !IsDisposed)
			RenderApplicationIcon();
	}

	private void RenderApplicationIcon() {
		var icon = ApplicationIcon.Value;
		if (icon == null || _iconPanel.Width <= 0 || _iconPanel.Height <= 0)
			return;
		SetIconImage(icon.ToBitmap(_iconPanel.Width, _iconPanel.Height));
	}

	private void SetIconImage(Image image) {
		var previousImage = _iconImage;
		_iconPanel.BackgroundImage = image;
		_iconImage = image;
		previousImage?.Dispose();
	}

}

