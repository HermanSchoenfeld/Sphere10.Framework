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
using System.Runtime.Versioning;

namespace Sphere10.Framework.Drawing;

/// <summary>
/// A wrapper for the GDI Graphic instance. Contains also a set of class used to cache pens and brushes.
/// </summary>
[SupportedOSPlatform("windows")]
public class GraphicsCache : IDisposable {
	private Rectangle _clipRectangle;

	/// <summary>Borrows graphics and owns the pens and brushes created while drawing.</summary>
	public GraphicsCache(Graphics graphics) {
		Graphics = graphics;
		_clipRectangle = Rectangle.Empty;
		PenCache = new PenCache(20);
		BrushCache = new BrushCache(20);
	}

	/// <summary>Borrows graphics with an explicit clipping rectangle.</summary>
	public GraphicsCache(Graphics graphics, Rectangle clipRectangle) {
		Graphics = graphics;
		_clipRectangle = clipRectangle;
		PenCache = new PenCache(20);
		BrushCache = new BrushCache(20);
	}

	/// <summary>Borrows graphics and configures the capacities of its owned drawing caches.</summary>
	public GraphicsCache(Graphics graphics, Rectangle clipRectangle, int pensCapacity, int brushCapacity) {
		Graphics = graphics;
		_clipRectangle = clipRectangle;
		PenCache = new PenCache(pensCapacity);
		BrushCache = new BrushCache(brushCapacity);
	}

	/// <summary>Gets the caller-supplied clipping rectangle.</summary>
	public Rectangle ClipRectangle => _clipRectangle;

	/// <summary>Gets the borrowed graphics without transferring ownership.</summary>
	public Graphics Graphics { get; private set; }

	/// <summary>Gets the pen cache owned by this drawing operation.</summary>
	public PenCache PenCache { get; private set; }

	/// <summary>Gets the brush cache owned by this drawing operation.</summary>
	public BrushCache BrushCache { get; private set; }

	/// <summary>Disposes cached drawing resources while leaving the borrowed graphics open.</summary>
	public void Dispose() {
		PenCache?.Dispose();
		PenCache = null;
		BrushCache?.Dispose();
		BrushCache = null;
		Graphics = null;
	}
}
