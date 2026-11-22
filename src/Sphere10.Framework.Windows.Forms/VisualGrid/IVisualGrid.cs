// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

public interface IVisualGrid {
	event EventHandlerEx<VisualGrid, object> EntitySelected;
	event EventHandlerEx<VisualGrid, object> EntityDeselected;
	event EventHandlerEx<VisualGrid, object> EntityCreated;
	event EventHandlerEx<VisualGrid, object> EntityUpdated;
	event EventHandlerEx<VisualGrid, object> EntityDeleted;
	event EventHandlerEx<VisualGrid, VisualEntityPropertyChangedEventArgs> EntityPropertyChanged;
	event EventHandlerEx<VisualGrid> DataBindingComplete;
	event EventHandlerEx<VisualGrid> SelectionReconciled;
	event EventHandlerEx<VisualGrid> SelectedCellChanged;
	event EventHandlerEx<VisualGrid, object, string> RowDetailExpansionRejected;
	event EventHandlerEx<VisualGridSettings> PreferencesChanged;

	Type EntityEditorDisplay { get; }
	string GridTitle { get; }
	DataSourceCapabilities Capabilities { get; set; }
	bool IsDataSourceBound { get; }
	IVisualGridColumn[] GridBindings { get; }
	Func<object> PublicationIdentity { get; set; }
	Func<object, object> VisibleSelectionIdentity { get; set; }

	void ConfigureDataSource<T>(IDataSource<T> source, Func<T, object> resolvePublishedEntity = null, Func<T, object> visibleSelectionIdentity = null);
	Task RefreshDataSourceAsync(CancellationToken cancellationToken = default);
	void InvalidateDataSource();
	Task SetDataSource<TEntity>(IDataSource<TEntity> dataSource);
	Task<Array> ReadFilteredEntitiesAsync(CancellationToken cancellationToken = default);
	Task<bool> RefreshEntityAsync(object entity);
	Task<bool> RefreshSelectedEntityAsync();
}

