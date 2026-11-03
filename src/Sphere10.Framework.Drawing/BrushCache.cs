// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Runtime.Versioning;

namespace Sphere10.Framework.Drawing;

/// <summary>
/// Caches the brushes used by drawing methods and owns every brush returned until disposal.
/// Use GetBrush to retrieve or create a brush, and dispose the cache when drawing finishes.
/// Usually this cache is owned by GraphicsCache rather than used directly.
/// </summary>
[SupportedOSPlatform("windows")]
public class BrushCache : SyncDisposable {
	private readonly ActionCache<Color, SolidBrush> _brushes;
	private readonly Disposables _uncachedBrushes = new();

	/// <summary>Creates a brush cache with a nonnegative number of reusable slots.</summary>
	/// <param name="maxCapacity">The maximum number of colors retained for reuse; zero disables reuse.</param>
	public BrushCache(int maxCapacity) {
		_brushes = new ActionCache<Color, SolidBrush>(static color => new SolidBrush(color), static _ => 1, CacheReapPolicy.None, maxCapacity: maxCapacity);
		_brushes.ItemRemoved += static (_, item) => item.Dispose();
	}

	/// <summary>
	/// Returns a cached brush for the color, or creates one owned by this cache until disposal.
	/// Once all reusable slots are occupied, additional brushes remain uncached and are still released when drawing finishes.
	/// </summary>
	/// <param name="color">The exact color identity used to look up the brush.</param>
	/// <returns>A borrowed brush that must not outlive this cache.</returns>
	public SolidBrush GetBrush(Color color) {
		// Keep lookup, admission and overflow ownership within the same cache scope.
		using var scope = _brushes.EnterWriteScope();
		if (_brushes.ContainsCachedItem(color) || _brushes.ItemCount < _brushes.MaxCapacity)
			return _brushes[color];

		// Capacity limits reuse, so earlier brushes remain valid while overflow brushes are owned separately.
		var brush = new SolidBrush(color);
		_uncachedBrushes.Add(brush);
		return brush;
	}

	/// <summary>Releases cached and overflow brushes without owning the drawing surface.</summary>
	protected override void DisposeInternal() {
		// Exclude admission while releasing reusable entries and every overflow resource.
		using var scope = _brushes.EnterWriteScope();
		_brushes.Dispose();
		_uncachedBrushes.Dispose();
		_uncachedBrushes.Clear();
	}
}
