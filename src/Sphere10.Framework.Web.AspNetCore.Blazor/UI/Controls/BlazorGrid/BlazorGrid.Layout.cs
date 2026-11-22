// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;

// Owns the grid's density and which of its caption, toolbar and footer rows are shown.
public partial class BlazorGrid<TItem> {

	/// <summary>The grid's row height and paddings. Null uses the application's <see cref="ApplicationOptions.GridDensity"/>, which defaults to Normal.</summary>
	[Parameter] public BlazorGridDensity? Density { get; set; }

	/// <summary>When the footer with the item count, paging mode and pager is shown. Defaults to Always.</summary>
	[Parameter] public BlazorGridFooterMode Footer { get; set; } = BlazorGridFooterMode.Always;

	/// <summary>Where the reload command is offered. Defaults to a Reload button in the toolbar row.</summary>
	/// <remarks>Header places a small reload icon in the end cell of the column header row. The toolbar row is omitted when nothing remains in it.</remarks>
	[Parameter] public BlazorGridReloadPlacement ReloadPlacement { get; set; } = BlazorGridReloadPlacement.Toolbar;

	/// <summary>Stretches the grid to its container's height, ignoring <see cref="Height"/>; the rows scroll beneath a sticky header. Defaults to false.</summary>
	/// <remarks>The container needs a definite height, such as a <c>SplitContainer</c> panel or a sized screen section.</remarks>
	[Parameter] public bool Fill { get; set; }

	// Resolves the host's presentation defaults; a grid rendered without the application services uses Normal density.
	[Inject] private IServiceProvider Services { get; set; }

	// The application's presentation defaults, when the host registered them.
	private ApplicationOptions ApplicationOptions => field ??= Services?.GetService<ApplicationOptions>();

	// The density this grid renders with, inherited by its default nested grids.
	private BlazorGridDensity EffectiveDensity => Density ?? ApplicationOptions?.GridDensity ?? BlazorGridDensity.Normal;

	// The grid root's classes, including the density modifier.
	private string GridClass => "sphere10-grid" + (EffectiveDensity == BlazorGridDensity.Compact ? " sphere10-grid-compact" : null) + (Fill ? " sphere10-grid-fill" : null);

	// An empty caption renders no caption element rather than an empty padded row.
	private bool ShowCaption => !string.IsNullOrEmpty(Caption);

	// The toolbar's New button follows the source's create capability and the grid's NewItem trait.
	private bool ShowNewCommand => Can(DataSourceCapabilities.CanCreate) && (GridType == 0 || GridType.HasFlag(GridTypes.NewItem));

	// The toolbar's Edit button is replaced by the Edit row command when that is shown.
	private bool ShowEditCommand => Can(DataSourceCapabilities.CanUpdate) && !RowCommands.HasFlag(BlazorGridRowCommands.Edit);

	// The toolbar's Delete button is replaced by the Delete row command when that is shown.
	private bool ShowDeleteCommand => Can(DataSourceCapabilities.CanDelete) && Controller.HasSelection && !RowCommands.HasFlag(BlazorGridRowCommands.Delete);

	// The toolbar row is rendered only when it holds a command or the search box.
	private bool ShowToolbar => Can(DataSourceCapabilities.CanSearch) || ReloadPlacement == BlazorGridReloadPlacement.Toolbar
		|| ShowNewCommand || ShowEditCommand || ShowDeleteCommand || Preferences != null;

	// The footer follows its mode; Paged mode hides it while every result fits one page.
	private bool ShowFooter => Footer switch {
		BlazorGridFooterMode.Always => true,
		BlazorGridFooterMode.Paged => Can(DataSourceCapabilities.CanPage) && Controller.PageMode != BlazorGridPageMode.Single && TotalPages > 1,
		_ => false
	};
}
