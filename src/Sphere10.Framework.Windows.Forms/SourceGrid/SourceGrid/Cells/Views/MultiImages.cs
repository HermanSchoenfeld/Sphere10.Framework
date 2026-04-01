// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Dev Age
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Views;

/// <summary>
/// Summary description for VisualModelCheckBox.
/// </summary>
[Serializable]
public class MultiImages : Cell {

	#region Constructors

	/// <summary>
	/// Use default setting
	/// </summary>
	public MultiImages() {
		ElementsDrawMode = Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.ElementsDrawMode.Covering;
	}

	/// <summary>
	/// Copy constructor.  This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <param name="other"></param>
	public MultiImages(MultiImages other)
		: base(other) {
		mImages = (Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.VisualElementList)other.mImages.Clone();
	}

	#endregion

	private Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.VisualElementList mImages = new Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.VisualElementList();

	/// <summary>
	/// Images of the cells
	/// </summary>
	public Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.VisualElementList SubImages {
		get { return mImages; }
	}

	protected override IEnumerable<Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.IVisualElement> GetElements() {
		foreach (Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.IVisualElement v in GetBaseElements())
			yield return v;

		foreach (Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.IVisualElement v in SubImages)
			yield return v;
	}
	private IEnumerable<Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.IVisualElement> GetBaseElements() {
		return base.GetElements();
	}

	#region Clone

	/// <summary>
	/// Clone this object. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new MultiImages(this);
	}

	#endregion

}

