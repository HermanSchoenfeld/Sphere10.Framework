// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Sphere10.Framework.Web.AspNetCore.Blazor.Modal;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;

// Owns the row command column, opening rows and the bound selection.
public partial class BlazorGrid<TItem> {
	private const int IconCommandWidth = 26;
	private const string EditIconPath = "M3 17l1-4 9-9 3 3-9 9zM11 6l3 3";
	// Opening a row opens its record for editing, so the Open command shows the edit pencil, never a folder (owner, 22 November 2026).
	private const string OpenIconPath = EditIconPath;
	private const string DeleteIconPath = "M4 6h12M8 6V4h4v2M6 6l1 11h6l1-11M9 9v5M11 9v5";
	private const string ReloadIconPath = "M16 10a6 6 0 1 1-1.8-4.3M16 3v4h-4";
	private bool _selectionBound;
	private bool _selectionRequested;
	private TItem _boundSelection;
	// The rows the last render displayed and their number, which changes whenever the rows do, so a click the browser reports
	// opens the row it was rendered from or nothing.
	private TItem[] _clickRows = Array.Empty<TItem>();
	private int _clickRowsVersion;

	/// <summary>How row commands and their column are presented. Defaults to Text, a 120-pixel column headed "Actions".</summary>
	[Parameter] public BlazorGridActionStyle ActionStyle { get; set; } = BlazorGridActionStyle.Text;

	/// <summary>The built-in Open, Edit and Delete commands shown on each row before the custom <see cref="Actions"/>. Defaults to None.</summary>
	[Parameter] public BlazorGridRowCommands RowCommands { get; set; } = BlazorGridRowCommands.None;

	/// <summary>
	/// What a single click on a row does. Defaults to <see cref="BlazorGridRowClick.Select"/>; <see cref="BlazorGridRowClick.Open"/> with
	/// an <see cref="ItemOpened"/> callback selects and opens the row, except for a click on a control inside the row.
	/// </summary>
	[Parameter] public BlazorGridRowClick RowClick { get; set; } = BlazorGridRowClick.Select;

	/// <summary>An icon CSS class for the Open row command, such as "fas fa-pen". Null uses the grid's built-in edit (pencil) icon.</summary>
	[Parameter] public string OpenCommandIcon { get; set; }

	/// <summary>An icon CSS class for the Edit row command. Null uses the grid's built-in icon.</summary>
	[Parameter] public string EditCommandIcon { get; set; }

	/// <summary>An icon CSS class for the Delete row command. Null uses the grid's built-in icon.</summary>
	[Parameter] public string DeleteCommandIcon { get; set; }

	/// <summary>Opens a row. When supplied, double-click and Enter open the row instead of starting an inline edit; F2 still edits.</summary>
	[Parameter] public EventCallback<TItem> ItemOpened { get; set; }

	// The getter reports the controller's selection and the setter only records a changed request, so this parameter is not an auto-property.
#pragma warning disable BL0007
	/// <summary>The entity currently selected in the grid. Bind it with <c>@bind-SelectedItem</c> to drive a detail panel.</summary>
	/// <remarks>
	/// Reading returns the controller's selection. A supplied value is applied when it changes: null clears the selection and an item
	/// is selected once it is on the displayed page and no row edit is open. An unchanged value never overrides the user's later selection.
	/// </remarks>
	[Parameter]
	public TItem SelectedItem {
		get => Controller.SelectedItem;
		set {
			if (_selectionBound && EqualityComparer<TItem>.Default.Equals(_boundSelection, value))
				return;
			_selectionBound = true;
			_boundSelection = value;
			_selectionRequested = true;
		}
	}
#pragma warning restore BL0007

	// Determines whether the command column is rendered: for row commands, or to hold the header's reload icon.
	private bool ShowCommandColumn => ShowActions || ReloadPlacement == BlazorGridReloadPlacement.Header;

	// The column's minimum width: the original 120 pixels for text, or one icon slot per possible command.
	private int CommandColumnWidth {
		get {
			if (ActionStyle == BlazorGridActionStyle.Text)
				return ShowActions ? 120 : 40;
			return 8 + IconCommandWidth * Math.Max(1, ShowActions ? Actions.Length + VisibleRowCommandCount : 0);
		}
	}

	// Counts the built-in commands available from the source and callbacks.
	private int VisibleRowCommandCount => (ShowOpenRowCommand ? 1 : 0) + (ShowEditRowCommand ? 1 : 0) + (ShowDeleteRowCommand ? 1 : 0);

	private bool ShowOpenRowCommand => RowCommands.HasFlag(BlazorGridRowCommands.Open) && ItemOpened.HasDelegate;

	// Whether a single click opens its row; the grid's browser behavior reports such clicks.
	private bool OpensOnClick => RowClick == BlazorGridRowClick.Open && ItemOpened.HasDelegate;

	private bool ShowEditRowCommand => RowCommands.HasFlag(BlazorGridRowCommands.Edit) && Can(DataSourceCapabilities.CanUpdate);

	private bool ShowDeleteRowCommand => RowCommands.HasFlag(BlazorGridRowCommands.Delete) && Can(DataSourceCapabilities.CanDelete);

	/// <summary>Selects and opens a row through <see cref="ItemOpened"/>.</summary>
	/// <remarks>
	/// The item is matched among the displayed rows by the controller's <see cref="IBlazorGridController{TItem}.EntityComparer"/>
	/// and the displayed row is opened. An item that is no longer displayed, such as a row that a read replaced after it was
	/// rendered, is ignored.
	/// </remarks>
	public async Task OpenItemAsync(TItem item) {
		if (NavigationDisabled || !ItemOpened.HasDelegate || !TryGetVisibleItem(item, out var row))
			return;
		if (!IsSelected(row)) {
			Controller.Select(row);
			await NotifySelectionAsync();
		}
		await ItemOpened.InvokeAsync(row);
	}

	/// <summary>Opens the row a single click landed on. The grid's browser behavior calls it when <see cref="RowClick"/> is Open.</summary>
	/// <param name="rows">The number of the displayed rows the clicked row was rendered with; a click on rows since replaced is ignored.</param>
	/// <param name="index">The clicked row's position among those rows.</param>
	[JSInvokable]
	public Task OpenClickedRowAsync(int rows, int index) {
		if (_disposed || !OpensOnClick || rows != _clickRowsVersion || index < 0 || index >= _clickRows.Length)
			return Task.CompletedTask;
		return OpenItemAsync(_clickRows[index]);
	}

	// Double-click opens the row when an opener is supplied, otherwise it starts the existing inline edit. A grid whose rows open on a
	// single click has already opened the row on the double-click's first click.
	private Task RowDoubleClickAsync(TItem item) {
		if (OpensOnClick)
			return Task.CompletedTask;
		return ItemOpened.HasDelegate ? OpenItemAsync(item) : BeginInlineEditAsync(item);
	}

	// Records the rows being rendered for the browser's row clicks and returns their number, which changes whenever the rows do.
	private int CaptureClickRows() {
		var rows = Controller.Items;
		if (!rows.SequenceEqual(_clickRows)) {
			_clickRows = rows;
			_clickRowsVersion++;
		}
		return _clickRowsVersion;
	}

	// Starts a buffered edit of the row from its Edit command.
	private async Task EditRowAsync(TItem item) {
		if (NavigationDisabled || !TryGetVisibleItem(item, out var row))
			return;
		BeginEdit(row);
		await NotifySelectionAsync();
	}

	// Selects the row and asks for confirmation: icon-style grids use the host's confirmation dialog when a modal host is initialized,
	// and text-style grids, or grids without a modal host, use the grid's own deletion bar.
	private async Task DeleteRowAsync(TItem item) {
		if (NavigationDisabled || !TryGetVisibleItem(item, out var row))
			return;
		if (!IsSelected(row)) {
			Controller.Select(row);
			await NotifySelectionAsync();
		}
		var views = ActionStyle == BlazorGridActionStyle.Icons && Services?.GetService<ModalService>()?.IsInitialized == true ? Services.GetService<ViewService>() : null;
		if (views == null) {
			RequestDelete();
			return;
		}
		_operationError = null;
		if (await views.ConfirmDialogAsync("Delete", "Delete the selected item?", "Delete") && !_disposed && IsSelected(row))
			await ConfirmDeleteAsync();
	}

	// Finds the displayed row of a clicked item by the controller's entity comparer. Row events carry the item their row was rendered
	// with, and a read or refresh can replace the rows before the event is handled: the event then acts on the entity's current row,
	// or is ignored when the entity is no longer displayed, instead of failing the controller's visible-item guard and the circuit.
	private bool TryGetVisibleItem(TItem item, out TItem row) {
		foreach (var candidate in Controller.Items) {
			if (Controller.EntityComparer.Equals(candidate, item)) {
				row = candidate;
				return true;
			}
		}
		row = default;
		return false;
	}

	// Applies a changed bound selection once its item is displayed and no edit or load is in progress.
	private async Task ApplySelectionRequestAsync() {
		if (!_selectionRequested || _disposed || Controller.IsEditing || Controller.IsSaving || Controller.IsLoading)
			return;
		var requested = _boundSelection;
		if (EqualityComparer<TItem>.Default.Equals(requested, default)) {
			_selectionRequested = false;
			if (Controller.HasSelection)
				Controller.ClearSelection();
		} else {
			if (!Controller.Items.Any(item => Controller.EntityComparer.Equals(item, requested)))
				return;
			_selectionRequested = false;
			if (!IsSelected(requested))
				Controller.Select(requested);
		}
		await NotifySelectionAsync();
	}

	// Lists the row's built-in commands, followed by its available custom actions when they are shown as icons.
	private RowCommand[] GetRowCommands(TItem item) {
		var commands = new List<RowCommand>();
		if (ShowOpenRowCommand)
			commands.Add(new RowCommand("Open", OpenCommandIcon, OpenIconPath, null, () => OpenItemAsync(item)));
		if (ShowEditRowCommand)
			commands.Add(new RowCommand("Edit", EditCommandIcon, EditIconPath, null, () => EditRowAsync(item)));
		if (ShowDeleteRowCommand)
			commands.Add(new RowCommand("Delete", DeleteCommandIcon, DeleteIconPath, null, () => DeleteRowAsync(item)));
		if (ActionStyle == BlazorGridActionStyle.Icons)
			foreach (var action in Actions.Where(action => action.IsActionAvailable?.Invoke(item) ?? true))
				commands.Add(new RowCommand(action.Name, action.Icon, null, action.IconURL, () => PerformActionAsync(action, item)));
		return commands.ToArray();
	}

	#region Inner Types

	// A rendered row command: its name is the tooltip and accessible name; an icon class needs the host's icon font, while
	// an icon path is drawn as inline SVG and an image URL as an image. A command with none of them shows its name.
	private sealed record RowCommand(string Name, string IconClass, string IconPath, string ImageUrl, Func<Task> Execute);

	#endregion
}
