// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

// Constructs the control and wires its native events. The other partials share this
// control and its scoped visual state; they do not introduce separate grid owners.
/// <summary>Displays and edits source-backed entities with native selection, paging and user preferences.</summary>
public partial class VisualGrid : UserControlEx, IVisualGrid {
	#region Constructors

	public VisualGrid() {
		State = VisualState.Normal;
		using (EnterVisualState(VisualState.Loading)) {
			InitializeComponent();
			foreach (var pageButton in new ButtonBase[] { _firstPageButton, _previousPageButton, _nextPageButton, _lastPageButton })
				pageButton.ScaleImageToDeviceDpi();
			_pageModeButton.ItemTextProvider = mode => (VisualGridPageMode)mode == VisualGridPageMode.Paged ? "Manual" : mode.ToString();
			_pageModeButton.SetItems(new object[] { VisualGridPageMode.Single, VisualGridPageMode.Auto, VisualGridPageMode.Paged });
			Disposed += (_, _) => ResetRowHeightMeasurements();
			_grid.MouseDown += Grid_MouseDown;
			_grid.KeyDown += Grid_KeyDown;
			_grid.Columns.ColumnWidthChanged += GridColumnWidthChanged;
			_grid.MouseUp += GridPreferenceResizeCompleted;
			var preferenceEvents = new SourceGrid.Cells.Controllers.CustomEvents();
			preferenceEvents.EditEnded += GridPreferenceEditEnded;
			_grid.Controller.AddController(preferenceEvents);
			// Keep SourceGrid's controller order while giving this grid independent mouse-selection settings.
			_grid.Controller.RemoveController(SourceGrid.Cells.Controllers.MouseSelection.Default);
			_grid.Controller.RemoveController(SourceGrid.Cells.Controllers.CellEventDispatcher.Default);
			_grid.Controller.AddController(_mouseSelectionController);
			_grid.Controller.AddController(SourceGrid.Cells.Controllers.CellEventDispatcher.Default);
			//BorderStyle = BorderStyle.None;
			_selectedEntity = null;
			_dataSource = null;
			_sortColumnIndex = 0;
			_sortColumnName = null;
			_sortDirection = SortDirection.Ascending;
			_pageSize = _defaultPageSize;
			_manualPageSize = _defaultPageSize;
			_pageSizeUpDown.Minimum = 1;
			_pageSizeUpDown.Maximum = 9999;
			_pageSizeUpDown.Value = _pageSize;
			_pageMode = VisualGridPageMode.Paged;
			_pageNumber = 0;
			_endPageNumber = 0;
			_totalRecords = 0;
			_rowToEntityMap = new Dictionary<int, object>();
			CalculateEntityToRowLookup();
			_crudCapabilities = DataSourceCapabilities.Default;
			_columnsBindings = Array.Empty<IVisualGridColumn>();
			_entityEditorAdapter = new VisualEntityEditorAdapter<object>();
			_entityEditorType = typeof(VisualEntityEditor);
			_gridContainerPanel.Resize += GridContainerPanel_Resize;
			LeftClickToDeselect = true;
			RightClickForContextMenu = true;
			EnableKeyNavigation = true;
			UseEntityReferenceForLookup = false;
			AutoSelectOnCreate = false;
			_refreshThrottle = new Throttle(TimeSpan.FromMilliseconds(250));
			_grid.MinimumHeight = _defaultRowHeight;
			OrganizeLayout();
		}
		if (!Tools.Runtime.IsDesignMode) {
			_awaitingInitialBinding = true;
			State = VisualState.Loading;
			_layoutPanel.Visible = false;
			UpdateLoadingIndicator();
		}
	}

	#endregion

	#region Properties

	private VisualState State { get; set; }

	#endregion

	#region Methods

	/// <summary>Reports accepted grid operations instead of native drafts, layout and snapshot publication.</summary>
	protected override bool ShouldObserveChildStateChanges(Control child) => false;

	/// <summary>Restores the previous interaction state when the returned scope is disposed.</summary>
	private ActionScope EnterVisualState(VisualState newState) {
		var previousState = State;
		var initialBinding = _awaitingInitialBinding;
		State = newState;
		return Tools.Scope.ExecuteOnDispose(() => {
			State = initialBinding && !_awaitingInitialBinding ? VisualState.Normal : previousState;
			QueueDataSourceNotification();
		});
	}

	#endregion

	#region Inner Types

	private enum VisualState {
		Normal,
		Selecting,
		Loading
	}

	#endregion
}
