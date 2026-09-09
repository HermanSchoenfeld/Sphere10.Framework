// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

/// <summary>A content-only screen for checking that detached hosts do not reserve empty menu or toolbar rows.</summary>
public class ScreenHostingPlainTestScreen : ApplicationScreen {
	public ScreenHostingPlainTestScreen() {
		Title = "Plain screen";
		ActivationMode = ScreenActivationMode.SingleInstance;
		var Layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
		Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		Layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		Layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		Layout.Controls.Add(new Label {
			AutoSize = true,
			Dock = DockStyle.Fill,
			Text = "This screen has no menu or toolbar. Undock it: the content should begin immediately below the window caption, " +
				"with no empty bars. Edit these notes, then re-dock to check that the content is retained."
		});
		Layout.Controls.Add(new TextBox {
			Multiline = true,
			Dock = DockStyle.Fill,
			ScrollBars = ScrollBars.Vertical,
			Text = "Notes for the screen without menus or a toolbar."
		});
		Controls.Add(Layout);
	}
}
