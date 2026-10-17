// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Dev Age
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements;

public interface ISortIndicator : IVisualElement {
	HeaderSortStyle SortStyle { get; set; }
}


/// <summary>
/// A class used to draw a generic sort indicator, usually a arrow. Use the SortStyle to customize the sort style (arrow up or arrow down)
/// </summary>
[Serializable]
public class SortIndicator : Icon, ISortIndicator {

	#region Constuctor

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

	#endregion

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new SortIndicator(this);
	}

	#region Properties
	/// <summary>Retains the glyph's width before sorting so natural header sizing also fits a later sort indicator.</summary>
	[DefaultValue(true)]
	public bool ReserveSpaceWhenUnsorted { get; set; } = true;

	private HeaderSortStyle mHeaderSortStyle = HeaderSortStyle.None;

	[DefaultValue(HeaderSortStyle.None)]
	public virtual HeaderSortStyle SortStyle {
		get { return mHeaderSortStyle; }
		set {
			mHeaderSortStyle = value;
			if (mHeaderSortStyle == HeaderSortStyle.Ascending)
				Value = Resources.IEW_SortUp;
			else if (mHeaderSortStyle == HeaderSortStyle.Descending)
				Value = Resources.IEW_SortDown;
			else
				Value = null;
		}
	}

	#endregion

	protected override SizeF OnMeasureContent(MeasureHelper measure, SizeF maxSize) {
		var size = base.OnMeasureContent(measure, maxSize);
		if (ReserveSpaceWhenUnsorted) {
			size.Width = Math.Max(size.Width, Math.Max(Resources.IEW_SortUp.Width, Resources.IEW_SortDown.Width));
			size.Height = Math.Max(size.Height, Math.Max(Resources.IEW_SortUp.Height, Resources.IEW_SortDown.Height));
		}
		return size;
	}

}

