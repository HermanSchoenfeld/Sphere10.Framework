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

/// <summary>A slider on a tool strip or status strip, for a whole-number setting the user adjusts by dragging, such as a zoom level or a
/// number of columns. It hosts a standard track bar without tick marks, sized to the strip.</summary>
/// <remarks>Set <see cref="Minimum"/> and <see cref="Maximum"/>, then follow <see cref="ValueChanged"/>. Assigning the value it already
/// has raises nothing.</remarks>
[ToolStripItemDesignerAvailability(ToolStripItemDesignerAvailability.ToolStrip | ToolStripItemDesignerAvailability.StatusStrip)]
[DefaultEvent(nameof(ValueChanged))]
public class ToolStripTrackBar : ToolStripControlHost {
	[Description("Raised after the user or the owner changed the value")]
	[Category("Action")]
	public event EventHandlerEx ValueChanged;

	private const int DefaultLogicalWidth = 120;

	public ToolStripTrackBar()
		: base(new TrackBar { AutoSize = false, TickStyle = TickStyle.None, Minimum = 0, Maximum = 10, TabStop = false }) {
		AutoSize = false;
		Size = new Size(DefaultLogicalWidth, 22);
		TrackBar.ValueChanged += TrackBarValueChanged;
	}

	/// <summary>The hosted track bar.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public TrackBar TrackBar => (TrackBar)Control;

	[Category("Behavior"), DefaultValue(0), Description("The lowest value of the slider.")]
	public int Minimum {
		get => TrackBar.Minimum;
		set => TrackBar.Minimum = value;
	}

	[Category("Behavior"), DefaultValue(10), Description("The highest value of the slider.")]
	public int Maximum {
		get => TrackBar.Maximum;
		set => TrackBar.Maximum = value;
	}

	/// <summary>The slider's position. A value outside the range is brought to its nearer end.</summary>
	[Category("Behavior"), DefaultValue(0), Description("The slider's position.")]
	public int Value {
		get => TrackBar.Value;
		set => TrackBar.Value = Math.Clamp(value, TrackBar.Minimum, TrackBar.Maximum);
	}

	/// <summary>How far the Page Up and Page Down keys, and a click beside the thumb, move the slider.</summary>
	[Category("Behavior"), DefaultValue(5), Description("How far a page key or a click beside the thumb moves the slider.")]
	public int LargeChange {
		get => TrackBar.LargeChange;
		set => TrackBar.LargeChange = value;
	}

	protected void NotifyValueChanged() {
		OnValueChanged();
		ValueChanged?.Invoke();
	}

	protected virtual void OnValueChanged() {
	}

	protected override void Dispose(bool disposing) {
		if (disposing && Control != null)
			TrackBar.ValueChanged -= TrackBarValueChanged;
		base.Dispose(disposing);
	}

	private void TrackBarValueChanged(object sender, EventArgs args) => NotifyValueChanged();
}
