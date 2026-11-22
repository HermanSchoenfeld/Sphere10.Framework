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
using System.Windows.Forms;
using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;

namespace Tools;

// Adds grid, array-projection and row-detail factories to Tools.WinForms.
// The constructed owners retain binding, presentation and disposal behavior.
public static partial class WinForms {

	/// <summary>Creates the existing grid and adapters on their owning UI thread without choosing paging, collection, column or settings policy.</summary>
	public static class Grids {
		/// <summary>Creates a fresh caller-owned grid. Configure its existing IDataSource, GridBindings, CreateDetail and Preferences APIs normally.</summary>
		public static VisualGrid CreateVisualGrid() => new VisualGrid();

		/// <summary>Creates an array controller over its ordinary new array store. The borrowed grid and collector retain their existing lifetime and worker-dispatch contracts.</summary>
		public static VisualGridArrayController<T> CreateArrayController<T>(VisualGrid grid, Func<T, object> identity, Func<CancellationToken, Task<T[]>> collect,
			Action<T[]> changed = null
		) => new VisualGridArrayController<T>(grid, identity, collect, changed);

		/// <summary>Creates an array controller using a caller-supplied store. Grid, store and collector remain under the caller's existing ownership; refresh on the creating UI thread.</summary>
		public static VisualGridArrayController<T> CreateArrayController<T>(VisualGrid grid, ArrayDataSource<T> source, Func<T, object> identity,
			Func<CancellationToken, Task<T[]>> collect, Action<T[]> changed = null, IDataSource<T> viewSource = null
		) => new VisualGridArrayController<T>(grid, source, identity, collect, changed, viewSource);

		/// <summary>Keeps a borrowed source's acquisition separate from a view's hierarchy or other array projection.</summary>
		public static VisualGridArrayController<T> CreateArrayController<T>(VisualGrid grid, IDataSource<T> captureSource,
			ArrayDataSource<T> projectionSource, Func<T, object> identity, Action<T[]> changed = null, IDataSource<T> viewSource = null
		) => new VisualGridArrayController<T>(grid, captureSource, projectionSource, identity, changed, viewSource);

		/// <summary>Creates a child-source detail adapter, taking ownership of the fresh grid. The parent grid supplies and owns the child-source context.</summary>
		public static VisualGridDataSourceDetail CreateDataSourceDetail(VisualGrid grid, Action<VisualGrid, VisualGridDetailContext> beforePublish = null)
			=> new VisualGridDataSourceDetail(grid, beforePublish);

		/// <summary>Creates the existing general row-detail adapter, taking ownership of the control and supplied resources. Its optional publication callback runs on the owning UI thread.</summary>
		public static VisualGridControlDetail CreateControlDetail(Control control, Action<Control, object> apply = null, params IDisposable[] resources)
			=> new VisualGridControlDetail(control, apply, resources);
	}
}
