// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

public partial class ProductAboutBox : ApplicationForm, IAboutBox {
	private bool _detailsLayoutInitialized;
	private bool _arrangingDetails;

	public ProductAboutBox() {
		InitializeComponent();
	}

	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (!Tools.Runtime.IsDesignMode) {
			var icon = Sphere10Framework.Instance.ServiceProvider.GetService<IApplicationIconProvider>()?.ApplicationIcon;
			if (icon != null)
				Icon = icon;
			Text = StringFormatter.FormatEx(this.Text);
			_label1.Text = StringFormatter.FormatEx(_label1.Text);
			_label4.Text = StringFormatter.FormatEx(_label4.Text);
			_label2.Text = StringFormatter.FormatEx(_label2.Text);
			_label3.Text = StringFormatter.FormatEx(_label3.Text);
			_link1.Text = StringFormatter.FormatEx(_link1.Text).TrimStart("https://").TrimStart("http://");
			_label9.Text = StringFormatter.FormatEx(_label9.Text);
			_label10.Text = StringFormatter.FormatEx(_label10.Text);
			_label11.Text = StringFormatter.FormatEx(_label11.Text);
			_label12.Text = StringFormatter.FormatEx(_label12.Text);
			_companyNumberLabel.Text = StringFormatter.FormatEx(_companyNumberLabel.Text);
			_detailsLayoutInitialized = true;
			ArrangeProductDetails();
		}
	}

	protected override void OnLayout(LayoutEventArgs e) {
		base.OnLayout(e);
		if (_detailsLayoutInitialized && !_arrangingDetails && !Disposing && !IsDisposed)
			ArrangeProductDetails();
	}

	protected virtual void ArrangeAdditionalDetails(int detailsBottom) {
	}

	private void ArrangeProductDetails() {
		if (_arrangingDetails || Disposing || IsDisposed)
			return;
		_arrangingDetails = true;
		SuspendLayout();
		using var layoutState = Tools.Scope.ExecuteOnDispose(() => {
			ResumeLayout(true);
			_arrangingDetails = false;
		});
		// Formatted metadata determines the space each column needs at the current font and DPI.
		var productLabels = new Label[] { _label1, _companyNumberLabel, _label2, _label3, _label4, _link1 };
		var usageCaptions = new[] { _label5, _label6, _label7, _label8 };
		var usageValues = new[] { _label9, _label10, _label11, _label12 };
		var bodyLabels = productLabels.Concat(usageCaptions).Concat(usageValues).ToArray();
		var margin = LogicalToDeviceUnits(14);
		var columnGap = LogicalToDeviceUnits(20);
		var valueGap = LogicalToDeviceUnits(12);
		var rowHeight = bodyLabels.Max(label => Math.Max(label.Font.Height, label.PreferredSize.Height)) + LogicalToDeviceUnits(1);
		var productWidth = productLabels.Max(label => label.PreferredSize.Width);
		var captionWidth = usageCaptions.Max(label => label.PreferredSize.Width);
		var valueWidth = usageValues.Max(label => label.PreferredSize.Width);
		var bodyTop = applicationBanner1.Bottom + LogicalToDeviceUnits(20);
		var bodyBottom = bodyTop + productLabels.Length * rowHeight;
		var minimumWidth = margin * 2 + productWidth + columnGap + captionWidth + valueGap + valueWidth;
		if (ClientSize.Width < minimumWidth)
			ClientSize = new Size(minimumWidth, ClientSize.Height);

		// Keep the designer's direct controls while removing overlap from stale right-anchored text bounds.
		for (var index = 0; index < productLabels.Length; index++)
			productLabels[index].Location = new Point(margin, bodyTop + index * rowHeight);
		var captionLeft = margin + productWidth + columnGap;
		var valueLeft = captionLeft + captionWidth + valueGap;
		for (var index = 0; index < usageCaptions.Length; index++) {
			usageCaptions[index].Bounds = new Rectangle(captionLeft, bodyTop + index * rowHeight, captionWidth, rowHeight);
			usageValues[index].Location = new Point(valueLeft, bodyTop + index * rowHeight);
		}
		ArrangeAdditionalDetails(bodyBottom);

		// Retain space for inherited details and additional information controls and bottom-anchored commands.
		foreach (Control control in Controls)
			if (control != okButton && control != applicationBanner1 && !bodyLabels.Contains(control)
				&& control.Dock == DockStyle.None && !control.Anchor.HasFlag(AnchorStyles.Bottom))
				bodyBottom = Math.Max(bodyBottom, control.Bottom);
		var minimumHeight = bodyBottom + LogicalToDeviceUnits(6) + okButton.Height + LogicalToDeviceUnits(13);
		if (ClientSize.Height < minimumHeight)
			ClientSize = new Size(ClientSize.Width, minimumHeight);
		okButton.Location = new Point(ClientSize.Width - margin - okButton.Width, ClientSize.Height - LogicalToDeviceUnits(13) - okButton.Height);
	}

	private void _productLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
		var url = StringFormatter.FormatEx("{CompanyUrl}");
		var websiteLauncher = Sphere10Framework.Instance.ServiceProvider.GetService<IWebsiteLauncher>();
		websiteLauncher.LaunchWebsite(url);
	}

}

