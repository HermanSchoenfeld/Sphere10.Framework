// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using Sphere10.Framework.Drawing;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

/// <summary>
/// A class used to draw a generic sort indicator, usually a arrow. Use the SortStyle to customize the sort style (arrow up or arrow down)
/// </summary>
[Serializable]
public class SortIndicator : Icon, ISortIndicator {
	private HeaderSortStyle _headerSortStyle = HeaderSortStyle.None;

	/// <summary>
	/// Default constructor
	/// </summary>
	public SortIndicator() {
		AnchorArea = new AnchorArea(float.NaN, float.NaN, 0, float.NaN, false, true);
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public SortIndicator(SortIndicator other)
		: base(other) {
		SortStyle = other.SortStyle;
		ReserveSpaceWhenUnsorted = other.ReserveSpaceWhenUnsorted;
	}

	/// <summary>Retains the glyph's width before sorting so natural header sizing also fits a later sort indicator.</summary>
	[DefaultValue(true)]
	public bool ReserveSpaceWhenUnsorted { get; set; } = true;

	[DefaultValue(HeaderSortStyle.None)]
	public virtual HeaderSortStyle SortStyle {
		get {
			return _headerSortStyle;
		}

		set {
			_headerSortStyle = value;
			if (_headerSortStyle == HeaderSortStyle.Ascending)
				Value = Resources.IEW_SortUp;
			else if (_headerSortStyle == HeaderSortStyle.Descending)
				Value = Resources.IEW_SortDown;
			else
				Value = null;
		}
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new SortIndicator(this);
	}

	protected override SizeF OnMeasureContent(GraphicsMeasurement measure, SizeF maxSize) {
		var size = base.OnMeasureContent(measure, maxSize);
		if (ReserveSpaceWhenUnsorted) {
			size.Width = Math.Max(size.Width, Math.Max(Resources.IEW_SortUp.Width, Resources.IEW_SortDown.Width));
			size.Height = Math.Max(size.Height, Math.Max(Resources.IEW_SortUp.Height, Resources.IEW_SortDown.Height));
		}

		return size;
	}
}
