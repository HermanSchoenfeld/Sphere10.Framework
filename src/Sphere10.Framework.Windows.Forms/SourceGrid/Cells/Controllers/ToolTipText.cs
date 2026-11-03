// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

/// <summary>
/// Allow to customize the tooltiptext of a cell. This class read the tooltiptext from the ICellToolTipText.GetToolTipText.  This behavior can be shared between multiple cells.
/// </summary>
public class ToolTipText : ControllerBase {
	private string _toolTipTitle = string.Empty;
	private System.Windows.Forms.ToolTipIcon _toolTipIcon = System.Windows.Forms.ToolTipIcon.None;
	private bool _isBalloon = false;
	private System.Drawing.Color _backColor = System.Drawing.Color.Empty;
	private System.Drawing.Color _foreColor = System.Drawing.Color.Empty;

	/// <summary>
	/// Default tooltiptext
	/// </summary>
	public readonly static ToolTipText Default = new ToolTipText();

	public string ToolTipTitle {
		get {
			return _toolTipTitle;
		}
		set {
			_toolTipTitle = value;
		}
	}

	public System.Windows.Forms.ToolTipIcon ToolTipIcon {
		get {
			return _toolTipIcon;
		}
		set {
			_toolTipIcon = value;
		}
	}

	public bool IsBalloon {
		get {
			return _isBalloon;
		}
		set {
			_isBalloon = value;
		}
	}

	public System.Drawing.Color BackColor {
		get {
			return _backColor;
		}
		set {
			_backColor = value;
		}
	}

	public System.Drawing.Color ForeColor {
		get {
			return _foreColor;
		}
		set {
			_foreColor = value;
		}
	}

	// IBehaviorModel Members
	public override void OnMouseEnter(CellContext sender, EventArgs e) {
		base.OnMouseEnter(sender, e);

		ApplyToolTipText(sender, e);
	}

	public override void OnMouseLeave(CellContext sender, EventArgs e) {
		base.OnMouseLeave(sender, e);

		ResetToolTipText(sender, e);
	}

	/// <summary>
	/// Change the cursor with the cursor of the cell
	/// </summary>
	protected virtual void ApplyToolTipText(CellContext sender, EventArgs e) {
		Models.IToolTipText toolTip;
		if ((toolTip = (Models.IToolTipText)sender.Cell.Model.FindModel(typeof(Models.IToolTipText))) != null) {
			var text = toolTip.GetToolTipText(sender);
			if (text != null && text.Length > 0) {
				sender.Grid.ToolTipText = text;
				sender.Grid.ToolTip.ToolTipTitle = ToolTipTitle;
				sender.Grid.ToolTip.ToolTipIcon = ToolTipIcon;
				sender.Grid.ToolTip.IsBalloon = IsBalloon;
				if (BackColor.IsEmpty == false)
					sender.Grid.ToolTip.BackColor = BackColor;
				if (ForeColor.IsEmpty == false)
					sender.Grid.ToolTip.ForeColor = ForeColor;
			}
		}
	}

	/// <summary>
	/// Reset the original cursor
	/// </summary>
	protected virtual void ResetToolTipText(CellContext sender, EventArgs e) {
		if (sender.Cell.Model.FindModel(typeof(Models.IToolTipText)) != null)
			sender.Grid.ToolTipText = null;
	}
}
