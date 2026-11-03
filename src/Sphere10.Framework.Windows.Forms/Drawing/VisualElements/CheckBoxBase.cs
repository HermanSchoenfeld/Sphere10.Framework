// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

[Serializable]
public abstract class CheckBoxBase : VisualElementBase, ICheckBox {
	private ControlDrawStyle _controlDrawStyle = ControlDrawStyle.Normal;
	private CheckBoxState _checkBoxState = CheckBoxState.Undefined;

	/// <summary>
	/// Default constructor
	/// </summary>
	public CheckBoxBase() {
		AnchorArea = new AnchorArea(float.NaN, float.NaN, float.NaN, float.NaN, true, true);
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public CheckBoxBase(CheckBoxBase other)
		: base(other) {
		Style = other.Style;
		CheckBoxState = other.CheckBoxState;
	}

	public virtual ControlDrawStyle Style {
		get => _controlDrawStyle;
		set => _controlDrawStyle = value;
	}

	public virtual CheckBoxState CheckBoxState {
		get => _checkBoxState;
		set => _checkBoxState = value;
	}

	protected virtual bool ShouldSerializeStyle() {
		return Style != ControlDrawStyle.Normal;
	}

	protected virtual bool ShouldSerializeCheckBoxState() {
		return CheckBoxState != CheckBoxState.Undefined;
	}
}
