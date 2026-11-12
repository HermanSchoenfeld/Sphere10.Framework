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
using Sphere10.Framework.Windows;

namespace Tools;

public static partial class WinForms {

	/// <summary>Creates the existing grid and adapters on their owning UI thread without choosing paging, collection, column or settings policy.</summary>
	public static class Grids {
		/// <summary>Creates a fresh caller-owned grid. Configure its existing IDataSource, GridBindings, RowDetails and Preferences APIs normally.</summary>
		public static VisualGrid CreateVisualGrid() => new VisualGrid();

		/// <summary>Binds a borrowed ranged source and grid through the existing controller. Configure and refresh on that same UI thread; the source retains responsibility for its read execution.</summary>
		public static VisualGridDataSourceController<T> CreateDataSourceController<T>(VisualGrid grid, IDataSource<T> source, Func<T, object> resolvePublishedEntity = null)
			=> new VisualGridDataSourceController<T>(grid, source, resolvePublishedEntity);

		/// <summary>Observes a borrowed source and presents its published values on the control dispatcher without starting collection.</summary>
		public static DataSourceViewSubscription<T> CreateViewSubscription<T>(Control owner, IDataSource<T> source, Func<Task> refresh, Action<Exception> reportFailure = null)
			=> new DataSourceViewSubscription<T>(owner, source, refresh, reportFailure);

		/// <summary>Creates an array controller over its ordinary new array store. The borrowed grid and collector retain their existing lifetime and worker-dispatch contracts.</summary>
		public static VisualGridArrayController<T> CreateArrayController<T>(VisualGrid grid, Func<T, object> identity, Func<CancellationToken, Task<T[]>> collect,
			Action<T[]> changed = null
		) {
			return new VisualGridArrayController<T>(grid, identity, collect, changed);
		}

		/// <summary>Creates an array controller using a caller-supplied store. Grid, store and collector remain under the caller's existing ownership; refresh on the creating UI thread.</summary>
		public static VisualGridArrayController<T> CreateArrayController<T>(VisualGrid grid, ArrayDataSource<T> source, Func<T, object> identity,
			Func<CancellationToken, Task<T[]>> collect, Action<T[]> changed = null
		) {
			return new VisualGridArrayController<T>(grid, source, identity, collect, changed);
		}

		/// <summary>Keeps a borrowed source's acquisition separate from a view's hierarchy or other array projection.</summary>
		public static VisualGridArrayController<T> CreateArrayController<T>(VisualGrid grid, IDataSource<T> captureSource,
			ArrayDataSource<T> projectionSource, Func<T, object> identity, Action<T[]> changed = null
		) {
			return new VisualGridArrayController<T>(grid, captureSource, projectionSource, identity, changed);
		}

		/// <summary>Creates the existing row-detail adapter, taking ownership of the fresh child grid. The RowDetails owner publishes prepared values and disposes the adapter on its UI thread.</summary>
		public static VisualGridArrayDetail<T> CreateArrayDetail<T>(VisualGrid grid, Func<T, object> identity, Action<T[]> changed = null,
			ArrayDataSource<T> source = null, Action<VisualGrid, T[]> beforePublish = null, Func<object, T[]> readPreparedRows = null
		) {
			return new VisualGridArrayDetail<T>(grid, identity, changed, source, beforePublish, readPreparedRows);
		}

		/// <summary>Creates the existing general row-detail adapter, taking ownership of the control and supplied resources. Its optional publication callback runs on the owning UI thread.</summary>
		public static VisualGridControlDetail CreateControlDetail(Control control, Action<Control, object> apply = null, params IDisposable[] resources)
			=> new VisualGridControlDetail(control, apply, resources);
	}
}
