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
using System.Windows.Forms.VisualStyles;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

[Serializable]
public class CheckBoxThemed : CheckBoxBase {
	/// <summary>
	/// Standard checkbox used when the XP style are disabled.
	/// </summary>
	private CheckBox _standardCheckBox = new CheckBox();

	/// <summary>
	/// Default constructor
	/// </summary>
	public CheckBoxThemed() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public CheckBoxThemed(CheckBoxThemed other)
		: base(other) {
	}

	public override ControlDrawStyle Style {
		get {
			return base.Style;
		}
		set {
			base.Style = value;
			_standardCheckBox.Style = value;
		}
	}

	public override CheckBoxState CheckBoxState {
		get {
			return base.CheckBoxState;
		}
		set {
			base.CheckBoxState = value;
			_standardCheckBox.CheckBoxState = value;
		}
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new CheckBoxThemed(this);
	}

	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement()))
			GetRenderer(GetBackgroundElement()).DrawBackground(graphics.Graphics, Rectangle.Round(area));
		else
			_standardCheckBox.Draw(graphics, area);
	}

	protected override SizeF OnMeasureContent(GraphicsMeasurement measure, SizeF maxSize) {
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement()))
			return GetRenderer(GetBackgroundElement()).GetPartSize(measure.Graphics, ThemeSizeType.True);
		return _standardCheckBox.Measure(measure, Size.Empty, maxSize);
	}

	protected VisualStyleElement GetBackgroundElement() {
		if (Style == ControlDrawStyle.Hot) {
			if (CheckBoxState == CheckBoxState.Checked)
				return VisualStyleElement.Button.CheckBox.CheckedHot;
			if (CheckBoxState == CheckBoxState.Unchecked)
				return VisualStyleElement.Button.CheckBox.UncheckedHot;
			return VisualStyleElement.Button.CheckBox.MixedHot;
		}
		if (Style == ControlDrawStyle.Pressed) {
			if (CheckBoxState == CheckBoxState.Checked)
				return VisualStyleElement.Button.CheckBox.CheckedPressed;
			if (CheckBoxState == CheckBoxState.Unchecked)
				return VisualStyleElement.Button.CheckBox.UncheckedPressed;
			return VisualStyleElement.Button.CheckBox.MixedPressed;
		}
		if (Style == ControlDrawStyle.Disabled) {
			if (CheckBoxState == CheckBoxState.Checked)
				return VisualStyleElement.Button.CheckBox.CheckedDisabled;
			if (CheckBoxState == CheckBoxState.Unchecked)
				return VisualStyleElement.Button.CheckBox.UncheckedDisabled;
			return VisualStyleElement.Button.CheckBox.MixedDisabled;
		}
		if (CheckBoxState == CheckBoxState.Checked)
			return VisualStyleElement.Button.CheckBox.CheckedNormal;
		if (CheckBoxState == CheckBoxState.Unchecked)
			return VisualStyleElement.Button.CheckBox.UncheckedNormal;
		return VisualStyleElement.Button.CheckBox.MixedNormal;
	}

	/// <summary>
	/// Gets the System.Windows.Forms.VisualStyles.VisualStyleRenderer to draw the specified element.
	/// </summary>
	/// <param name="element"></param>
	/// <returns></returns>
	protected System.Windows.Forms.VisualStyles.VisualStyleRenderer GetRenderer(VisualStyleElement element) {
		return new System.Windows.Forms.VisualStyles.VisualStyleRenderer(element);
	}
}
