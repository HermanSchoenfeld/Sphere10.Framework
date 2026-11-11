// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;
using Sphere10.Framework.Windows.Forms.SourceGrid.Selection;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Observes low-level cell commits and selected regions without changing SourceGrid's native ancestry.</summary>
public sealed class SourceGridControlStateEventProvider : ControlStateEventProviderBase<GridVirtual> {
	private readonly CustomEvents _cellEvents = new();
	private IGridSelection _selection;

	/// <summary>Adds one removable controller and follows the current selection object.</summary>
	protected override void RegisterStateChangedListener(GridVirtual control, EventHandlerEx eventHandler) {
		_cellEvents.ValueChanged += CellValueChanged;
		control.Controller.AddController(_cellEvents);
		control.Invalidated += GridInvalidated;
		control.HandleCreated += GridHandleCreated;
		ObserveSelection();
	}

	/// <summary>Removes the controller and exact selection subscription from the borrowed grid.</summary>
	protected override void DeregisterStateChangedListener(GridVirtual control, EventHandlerEx eventHandler) {
		control.Invalidated -= GridInvalidated;
		control.HandleCreated -= GridHandleCreated;
		control.Controller.RemoveController(_cellEvents);
		_cellEvents.ValueChanged -= CellValueChanged;
		if (_selection != null)
			_selection.SelectionChanged -= SelectionChanged;
		_selection = null;
	}

	// Cell values are already stored when the shared controller dispatches this event.
	private void CellValueChanged(object sender, EventArgs args) => NotifyStateChanged();

	// SelectionMode replaces its selection object and invalidates a live grid synchronously.
	private void GridInvalidated(object sender, InvalidateEventArgs args) => ObserveSelection();

	// Handle creation also reconciles configuration performed before a native window existed.
	private void GridHandleCreated(object sender, EventArgs args) => ObserveSelection();

	// Keep one listener on the current selection and release the previous selection immediately.
	private void ObserveSelection() {
		var selection = Control?.Selection;
		if (ReferenceEquals(_selection, selection))
			return;

		if (_selection != null)
			_selection.SelectionChanged -= SelectionChanged;
		_selection = selection;
		if (_selection != null)
			_selection.SelectionChanged += SelectionChanged;
	}

	// Selected-region changes are retained UI state; ordinary cursor hover is not observed.
	private void SelectionChanged(object sender, RangeRegionChangedEventArgs args) => NotifyStateChanged();
}
