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
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing;

/// <summary>
/// Measures native visual elements with owned or borrowed graphics.
/// </summary>
public class GraphicsMeasurement : IDisposable {
	private readonly bool _ownsGraphics;
	private Graphics _graphics;

	/// <summary>Creates and owns graphics for the supplied control.</summary>
	public GraphicsMeasurement(Control control) {
		_graphics = control.CreateGraphics();
		_ownsGraphics = true;
	}

	/// <summary>Borrows graphics without taking disposal ownership.</summary>
	public GraphicsMeasurement(Graphics graphics) => _graphics = graphics;

	/// <summary>Borrows graphics from an existing drawing cache.</summary>
	public GraphicsMeasurement(GraphicsCache graphics)
		: this(graphics.Graphics) {
	}

	/// <summary>Gets the graphics used for measurement.</summary>
	public Graphics Graphics => _graphics;

	/// <summary>Releases control-created graphics; borrowed graphics retain their owner.</summary>
	public void Dispose() {
		if (_graphics != null && _ownsGraphics) {
			_graphics.Dispose();
			_graphics = null;
		}
	}
}
