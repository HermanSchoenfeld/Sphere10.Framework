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
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A check box on a tool strip or status strip, for a view option the user turns on and off, such as "Flatten Window Hierarchy".
/// It hosts a standard check box with its caption beside the box, on the strip's own background.</summary>
/// <remarks>Set <see cref="ToolStripItem.Text"/> and follow <see cref="CheckedChanged"/>. Assigning the state it already has raises nothing.</remarks>
[ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.ToolStrip | ToolStripItemDesignerAvailability.StatusStrip)]
[DefaultEvent(nameof(CheckedChanged))]
public class ToolStripCheckBox : ToolStripControlHost {
	[Description("Raised after the user or the owner checked or cleared the box")]
	[Category("Action")]
	public event EventHandlerEx CheckedChanged;

	public ToolStripCheckBox()
		: base(new CheckBox { AutoSize = true, BackColor = Color.Transparent, UseVisualStyleBackColor = false, Padding = new Padding(4, 0, 4, 0) }) {
		CheckBox.CheckedChanged += CheckBoxCheckedChanged;
	}

	/// <summary>The hosted check box.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public CheckBox CheckBox => (CheckBox)Control;

	/// <summary>Whether the box is checked.</summary>
	[Category("Behavior"), DefaultValue(false), Description("Whether the box is checked.")]
	public bool Checked {
		get => CheckBox.Checked;
		set => CheckBox.Checked = value;
	}

	protected void NotifyCheckedChanged() {
		OnCheckedChanged();
		CheckedChanged?.Invoke();
	}

	protected virtual void OnCheckedChanged() {
	}

	protected override void Dispose(bool disposing) {
		if (disposing && Control != null)
			CheckBox.CheckedChanged -= CheckBoxCheckedChanged;
		base.Dispose(disposing);
	}

	private void CheckBoxCheckedChanged(object sender, EventArgs args) => NotifyCheckedChanged();
}
