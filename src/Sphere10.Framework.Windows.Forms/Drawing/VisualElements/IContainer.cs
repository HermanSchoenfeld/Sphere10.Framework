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
using System.Drawing;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

public interface IContainer : IVisualElement {
	/*
		IBorder Border {
			get;
			set;
		}
		Padding Padding {
			get;
			set;
		}
		IVisualElement Background {
			get;
			set;
		}
		ElementsDrawMode ElementsDrawMode {
			get;
			set;
		}
		IEnumerable<IVisualElement> GetElements();

		 */
	RectangleF GetContentRectangle(GraphicsMeasurement measure, RectangleF backGroundArea);

	SizeF GetExtent(GraphicsMeasurement measure, SizeF contentSize);
}
