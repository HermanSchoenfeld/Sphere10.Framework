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
public abstract class DropDownButtonBase : VisualElementBase, IDropDownButton {
	private ButtonStyle _controlDrawStyle = ButtonStyle.Normal;

	/// <summary>
	/// Default constructor
	/// </summary>
	public DropDownButtonBase() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public DropDownButtonBase(DropDownButtonBase other)
		: base(other) {
		Style = other.Style;
	}

	public virtual ButtonStyle Style {
		get => _controlDrawStyle;
		set => _controlDrawStyle = value;
	}

	protected virtual bool ShouldSerializeStyle() {
		return Style != ButtonStyle.Normal;
	}
}
