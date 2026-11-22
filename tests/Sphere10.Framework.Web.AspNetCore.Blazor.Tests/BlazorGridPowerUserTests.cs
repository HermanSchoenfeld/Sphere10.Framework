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
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;
using ComponentHost = Sphere10.Framework.Web.AspNetCore.Blazor.Tests.ComponentParameterLifecycleTests.ComponentHost;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Verifies the grid's opt-in power-user presentation: density, omitted chrome, icon row commands, the header reload icon, bound selection and opening rows.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorGridPowerUserTests {
	private const DataSourceCapabilities BrowseCapabilities = DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;
	private const string CommandHeaderPattern = "<th\\b[^>]*class=\"sphere10-grid-command-header\"[^>]*>(?<content>.*?)</th>";

	[Test]
	public async Task DefaultGridKeepsItsCaptionToolbarReloadFooterAndNormalDensity() {
		await RenderAsync(Parameters(Source(3)), (_, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(Regex.IsMatch(html, "<caption[^>]*class=\"sphere10-grid-caption\"[^>]*>Data grid</caption>"), Is.True);
			Assert.That(html, Does.Contain("class=\"sphere10-grid-toolbar\"").And.Contain(">Reload</button>"));
			Assert.That(html, Does.Contain("class=\"sphere10-grid-footer\"").And.Contain("3 items"));
			Assert.That(RootClass(html), Is.EqualTo("sphere10-grid"));
			return Task.CompletedTask;
		});
	}

	[TestCase("")]
	[TestCase(null)]
	public async Task EmptyCaptionRendersNoCaptionElement(string caption) {
		var parameters = Parameters(Source(2));
		parameters[nameof(BlazorGrid<Record>.Caption)] = caption;
		await RenderAsync(parameters, (_, _, rendered) => {
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("<caption"));
			return Task.CompletedTask;
		});
	}

	[Test]
	public async Task HeaderReloadIconReplacesTheToolbarRowWhenNothingElseRemainsInIt() {
		var source = Source(2);
		var parameters = Parameters(source);
		parameters[nameof(BlazorGrid<Record>.AllowedCapabilities)] = BrowseCapabilities;
		parameters[nameof(BlazorGrid<Record>.ReloadPlacement)] = BlazorGridReloadPlacement.Header;
		await RenderAsync(parameters, async (grid, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Not.Contain("sphere10-grid-toolbar"), "A toolbar row that holds nothing is omitted.");
			var header = Regex.Match(html, CommandHeaderPattern, RegexOptions.Singleline);
			Assert.That(header.Success, Is.True);
			Assert.That(header.Groups["content"].Value, Does.Contain("class=\"sphere10-grid-icon-button\" title=\"Reload\" aria-label=\"Reload\"").And.Contain("<svg"));
			Assert.That(html, Does.Contain("data-grid-width=\"40\" style=\"width: 40px\""), "A text-style column holding only the reload icon is 40 pixels wide.");
			var reads = source.Reads;
			await grid.RefreshAsync();
			Assert.That(source.Reads, Is.EqualTo(reads + 1));
		});
	}

	[Test]
	public async Task SearchKeepsTheToolbarRowWhenReloadMovesToTheHeader() {
		var parameters = Parameters(Source(2));
		parameters[nameof(BlazorGrid<Record>.AllowedCapabilities)] = BrowseCapabilities | DataSourceCapabilities.CanSearch;
		parameters[nameof(BlazorGrid<Record>.ReloadPlacement)] = BlazorGridReloadPlacement.Header;
		await RenderAsync(parameters, (_, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain("class=\"sphere10-grid-toolbar\"").And.Contain(">Search</button>").And.Not.Contain(">Reload</button>"));
			return Task.CompletedTask;
		});
	}

	[TestCase(BlazorGridFooterMode.Always, 3, true)]
	[TestCase(BlazorGridFooterMode.Paged, 3, false)]
	[TestCase(BlazorGridFooterMode.Paged, 12, true)]
	[TestCase(BlazorGridFooterMode.Hidden, 12, false)]
	public async Task FooterFollowsItsMode(BlazorGridFooterMode mode, int rows, bool shown) {
		var parameters = Parameters(Source(rows));
		parameters[nameof(BlazorGrid<Record>.Footer)] = mode;
		parameters[nameof(BlazorGrid<Record>.PageSize)] = 5;
		await RenderAsync(parameters, (_, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(html.Contains("class=\"sphere10-grid-footer\""), Is.EqualTo(shown));
			Assert.That(html.Contains($"{rows} items"), Is.EqualTo(shown), "The item count belongs to the footer.");
			Assert.That(html.Contains("aria-label=\"Grid pages\""), Is.EqualTo(shown));
			return Task.CompletedTask;
		});
	}

	[Test]
	public async Task IconActionsRenderInANarrowUntitledColumnWithTooltips() {
		var deleted = new List<Record>();
		var parameters = Parameters(Source(2));
		parameters[nameof(BlazorGrid<Record>.ActionStyle)] = BlazorGridActionStyle.Icons;
		parameters[nameof(BlazorGrid<Record>.Actions)] = new[] {
			new GridAction<Record>("Delete record", null, null) { Icon = "fas fa-trash-alt", ActionWorkAsync = record => { deleted.Add(record); return Task.CompletedTask; } },
			new GridAction<Record>("Archive record", null, "/icons/archive.svg")
		};
		await RenderAsync(parameters, (_, _, rendered) => {
			var html = rendered.ToHtmlString();
			var header = Regex.Match(html, CommandHeaderPattern, RegexOptions.Singleline).Groups["content"].Value;
			Assert.That(Regex.IsMatch(header, "^<span[^>]*class=\"sphere10-grid-visually-hidden\"[^>]*>Row commands</span>$"), Is.True, "The column has no visible header text.");
			Assert.That(html, Does.Not.Contain("Actions</th>"));
			Assert.That(html, Does.Contain("data-grid-width=\"60\" style=\"width: 60px\""), "The column is sized to its two icons.");
			var deletes = Regex.Matches(html, "<button\\b[^>]*class=\"sphere10-grid-icon-button\" title=\"Delete record\" aria-label=\"Delete record\"[^>]*>\\s*<span[^>]*class=\"fas fa-trash-alt\" aria-hidden=\"true\"[^>]*></span>");
			Assert.That(deletes, Has.Count.EqualTo(2));
			Assert.That(html, Does.Contain("title=\"Archive record\" aria-label=\"Archive record\"").And.Contain("<img src=\"/icons/archive.svg\" alt "), "An action without an icon class shows its image URL.");
			return Task.CompletedTask;
		});
	}

	[Test]
	public async Task TextActionsKeepTheActionsColumn() {
		var parameters = Parameters(Source(1));
		parameters[nameof(BlazorGrid<Record>.Actions)] = new[] { new GridAction<Record>("Details", record => record, null) };
		await RenderAsync(parameters, (_, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(Regex.IsMatch(html, "<th\\b[^>]*scope=\"col\"[^>]*>Actions</th>"), Is.True);
			Assert.That(html, Does.Contain(">Details</button>").And.Contain("data-grid-width=\"120\" style=\"width: 120px\""));
			return Task.CompletedTask;
		});
	}

	[Test]
	public async Task BuiltInRowCommandsReplaceTheToolbarEditAndDeleteButtons() {
		var opened = new List<Record>();
		var source = Source(2);
		var parameters = Parameters(source);
		parameters[nameof(BlazorGrid<Record>.ActionStyle)] = BlazorGridActionStyle.Icons;
		parameters[nameof(BlazorGrid<Record>.RowCommands)] = BlazorGridRowCommands.Open | BlazorGridRowCommands.Edit | BlazorGridRowCommands.Delete;
		parameters[nameof(BlazorGrid<Record>.OpenCommandIcon)] = "fas fa-pen";
		parameters[nameof(BlazorGrid<Record>.ItemOpened)] = EventCallback.Factory.Create<Record>(new object(), record => opened.Add(record));
		await RenderAsync(parameters, async (grid, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(Regex.Matches(html, "title=\"Open\" aria-label=\"Open\""), Has.Count.EqualTo(2));
			Assert.That(Regex.Matches(html, "title=\"Edit\" aria-label=\"Edit\""), Has.Count.EqualTo(2));
			Assert.That(Regex.Matches(html, "title=\"Delete\" aria-label=\"Delete\""), Has.Count.EqualTo(2));
			Assert.That(html, Does.Contain("class=\"fas fa-pen\" aria-hidden=\"true\""), "A supplied icon class replaces the built-in Open icon.");
			Assert.That(html, Does.Not.Contain(">Edit</button>").And.Not.Contain(">Delete</button>"), "Row commands replace the toolbar's Edit and Delete.");

			// Open selects the row and reports it; Delete selects it and asks through the grid's confirmation.
			var second = grid.Controller.Items[1];
			await grid.OpenItemAsync(second);
			Assert.That(opened, Is.EqualTo(new[] { second }));
			Assert.That(grid.SelectedItem, Is.SameAs(second));
			await Invoke(grid, "DeleteRowAsync", grid.Controller.Items[0]);
			Assert.That(grid.SelectedItem, Is.SameAs(grid.Controller.Items[0]));
			await Render(grid);
			Assert.That(rendered.ToHtmlString(), Does.Contain("aria-label=\"Confirm deletion\""));
			await Invoke(grid, "CancelDelete");
			await Invoke(grid, "EditRowAsync", grid.Controller.Items[1]);
			Assert.That(grid.Controller.IsEditing, Is.True);
			Assert.That(grid.Controller.EditingItem, Is.SameAs(second));
		});
	}

	[Test]
	public async Task RowCommandsNeedTheirCallbackAndCapabilities() {
		var parameters = Parameters(Source(1));
		parameters[nameof(BlazorGrid<Record>.ActionStyle)] = BlazorGridActionStyle.Icons;
		parameters[nameof(BlazorGrid<Record>.RowCommands)] = BlazorGridRowCommands.Open | BlazorGridRowCommands.Delete;
		parameters[nameof(BlazorGrid<Record>.AllowedCapabilities)] = BrowseCapabilities;
		await RenderAsync(parameters, (_, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Not.Contain("title=\"Open\"").And.Not.Contain("title=\"Delete\"").And.Not.Contain("sphere10-grid-command-header"),
				"Open needs ItemOpened and Delete needs the delete capability; without commands no column is rendered.");
			return Task.CompletedTask;
		});
	}

	[Test]
	public async Task DoubleClickOpensARowWhenAnOpenerIsSuppliedAndEditsOtherwise() {
		var opened = new List<Record>();
		var withOpener = Parameters(Source(2));
		withOpener[nameof(BlazorGrid<Record>.ItemOpened)] = EventCallback.Factory.Create<Record>(new object(), record => opened.Add(record));
		await RenderAsync(withOpener, async (grid, _, _) => {
			var row = grid.Controller.Items[1];
			await Invoke(grid, "RowDoubleClickAsync", row);
			Assert.That(opened, Is.EqualTo(new[] { row }));
			Assert.That(grid.Controller.IsEditing, Is.False, "Opening replaces the double-click inline edit.");
			Assert.That(grid.SelectedItem, Is.SameAs(row));
			await Invoke(grid, "RowKeyDownAsync", grid.Controller.Items[0], new KeyboardEventArgs { Key = "Enter" });
			Assert.That(opened, Has.Count.EqualTo(2), "Enter opens the focused row.");
		});
		await RenderAsync(Parameters(Source(2)), async (grid, _, _) => {
			await Invoke(grid, "RowDoubleClickAsync", grid.Controller.Items[0]);
			Assert.That(grid.Controller.IsEditing, Is.True, "Without an opener double-click keeps starting an inline edit.");
		});
	}

	// Clicking a row opens it for editing (owner, 22 November 2026). The browser behavior reports a plain click on a row marked with
	// data-grid-open-row, naming the rendered row set (data-grid-rows) and the row's position in it.
	[Test]
	public async Task RowClickOpenOpensTheClickedRowOnce() {
		var opened = new List<Record>();
		var source = Source(3);
		var parameters = Parameters(source);
		parameters[nameof(BlazorGrid<Record>.RowClick)] = BlazorGridRowClick.Open;
		parameters[nameof(BlazorGrid<Record>.ItemOpened)] = EventCallback.Factory.Create<Record>(new object(), record => opened.Add(record));
		await RenderAsync(parameters, async (grid, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(Regex.Matches(html, "<tr\\b[^>]*data-grid-open-row=\"(?<index>\\d+)\"").Select(match => match.Groups["index"].Value), Is.EqualTo(new[] { "0", "1", "2" }));
			Assert.That(html, Does.Contain("data-grid-open-row=\"0\"").And.Contain("tabindex=\"0\""));
			var rows = RowSet(html);
			await grid.OpenClickedRowAsync(rows, 1);
			Assert.That(opened, Is.EqualTo(new[] { source.Rows[1] }));
			Assert.That(grid.SelectedItem, Is.SameAs(source.Rows[1]), "The click selects the row it opens.");

			// Blazor's own selecting click and the double-click that follows neither open again nor clear the selection.
			await Invoke(grid, "SelectRowAsync", source.Rows[1], new MouseEventArgs { Detail = 1 });
			await Invoke(grid, "RowDoubleClickAsync", source.Rows[1]);
			Assert.That(opened, Has.Count.EqualTo(1));
			Assert.That(grid.SelectedItem, Is.SameAs(source.Rows[1]), "Clicking the selected row keeps it selected.");
			Assert.That(grid.Controller.IsEditing, Is.False);

			// Enter still opens; a click reported for another row set or outside the rows opens nothing.
			await Invoke(grid, "RowKeyDownAsync", source.Rows[2], new KeyboardEventArgs { Key = "Enter" });
			Assert.That(opened.Last(), Is.SameAs(source.Rows[2]));
			await grid.OpenClickedRowAsync(rows + 1, 0);
			await grid.OpenClickedRowAsync(rows, 3);
			await grid.OpenClickedRowAsync(rows, -1);
			Assert.That(opened, Has.Count.EqualTo(2));
		});
	}

	[Test]
	public async Task ClickOnReplacedRowsOpensNothing() {
		var opened = new List<Record>();
		var source = new ArrayDataSource<Record>(RecordsNamed("Old"));
		var parameters = new Dictionary<string, object> {
			["DataSource"] = source,
			["Columns"] = new[] { BlazorGridColumn<Record>.For(record => record.Name) },
			[nameof(BlazorGrid<Record>.PersistSettings)] = false,
			[nameof(BlazorGrid<Record>.RowClick)] = BlazorGridRowClick.Open,
			[nameof(BlazorGrid<Record>.ItemOpened)] = EventCallback.Factory.Create<Record>(new object(), record => opened.Add(record))
		};
		await RenderAsync<BlazorGrid<Record>>(parameters, async (grid, _, rendered) => {
			var before = RowSet(rendered.ToHtmlString());
			source.ReplaceData(RecordsNamed("New"));
			await grid.RefreshAsync(false);
			await Render(grid);
			var after = RowSet(rendered.ToHtmlString());
			Assert.That(after, Is.Not.EqualTo(before), "Replaced rows are numbered afresh.");
			await grid.OpenClickedRowAsync(before, 0);
			Assert.That(opened, Is.Empty, "A click on rows since replaced opens nothing.");
			await grid.OpenClickedRowAsync(after, 0);
			Assert.That(opened.Single().Name, Is.EqualTo("New 1"));
			await Render(grid);
			Assert.That(RowSet(rendered.ToHtmlString()), Is.EqualTo(after), "Rendering the same rows again keeps their number.");
		});
	}

	[Test]
	public async Task RowClickSelectsByDefaultAndOpensOnlyWithAnOpener() {
		var opened = new List<Record>();
		var source = Source(2);
		var withOpener = Parameters(source);
		withOpener[nameof(BlazorGrid<Record>.ItemOpened)] = EventCallback.Factory.Create<Record>(new object(), record => opened.Add(record));
		await RenderAsync(withOpener, async (grid, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Not.Contain("data-grid-open-row").And.Not.Contain("data-grid-rows"), "Select is the default.");
			await grid.OpenClickedRowAsync(1, 0);
			Assert.That(opened, Is.Empty);
			await Invoke(grid, "SelectRowAsync", source.Rows[0], new MouseEventArgs { Detail = 1 });
			Assert.That(grid.SelectedItem, Is.SameAs(source.Rows[0]));
			await Invoke(grid, "SelectRowAsync", source.Rows[0], new MouseEventArgs { Detail = 1 });
			Assert.That(grid.Controller.HasSelection, Is.False, "A select-on-click grid still clears the selection when the selected row is clicked.");
		});
		var withoutOpener = Parameters(Source(2));
		withoutOpener[nameof(BlazorGrid<Record>.RowClick)] = BlazorGridRowClick.Open;
		await RenderAsync(withoutOpener, async (grid, _, rendered) => {
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("data-grid-open-row"), "Open needs ItemOpened; without it a click selects.");
			await grid.OpenClickedRowAsync(1, 0);
			Assert.That(grid.Controller.HasSelection, Is.False);
		});
	}

	// A row opens its record for editing, so the Open command's built-in icon is the edit pencil, never a folder (owner, 22 November 2026).
	[Test]
	public async Task TheBuiltInOpenIconIsTheEditPencil() {
		var parameters = Parameters(Source(1));
		parameters[nameof(BlazorGrid<Record>.ActionStyle)] = BlazorGridActionStyle.Icons;
		parameters[nameof(BlazorGrid<Record>.RowCommands)] = BlazorGridRowCommands.Open | BlazorGridRowCommands.Edit;
		parameters[nameof(BlazorGrid<Record>.ItemOpened)] = EventCallback.Factory.Create<Record>(new object(), _ => { });
		await RenderAsync(parameters, (_, _, rendered) => {
			var html = rendered.ToHtmlString();
			var open = IconPath(html, "Open");
			Assert.That(open, Is.Not.Empty, "The Open command keeps its accessible name and draws an inline icon.");
			Assert.That(open, Is.EqualTo(IconPath(html, "Edit")));
			return Task.CompletedTask;
		});
	}

	// Choice values read as Title Case words (owner, 22 November 2026): an enum cell shows its display text unless its column names a
	// format, and the cell editor's options show it while their values stay the member names.
	[Test]
	public async Task EnumCellsAndEditorOptionsShowDisplayText() {
		var source = new RecordSource(new[] { new Record { Id = 1, Name = "One", Status = RecordStatus.PaymentFailed } });
		var status = BlazorGridColumn<Record>.For(record => record.Status);
		var number = BlazorGridColumn<Record>.For(record => record.Status, "Status Number");
		number.Format = "D";
		number.PreferenceKey = "status-number";
		var parameters = Parameters(source);
		parameters["Columns"] = new[] { BlazorGridColumn<Record>.For(record => record.Name), status, number };
		await RenderAsync(parameters, async (grid, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(Regex.IsMatch(html, "<td\\b[^>]*>Payment Failed</td>"), Is.True);
			Assert.That(Regex.IsMatch(html, "<td\\b[^>]*>1</td>"), Is.True, "A column format still formats the value.");
			grid.BeginEdit(grid.Controller.Items[0]);
			await Render(grid);
			html = rendered.ToHtmlString();
			Assert.That(html, Does.Match("<option value=\"Active\"[^>]*>Active</option>").And.Match("<option value=\"PaymentFailed\"[^>]*>Payment Failed</option>")
				.And.Match("<option value=\"Suspended\"[^>]*>On Hold</option>"));
		});
	}

	// A row's Open icon names the item the row was rendered with. Replacing the source's rows (a data source refresh) before the
	// click is handled once failed the controller's visible-item guard, and the exception ended the Blazor circuit (18 November 2026).
	[Test]
	public async Task RowEventsForReplacedRowsActOnTheCurrentRowOrAreIgnored() {
		var opened = new List<Record>();
		var source = new ArrayDataSource<Record>(RecordsNamed("Old"));
		var parameters = new Dictionary<string, object> {
			["DataSource"] = source,
			["Columns"] = new[] { BlazorGridColumn<Record>.For(record => record.Name) },
			[nameof(BlazorGrid<Record>.PersistSettings)] = false,
			[nameof(BlazorGrid<Record>.ActionStyle)] = BlazorGridActionStyle.Icons,
			[nameof(BlazorGrid<Record>.RowCommands)] = BlazorGridRowCommands.Open,
			[nameof(BlazorGrid<Record>.ItemOpened)] = EventCallback.Factory.Create<Record>(new object(), record => opened.Add(record))
		};
		await RenderAsync<BlazorGrid<Record>>(parameters, async (grid, _, _) => {
			var stale = grid.Controller.Items[1];
			var current = RecordsNamed("New");
			source.ReplaceData(current);
			await grid.RefreshAsync(false);
			Assert.That(grid.Controller.Items, Is.EqualTo(current), "The read replaced the rendered rows with new instances.");

			// By the default reference identity the clicked item is gone: its events do nothing and throw nothing.
			await grid.OpenItemAsync(stale);
			await Invoke(grid, "SelectRowAsync", stale, new MouseEventArgs { Detail = 1 });
			await Invoke(grid, "RowKeyDownAsync", stale, new KeyboardEventArgs { Key = " " });
			Assert.That(opened, Is.Empty);
			Assert.That(grid.Controller.HasSelection, Is.False);

			// By an identity the item still names a displayed row, which is selected and opened.
			grid.Controller.EntityComparer = new ProjectionEqualityComparer<Record, int>(record => record.Id);
			await grid.OpenItemAsync(stale);
			Assert.That(opened, Has.Count.EqualTo(1));
			Assert.That(opened[0], Is.SameAs(current[1]));
			Assert.That(grid.SelectedItem, Is.SameAs(current[1]));
		});
	}

	// A grid spans its viewport (owner, 19 November 2026): a marked column takes the spare width, and when none is marked the widest
	// configured column does, so a grid never ends partway across its card.
	[Test]
	public async Task TheWidestColumnStretchesWhenNoColumnIsMarkedToExpand() {
		var name = BlazorGridColumn<Record>.For(record => record.Name);
		name.Width = 220;
		var id = BlazorGridColumn<Record>.For(record => record.Id);
		id.Width = 80;
		var parameters = new Dictionary<string, object> {
			["DataSource"] = new ArrayDataSource<Record>(RecordsNamed("Row")),
			["Columns"] = new[] { id, name },
			[nameof(BlazorGrid<Record>.PersistSettings)] = false
		};
		await RenderAsync<BlazorGrid<Record>>(parameters, (_, _, rendered) => {
			var expands = Regex.Matches(rendered.ToHtmlString(), "<col [^>]*data-grid-width=\"(?<width>\\d+)\" data-grid-expands=\"(?<expands>true|false)\"")
				.Select(match => (Width: match.Groups["width"].Value, Expands: match.Groups["expands"].Value)).ToArray();
			Assert.That(expands, Is.EqualTo(new[] { ("80", "false"), ("220", "true") }));
			return Task.CompletedTask;
		});
		id.ExpandsToFit = true;
		await RenderAsync<BlazorGrid<Record>>(parameters, (_, _, rendered) => {
			var expands = Regex.Matches(rendered.ToHtmlString(), "data-grid-expands=\"(?<expands>true|false)\"").Select(match => match.Groups["expands"].Value).ToArray();
			Assert.That(expands, Is.EqualTo(new[] { "true", "false" }), "A marked column alone takes the spare width.");
			return Task.CompletedTask;
		});
	}

	// A date never wraps at its hyphens: date and time columns' cells are no-wrap, other columns' are not (22 November 2026).
	[Test]
	public async Task DateColumnsDoNotWrap() {
		var name = BlazorGridColumn<Record>.For(record => record.Name);
		var created = BlazorGridColumn<Record>.For(record => record.Created);
		var parameters = new Dictionary<string, object> {
			["DataSource"] = new ArrayDataSource<Record>([new Record { Id = 1, Name = "Row", Created = new DateTime(2026, 11, 22) }]),
			["Columns"] = new[] { name, created },
			[nameof(BlazorGrid<Record>.PersistSettings)] = false
		};
		await RenderAsync<BlazorGrid<Record>>(parameters, (_, _, rendered) => {
			var cells = Regex.Matches(rendered.ToHtmlString(), "<td(?<attributes>[^>]*)>").Select(match => match.Groups["attributes"].Value).ToArray();
			Assert.That(cells.Count(attributes => attributes.Contains("sphere10-grid-nowrap")), Is.EqualTo(1), "Only the date cell is no-wrap.");
			return Task.CompletedTask;
		});
	}

	// A grid given a row identity opens the current row of an entity whose rendered row a read replaced, so a click that lands
	// during a reload is not lost (19 November 2026).
	[Test]
	public async Task DetailIdentityMatchesRowEventsToTheCurrentRowsAfterARead() {
		var opened = new List<Record>();
		var source = new ArrayDataSource<Record>(RecordsNamed("Old"));
		var parameters = new Dictionary<string, object> {
			["DataSource"] = source,
			["Columns"] = new[] { BlazorGridColumn<Record>.For(record => record.Name) },
			[nameof(BlazorGrid<Record>.PersistSettings)] = false,
			[nameof(BlazorGrid<Record>.ActionStyle)] = BlazorGridActionStyle.Icons,
			[nameof(BlazorGrid<Record>.RowCommands)] = BlazorGridRowCommands.Open,
			[nameof(BlazorGrid<Record>.DetailIdentity)] = new Func<Record, object>(record => record.Id),
			[nameof(BlazorGrid<Record>.ItemOpened)] = EventCallback.Factory.Create<Record>(new object(), record => opened.Add(record))
		};
		await RenderAsync<BlazorGrid<Record>>(parameters, async (grid, _, _) => {
			var stale = grid.Controller.Items[1];
			var current = RecordsNamed("New");
			source.ReplaceData(current);
			await grid.RefreshAsync(false);
			await grid.OpenItemAsync(stale);
			Assert.That(opened, Has.Count.EqualTo(1));
			Assert.That(opened[0], Is.SameAs(current[1]));
			Assert.That(grid.SelectedItem, Is.SameAs(current[1]));
		});
	}

	[Test]
	public async Task CompactDensityIsSelectedPerGridOrByTheApplicationDefault() {
		var compact = Parameters(Source(1));
		compact[nameof(BlazorGrid<Record>.Density)] = BlazorGridDensity.Compact;
		await RenderAsync(compact, (_, _, rendered) => {
			Assert.That(RootClass(rendered.ToHtmlString()), Is.EqualTo("sphere10-grid sphere10-grid-compact"));
			return Task.CompletedTask;
		});
		var options = new ApplicationOptions(gridDensity: BlazorGridDensity.Compact);
		await RenderAsync(Parameters(Source(1)), (_, _, rendered) => {
			Assert.That(RootClass(rendered.ToHtmlString()), Is.EqualTo("sphere10-grid sphere10-grid-compact"), "The application default applies to grids that choose none.");
			return Task.CompletedTask;
		}, options);
		var normal = Parameters(Source(1));
		normal[nameof(BlazorGrid<Record>.Density)] = BlazorGridDensity.Normal;
		await RenderAsync(normal, (_, _, rendered) => {
			Assert.That(RootClass(rendered.ToHtmlString()), Is.EqualTo("sphere10-grid"), "A grid's own choice overrides the application default.");
			return Task.CompletedTask;
		}, options);
	}

	[Test]
	public async Task FillStretchesTheGridToItsContainerInsteadOfAFixedViewportHeight() {
		var parameters = Parameters(Source(2));
		parameters[nameof(BlazorGrid<Record>.Fill)] = true;
		parameters[nameof(BlazorGrid<Record>.Height)] = 300;
		await RenderAsync(parameters, (_, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(RootClass(html), Is.EqualTo("sphere10-grid sphere10-grid-fill"));
			var viewport = Regex.Match(html, "<div\\b[^>]*class=\"sphere10-grid-viewport\"[^>]*>").Value;
			Assert.That(viewport, Does.Not.Contain("style="), "The container, not Height, sizes a filling grid.");
			return Task.CompletedTask;
		});
		await RenderAsync(Parameters(Source(2)), (_, _, rendered) => {
			Assert.That(Regex.Match(rendered.ToHtmlString(), "<div\\b[^>]*class=\"sphere10-grid-viewport\"[^>]*>").Value, Does.Contain("style=\"max-height: 420px"));
			return Task.CompletedTask;
		});
	}

	[Test]
	public void ApplicationBuilderSetsTheGridDensityDefault() {
		var services = new ServiceCollection().AddLogging();
		services.BuildBlazorApplication()
			.WithGridDensity(BlazorGridDensity.Compact)
			.Build();
		using var provider = services.BuildServiceProvider();
		Assert.That(provider.GetRequiredService<ApplicationOptions>().GridDensity, Is.EqualTo(BlazorGridDensity.Compact));
		Assert.That(new ApplicationOptions().GridDensity, Is.EqualTo(BlazorGridDensity.Normal));
	}

	[Test]
	public async Task BoundSelectionDrivesTheGridAndReportsUserSelection() {
		var source = Source(4);
		var reported = new List<Record>();
		var parameters = Parameters(source);
		parameters[nameof(BlazorGrid<Record>.SelectedItem)] = source.Rows[2];
		parameters[nameof(BlazorGrid<Record>.SelectedItemChanged)] = EventCallback.Factory.Create<Record>(new object(), record => reported.Add(record));
		await RenderAsync(parameters, async (grid, host, rendered) => {
			Assert.That(grid.SelectedItem, Is.SameAs(source.Rows[2]), "A bound item is selected once its page is displayed.");
			Assert.That(reported, Is.EqualTo(new[] { source.Rows[2] }));
			Assert.That(rendered.ToHtmlString(), Does.Contain("class=\"sphere10-grid-selected\""));

			// A user selection is reported and an unchanged bound value does not take it back.
			await Invoke(grid, "SelectRowAsync", source.Rows[0], new MouseEventArgs { Detail = 1 });
			Assert.That(reported.Last(), Is.SameAs(source.Rows[0]));
			await host.UpdateAsync(new Dictionary<string, object>(parameters));
			await rendered.QuiescenceTask;
			Assert.That(grid.SelectedItem, Is.SameAs(source.Rows[0]));

			// A changed bound value selects that item, and null clears the selection.
			parameters[nameof(BlazorGrid<Record>.SelectedItem)] = source.Rows[3];
			await host.UpdateAsync(new Dictionary<string, object>(parameters));
			await rendered.QuiescenceTask;
			Assert.That(grid.SelectedItem, Is.SameAs(source.Rows[3]));
			parameters[nameof(BlazorGrid<Record>.SelectedItem)] = null;
			await host.UpdateAsync(new Dictionary<string, object>(parameters));
			await rendered.QuiescenceTask;
			Assert.That(grid.Controller.HasSelection, Is.False);
			Assert.That(reported.Last(), Is.Null);
		});
	}

	[Test]
	public async Task DefaultChildGridsInheritThePowerUserPresentation() {
		var child = new GraphNode<Record>(new Record { Id = 2, Name = "Child" });
		var parent = new GraphNode<Record>(new Record { Id = 1, Name = "Parent" }) { Connections = new[] { child } };
		var source = new ActionGraphDataSource<Record>(new[] { parent }, node => node.Entity.Id);
		var parameters = new Dictionary<string, object> {
			["DataSource"] = source,
			["Columns"] = new[] {
				new BlazorGridColumn<GraphNode<Record>> {
					ColumnName = "Name", PropertyName = nameof(Record.Name), DataType = typeof(string), PropertyValue = node => node.Entity.Name, CanEditCell = false
				}
			},
			[nameof(BlazorGrid<GraphNode<Record>>.Density)] = BlazorGridDensity.Compact,
			[nameof(BlazorGrid<GraphNode<Record>>.ActionStyle)] = BlazorGridActionStyle.Icons,
			[nameof(BlazorGrid<GraphNode<Record>>.Actions)] = new[] { new GridAction<GraphNode<Record>>("Inspect", node => node, null) { Icon = "fas fa-search" } },
			[nameof(BlazorGrid<GraphNode<Record>>.PersistSettings)] = false
		};
		await RenderAsync<BlazorGrid<GraphNode<Record>>>(parameters, async (grid, _, rendered) => {
			await grid.ExpandDetailAsync(grid.Controller.Items.Single());
			await rendered.QuiescenceTask;
			var html = rendered.ToHtmlString();
			Assert.That(Regex.Matches(html, "class=\"sphere10-grid sphere10-grid-compact\""), Has.Count.EqualTo(2), "The nested grid keeps the parent's density.");
			Assert.That(Regex.Matches(html, "class=\"sphere10-grid-command-header\""), Has.Count.EqualTo(2), "Both levels use the untitled icon command column.");
		});
	}

	// Builds the grid parameters for a source with no persisted preferences.
	private static Dictionary<string, object> Parameters(RecordSource source) => new() {
		["DataSource"] = source,
		["Columns"] = new[] { BlazorGridColumn<Record>.For(record => record.Name) },
		[nameof(BlazorGrid<Record>.PersistSettings)] = false
	};

	private static RecordSource Source(int rows) => new(Enumerable.Range(1, rows).Select(index => new Record { Id = index, Name = $"Record {index}" }).ToArray());

	// Two new row instances with the identities 1 and 2.
	private static Record[] RecordsNamed(string prefix) => Enumerable.Range(1, 2).Select(index => new Record { Id = index, Name = $"{prefix} {index}" }).ToArray();

	// The number of the rendered row set, which the browser behavior sends back with a row click.
	private static int RowSet(string html) => int.Parse(Regex.Match(html, "<tbody\\b[^>]*data-grid-rows=\"(?<rows>\\d+)\"").Groups["rows"].Value);

	// The inline SVG path drawn by the first row command with the given name.
	private static string IconPath(string html, string command)
		=> Regex.Match(html, $"<button\\b[^>]*title=\"{command}\" aria-label=\"{command}\"[^>]*>\\s*<svg\\b[^>]*>\\s*<path d=\"(?<path>[^\"]+)\"").Groups["path"].Value;

	private static string RootClass(string html) => Regex.Match(html, "<div\\b[^>]*?class=\"(?<class>sphere10-grid[^\"]*)\"[^>]*aria-busy").Groups["class"].Value;

	// Renders after a private handler was invoked directly, as the browser event that normally calls it would.
	private static Task Render<TItem>(BlazorGrid<TItem> grid) =>
		(Task)typeof(ComponentBase).GetMethod("InvokeAsync", BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(Action) })
			.Invoke(grid, new object[] { (Action)(() => typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(grid, null)) });

	private static async Task Invoke<TItem>(BlazorGrid<TItem> grid, string method, params object[] arguments) {
		var result = typeof(BlazorGrid<TItem>).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(grid, arguments);
		if (result is Task task)
			await task;
	}

	private static Task RenderAsync(Dictionary<string, object> parameters, Func<BlazorGrid<Record>, ComponentHost, HtmlRootComponent, Task> assertions, ApplicationOptions options = null)
		=> RenderAsync<BlazorGrid<Record>>(parameters, assertions, options);

	// Renders the grid in an isolated provider, optionally with application presentation defaults.
	private static async Task RenderAsync<TComponent>(Dictionary<string, object> parameters, Func<TComponent, ComponentHost, HtmlRootComponent, Task> assertions, ApplicationOptions options = null)
		where TComponent : IComponent {
		var services = new ServiceCollection().AddLogging().AddSingleton<IJSRuntime>(new GalleryRenderingTests.TestJsRuntime());
		if (options != null)
			services.AddSingleton(options);
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ComponentHost host = null;
			var rendered = await renderer.RenderComponentAsync<ComponentHost>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ComponentHost.ComponentType)] = typeof(TComponent),
				[nameof(ComponentHost.Parameters)] = parameters,
				[nameof(ComponentHost.Capture)] = (Action<ComponentHost>)(value => host = value)
			}));
			await rendered.QuiescenceTask;
			await assertions((TComponent)host.Component, host, rendered);
		});
	}

	#region Inner Types

	/// <summary>A row used by the power-user scenarios.</summary>
	public sealed class Record {

		/// <summary>The row identity.</summary>
		public int Id { get; set; }

		/// <summary>The displayed name.</summary>
		public string Name { get; set; }

		/// <summary>A choice value shown by the enum display scenarios.</summary>
		public RecordStatus Status { get; set; }

		/// <summary>A date shown by the no-wrap scenario.</summary>
		public DateTime? Created { get; set; }
	}

	/// <summary>The choice values of <see cref="Record.Status"/>.</summary>
	public enum RecordStatus {
		/// <summary>The record is active.</summary>
		Active,

		/// <summary>The record's payment failed.</summary>
		PaymentFailed,

		/// <summary>The record is described as on hold.</summary>
		[System.ComponentModel.Description("On Hold")]
		Suspended
	}

	/// <summary>Serves fixed rows with every CRUD capability and counts reads.</summary>
	public sealed class RecordSource : ListDataSource<Record> {

		/// <summary>Creates the source over the supplied rows.</summary>
		public RecordSource(Record[] rows)
			: base(new ExtendedList<Record>()) {
			Rows = rows;
			List.AddRange(rows);
		}

		/// <summary>The rows in their original order.</summary>
		public Record[] Rows { get; }

		/// <summary>The number of page reads.</summary>
		public int Reads { get; private set; }

		/// <summary>Counts a page read before serving it.</summary>
		public override Task<DataSourceItems<Record>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			Reads++;
			return base.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection);
		}
	}

	#endregion
}
