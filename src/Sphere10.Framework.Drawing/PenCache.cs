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
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;

namespace Sphere10.Framework.Drawing;

/// <summary>
/// Caches the pens used by drawing methods and owns every pen returned until disposal.
/// Use GetPen to retrieve or create a pen, and dispose the cache when drawing finishes.
/// Usually this cache is owned by GraphicsCache rather than used directly.
/// </summary>
[SupportedOSPlatform("windows")]
public class PenCache : SyncDisposable {
	private readonly ActionCache<PenKey, Pen> _pens;
	private readonly Disposables _uncachedPens = new();

	/// <summary>Creates a pen cache with a nonnegative number of reusable slots.</summary>
	/// <param name="maxCapacity">The maximum number of pen specifications retained for reuse; zero disables reuse.</param>
	public PenCache(int maxCapacity) {
		_pens = new ActionCache<PenKey, Pen>(CreatePen, static _ => 1, CacheReapPolicy.None, maxCapacity: maxCapacity);
		_pens.ItemRemoved += static (_, item) => item.Dispose();
	}

	/// <summary>
	/// Returns a cached pen for the complete specification, or creates one owned by this cache until disposal.
	/// Once all reusable slots are occupied, additional pens remain uncached and are still released when drawing finishes.
	/// </summary>
	/// <param name="color">The exact color identity used to look up the pen.</param>
	/// <param name="width">The native pen width.</param>
	/// <param name="style">The native dash style.</param>
	/// <returns>A borrowed pen that must not outlive this cache.</returns>
	public Pen GetPen(Color color, float width, DashStyle style) {
		// Keep lookup, admission and overflow ownership within the same cache scope.
		using var scope = _pens.EnterWriteScope();
		var key = new PenKey(color, width, style);
		if (_pens.ContainsCachedItem(key) || _pens.ItemCount < _pens.MaxCapacity)
			return _pens[key];

		// Capacity limits reuse, so earlier pens remain valid while overflow pens are owned separately.
		var pen = CreatePen(key);
		_uncachedPens.Add(pen);
		return pen;
	}

	/// <summary>Releases cached and overflow pens without owning the drawing surface.</summary>
	protected override void DisposeInternal() {
		// Exclude admission while releasing reusable entries and every overflow resource.
		using var scope = _pens.EnterWriteScope();
		_pens.Dispose();
		_uncachedPens.Dispose();
		_uncachedPens.Clear();
	}

	// Release the native pen if applying an invalid dash style fails before ownership can transfer to the cache.
	private static Pen CreatePen(PenKey key) {
		var pen = new Pen(key.Color, key.Width);
		try {
			if (pen.DashStyle != key.Style)
				pen.DashStyle = key.Style;
			return pen;
		} catch {
			pen.Dispose();
			throw;
		}
	}

	#region Inner Types

	// Color, width and dash style together identify the exact reusable native pen.
	private readonly struct PenKey : IEquatable<PenKey> {
		// Capture a complete pen specification before a cache lookup.
		public PenKey(Color color, float width, DashStyle style) {
			Color = color;
			Width = width;
			Style = style;
		}

		// Retains the exact named or ARGB color identity.
		public Color Color { get; }

		// Retains the exact native width used for lookup.
		public float Width { get; }

		// Retains the dash style used for lookup.
		public DashStyle Style { get; }

		// Preserve the exact color and floating-point comparisons used by the original drawing cache.
		public bool Equals(PenKey other) {
			return other.Color == Color && other.Style == Style && other.Width == Width;
		}

		// Keep object-based equality consistent with typed cache keys.
		public override bool Equals(object obj) {
			return obj is PenKey other && Equals(other);
		}

		// Hash the same fields that define equality for the typed cache lookup.
		public override int GetHashCode() {
			return HashCode.Combine(Color, Width, Style);
		}
	}

	#endregion
}
