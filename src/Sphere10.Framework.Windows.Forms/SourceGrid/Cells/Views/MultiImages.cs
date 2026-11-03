// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
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
	private Sphere10.Framework.Windows.Forms.Drawing.VisualElements.VisualElementList _images = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.VisualElementList();

	// Constructors
	/// <summary>
	/// Use default setting
	/// </summary>
	public MultiImages() {
		ElementsDrawMode = Sphere10.Framework.Windows.Forms.Drawing.ElementsDrawMode.Covering;
	}

	/// <summary>
	/// Copy constructor.  This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <param name="other"></param>
	public MultiImages(MultiImages other)
		: base(other) {
		_images = (Sphere10.Framework.Windows.Forms.Drawing.VisualElements.VisualElementList)other._images.Clone();
	}

	/// <summary>
	/// Images of the cells
	/// </summary>
	public Sphere10.Framework.Windows.Forms.Drawing.VisualElements.VisualElementList SubImages => _images;

	// Clone
	/// <summary>
	/// Clone this object. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new MultiImages(this);
	}

	protected override IEnumerable<Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IVisualElement> GetElements() {
		foreach (var v in GetBaseElements())
			yield return v;

		foreach (var v in SubImages)
			yield return v;
	}

	private IEnumerable<Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IVisualElement> GetBaseElements() {
		return base.GetElements();
	}
}
