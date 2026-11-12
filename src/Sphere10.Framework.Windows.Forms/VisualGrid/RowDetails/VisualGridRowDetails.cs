// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Opt-in owned content below an entity row. Model preparation runs on a worker; factories and publication run on the native owner.</summary>
public sealed class VisualGridRowDetails {
	/// <summary>Stable, nonnull unique identities within this provider. Use a schema-qualified key when different entity kinds share numeric IDs.</summary>
	public Func<object, object> Identity { get; init; }
	/// <summary>Optional child-query identity. Change it when relation/filter parameters change; ordinary counter updates need not cancel an active query.</summary>
	public Func<object, object> QueryIdentity { get; init; }
	/// <summary>Worker-only projection over detached values; do not inspect controls here.</summary>
	public Func<object, bool> HasDetails { get; init; }
	/// <summary>Produces detached values on a worker. No controls may be created or accessed in this callback.</summary>
	public Func<object, CancellationToken, Task<object>> PrepareAsync { get; init; }
	/// <summary>Creates an owned panel, image, editor or nested grid from already prepared values on the UI thread.</summary>
	public Func<object, object, VisualGridDetail> Create { get; init; }
	/// <summary>Height in 96-DPI logical pixels; the child owns scrolling inside this bounded area.</summary>
	public int Height { get; init; } = 240;
	/// <summary>For nested tables, shrink to committed native row heights up to Height; expanded descendants propagate their geometry. General panels keep Height.</summary>
	public bool FitHeightToChildRows { get; init; }
	/// <summary>Child tables borrow the parent's columns and resolved widths. Indentation stays inside column content rather than moving the child grid.</summary>
	public bool ShareParentColumns { get; init; }
	/// <summary>Optional stable PreferenceKey for the expansion/content gutter; the first displayed column is used when absent or hidden.</summary>
	public string IndentColumnKey { get; init; }
	public int MaximumDepth { get; init; } = 8;
	/// <summary>Shared limit for native expanded child surfaces across the complete recursive root.</summary>
	public int MaximumExpandedDetails { get; init; } = 16;
	/// <summary>Bounded value-only state retention; evicted controls and data sources are always disposed.</summary>
	public int MaximumRememberedDetails { get; init; } = 64;

	internal void Validate() {
		Guard.ArgumentNotNull(Identity, nameof(Identity));
		Guard.ArgumentNotNull(HasDetails, nameof(HasDetails));
		Guard.ArgumentNotNull(PrepareAsync, nameof(PrepareAsync));
		Guard.ArgumentNotNull(Create, nameof(Create));
		Guard.ArgumentInRange(Height, 40, 2000, nameof(Height));
		Guard.ArgumentInRange(MaximumDepth, 1, 32, nameof(MaximumDepth));
		Guard.ArgumentInRange(MaximumExpandedDetails, 1, 128, nameof(MaximumExpandedDetails));
		Guard.ArgumentInRange(MaximumRememberedDetails, 1, 4096, nameof(MaximumRememberedDetails));
	}
}
