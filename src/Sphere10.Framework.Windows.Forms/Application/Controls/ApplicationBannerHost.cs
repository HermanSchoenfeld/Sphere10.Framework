// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Hosts the registered application banner while retaining a stock banner on the designer surface.</summary>
public partial class ApplicationBannerHost : UserControl {

	private ApplicationBanner _banner;

	public ApplicationBannerHost() {
		InitializeComponent();
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ApplicationBanner Banner {
		get {
			if (DesignMode || Tools.Runtime.IsDesignMode)
				return _previewBanner;
			if (_banner != null)
				return _banner;
			var banner = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<ApplicationBanner>();
			Guard.Ensure(banner.Parent == null, "Each banner host requires its own application banner instance.");
			SuspendLayout();
			using var layoutState = Tools.Scope.ExecuteOnDispose(() => ResumeLayout(true));
			Controls.Remove(_previewBanner);
			_previewBanner.Dispose();
			_banner = banner;
			_banner.Dock = DockStyle.Fill;
			Controls.Add(_banner);
			return _banner;
		}
	}

	protected override void OnLoad(EventArgs e) {
		if (!DesignMode && !Tools.Runtime.IsDesignMode)
			_ = Banner;
		base.OnLoad(e);
	}
}
