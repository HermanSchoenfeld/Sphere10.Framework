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
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;
using ComponentHost = Sphere10.Framework.Web.AspNetCore.Blazor.Tests.ComponentParameterLifecycleTests.ComponentHost;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorGridRenderingTests {

	// Records the source arguments used for a grid query.
	private readonly record struct SourceRequest(int Length, int Page, string Sort, SortDirection Direction);

	/// <summary>Verifies settings button requires values and its trait controls editing.</summary>
	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task SettingsButtonRequiresValuesAndItsTraitControlsEditing(bool hasSettings, bool allowsSettingsChange) {
		var parameters = GridParameters(new ProbeSource<Row>(new Row { Name = "A" }), BlazorGridColumn<Row>.For(row => row.Name));
		if (hasSettings)
			parameters[nameof(BlazorGrid<Row>.Preferences)] = new BlazorGridSettings();
		if (!allowsSettingsChange)
			parameters[nameof(BlazorGrid<Row>.GridType)] = BlazorGrid<Row>.GridTypes.ActionColumn | BlazorGrid<Row>.GridTypes.NewItem;
		await RenderAsync<BlazorGrid<Row>>(parameters, (_, _, rendered) => {
			var button = Regex.Match(rendered.ToHtmlString(), "<button[^>]*>Settings</button>");
			Assert.That(button.Success, Is.EqualTo(hasSettings));
			if (hasSettings)
				Assert.That(button.Value.Contains("disabled"), Is.EqualTo(!allowsSettingsChange));
			return Task.CompletedTask;
		});
	}

	/// <summary>Verifies paging modes expose only the applicable footer controls.</summary>
	[TestCase(BlazorGridPageMode.Paged, true, true)]
	[TestCase(BlazorGridPageMode.Auto, true, false)]
	[TestCase(BlazorGridPageMode.Single, false, false)]
	public async Task PagingModesExposeOnlyTheApplicableFooterControls(BlazorGridPageMode mode, bool navigator, bool manualSize) {
		var parameters = GridParameters(new ProbeSource<Row>(new Row { Name = "A" }));
		parameters[nameof(BlazorGrid<Row>.PageMode)] = mode;
		await RenderAsync<BlazorGrid<Row>>(parameters, (grid, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(html.Contains("aria-label=\"Grid pages\""), Is.EqualTo(navigator));
			Assert.That(html.Contains("aria-label=\"Rows per page\""), Is.EqualTo(manualSize));
			var label = mode == BlazorGridPageMode.Paged ? "Manual" : mode.ToString();
			Assert.That(html, Does.Contain("aria-label=\"Paging mode\"").And.Contain("aria-haspopup=\"menu\"").And.Contain($"title=\"Paging mode: {label}\""));
			Assert.That(grid.Controller.PageMode, Is.EqualTo(mode));
			return Task.CompletedTask;
		});
	}

	/// <summary>Verifies caption is visible with explicit alignment and sortable headers reserve their indicator.</summary>
	[TestCase(BlazorGridHorizontalAlignment.Left, "left")]
	[TestCase(BlazorGridHorizontalAlignment.Center, "center")]
	[TestCase(BlazorGridHorizontalAlignment.Right, "right")]
	public async Task CaptionIsVisibleWithExplicitAlignmentAndSortableHeadersReserveTheirIndicator(BlazorGridHorizontalAlignment alignment, string css) {
		var parameters = GridParameters(new ProbeSource<Row>(new Row { Name = "A" }), BlazorGridColumn<Row>.For(row => row.Name));
		parameters[nameof(BlazorGrid<Row>.Caption)] = "Live Processes";
		parameters[nameof(BlazorGrid<Row>.CaptionAlignment)] = alignment;
		await RenderAsync<BlazorGrid<Row>>(parameters, (_, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(Regex.IsMatch(html, $"<caption(?=[^>]*class=\"sphere10-grid-caption\")(?=[^>]*style=\"text-align: {css}\")[^>]*>Live Processes</caption>"), Is.True);
			Assert.That(html, Does.Contain("class=\"sphere10-grid-sort-indicator\" aria-hidden=\"true\""));
			return Task.CompletedTask;
		});
	}

	/// <summary>Verifies image cells reuse framework resize modes and alignment.</summary>
	[TestCase(null, ResizeAlignment.CenterCenter, "none", "center center")]
	[TestCase(ResizeMethod.Stretch, ResizeAlignment.TopLeft, "fill", "left top")]
	[TestCase(ResizeMethod.AspectFill, ResizeAlignment.BottomRight, "cover", "right bottom")]
	[TestCase(ResizeMethod.AspectFit, ResizeAlignment.TopCenter, "contain", "center top")]
	[TestCase(ResizeMethod.AspectFitPadded, ResizeAlignment.CenterLeft, "contain", "left center")]
	public async Task ImageCellsReuseFrameworkResizeModesAndAlignment(ResizeMethod? method, ResizeAlignment alignment, string fit, string position) {
		var column = BlazorGridColumn<Row>.For(row => row.Name);
		column.ImageSource = row => "/icons/" + row.Name + ".svg";
		column.ImageAlternativeText = row => row.Name + " application";
		column.ImageAlignment = alignment;
		column.ImageResizeMethod = method;
		await RenderAsync<BlazorGrid<Row>>(GridParameters(new ProbeSource<Row>(new Row { Name = "sample" }), column), (_, _, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain("src=\"/icons/sample.svg\"").And.Contain("alt=\"sample application\""));
			Assert.That(html, Does.Contain($"object-fit: {fit}; object-position: {position}"));
			return Task.CompletedTask;
		});
	}

	/// <summary>Verifies loading surface covers the viewport initially and suppressed refresh leaves rows usable.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task LoadingSurfaceCoversTheViewportInitiallyAndSuppressedRefreshLeavesRowsUsable(bool suppressIndicator) {
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var first = new Row { Name = "First row" };
		var second = new Row { Name = "Second row" };
		var source = new ProbeSource<Row>(first, second) { ReadGate = gate.Task };
		var parameters = GridParameters(source);
		parameters[nameof(BlazorGrid<Row>.SuppressLoadingIndicator)] = suppressIndicator;
		await using var provider = new ServiceCollection().AddLogging().AddSingleton<IJSRuntime>(new GalleryRenderingTests.TestJsRuntime()).BuildServiceProvider();
		await using var settings = new BlazorSettingsProvider(provider, "grid-rendering-fixture");
		using var settingsScope = UserSettings.UseProvider(settings);
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ComponentHost host = null;
			var rendered = renderer.BeginRenderingComponent<ComponentHost>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ComponentHost.ComponentType)] = typeof(BlazorGrid<Row>),
				[nameof(ComponentHost.Parameters)] = parameters,
				[nameof(ComponentHost.Capture)] = (Action<ComponentHost>)(value => host = value)
			}));
			Assert.That(rendered.ToHtmlString(), Does.Contain("class=\"sphere10-grid-loading\"").And.Contain("aria-label=\"Loading grid\""));
			gate.SetResult();
			await rendered.QuiescenceTask;
			var grid = (BlazorGrid<Row>)host.Component;
			grid.Controller.Select(first);
			gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			source.ReadGate = gate.Task;
			var refresh = grid.RefreshAsync();
			var html = rendered.ToHtmlString();
			Assert.That(html.Contains("class=\"sphere10-grid-loading\""), Is.EqualTo(!suppressIndicator));
			Assert.That(html, Does.Contain("First row").And.Contain("Second row"));
			if (suppressIndicator) {
				grid.Controller.Select(second);
				Assert.That(grid.SelectedItem, Is.SameAs(second));
				Assert.That(html, Does.Not.Contain("aria-busy=\"true\""));
			}
			gate.SetResult();
			await refresh;
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("class=\"sphere10-grid-loading\""));
		});
	}

	/// <summary>Verifies explicit browser key restores once and only manual actions write preferences.</summary>
	[Test]
	public async Task ExplicitBrowserKeyRestoresOnceAndOnlyManualActionsWritePreferences() {
		var runtime = new GridJsRuntime();
		runtime.Module.Behavior.StoredSettings["orders"] = new BlazorGridSettings {
			PageMode = BlazorGridPageMode.Auto,
			PageSize = 7,
			PageNumber = 1
		};
		var parameters = GridParameters(new ProbeSource<Row>(Enumerable.Range(1, 40).Select(index => new Row { Name = "Row " + index }).ToArray()));
		parameters[nameof(BlazorGrid<Row>.SettingsKey)] = "orders";
		await RenderAsync<BlazorGrid<Row>>(parameters, async (grid, _, _) => {
			await grid.LoadJavascript();
			await grid.LoadJavascript();
			Assert.That(runtime.Module.Behavior.SettingsReads, Is.EqualTo(new[] { "orders" }));
			Assert.That(grid.Controller.PageMode, Is.EqualTo(BlazorGridPageMode.Auto));
			Assert.That(grid.Controller.ManualPageSize, Is.EqualTo(7));
			Assert.That(grid.CurrentPage, Is.EqualTo(1));
			await grid.SetViewportPageSizeAsync(4);
			Assert.That(runtime.Module.TypedWriteKeys, Is.Empty, "Viewport measurements must never write user preferences.");
			await grid.SetPageModeAsync(BlazorGridPageMode.Paged);
			Assert.That(runtime.Module.TypedWriteKeys.Count, Is.EqualTo(1));
			Assert.That(runtime.Module.TypedWriteKeys[0], Does.EndWith("/orders"));
			Assert.That((await UserSettings.GetAsync<BlazorGridSettings>("orders")).PageSize, Is.EqualTo(7), "The saved size is the manual preference, not the automatic capacity.");
		}, runtime);
	}

	/// <summary>Verifies typed user settings take precedence over the legacy browser record.</summary>
	[Test]
	public async Task TypedUserSettingsTakePrecedenceOverTheLegacyBrowserRecord() {
		var runtime = new GridJsRuntime();
		runtime.Module.Behavior.StoredSettings["orders"] = new BlazorGridSettings { PageSize = 9 };
		var parameters = GridParameters(new ProbeSource<Row>(Enumerable.Range(1, 40).Select(index => new Row { Name = "Row " + index }).ToArray()));
		parameters[nameof(BlazorGrid<Row>.SettingsKey)] = "orders";
		await RenderAsync<BlazorGrid<Row>>(parameters, async (grid, _, _) => {
			var settings = await UserSettings.GetAsync<BlazorGridSettings>("orders");
			settings.PageSize = 3;
			settings.Columns = new[] { new ApplicationGridColumnPreferences { Key = "Name", Title = "Name", Width = 215 } };
			await settings.SaveAsync();
			runtime.Module.TypedWriteKeys.Clear();
			await grid.LoadJavascript();
			Assert.That(grid.Controller.ManualPageSize, Is.EqualTo(3));
			Assert.That(grid.CaptureGridPreferences().Columns.Single(column => column.Key == "Name").Width, Is.EqualTo(215));
			Assert.That(runtime.Module.Behavior.SettingsReads, Is.Empty, "A typed root owns this preference once it exists.");
			Assert.That(runtime.Module.TypedWriteKeys, Is.Empty, "Restoring preferences must not save them.");
		}, runtime);
	}

	/// <summary>A damaged typed record does not prevent a completed paging action or get overwritten by it.</summary>
	[Test]
	public async Task CorruptTypedPreferencesPreserveDefaultsAndDoNotAbortManualPaging() {
		var runtime = new GridJsRuntime();
		var parameters = GridParameters(new ProbeSource<Row>(Enumerable.Range(1, 40).Select(index => new Row { Name = "Row " + index }).ToArray()));
		parameters[nameof(BlazorGrid<Row>.SettingsKey)] = "orders";
		await RenderAsync<BlazorGrid<Row>>(parameters, async (grid, _, _) => {
			var settings = await UserSettings.GetAsync<BlazorGridSettings>("orders");
			await settings.SaveAsync();
			var key = runtime.Module.TypedRecords.Keys.Single();
			runtime.Module.TypedRecords[key] = "{invalid JSON";
			runtime.Module.TypedWriteKeys.Clear();

			await grid.LoadJavascript();
			Assert.That(grid.Controller.ManualPageSize, Is.EqualTo(10));
			Assert.That(runtime.Module.Behavior.SettingsReads, Is.Empty, "A damaged typed root must not import a retired legacy record.");

			await grid.SetPageSizeAsync(4);

			Assert.That(grid.Controller.ManualPageSize, Is.EqualTo(4), "Optional preference failures must not abort the user’s paging action.");
			Assert.That(grid.Controller.Items, Has.Length.EqualTo(4));
			Assert.That(runtime.Module.TypedWriteKeys, Is.Empty);
			Assert.That(runtime.Module.TypedRecords[key], Is.EqualTo("{invalid JSON"));
		}, runtime);
	}

	/// <summary>Verifies owned preferences seed the first query and never use the separate browser record.</summary>
	[Test]
	public async Task OwnedPreferencesSeedTheFirstQueryAndNeverUseTheSeparateBrowserRecord() {
		var runtime = new GridJsRuntime();
		var name = BlazorGridColumn<Row>.For(row => row.Name, "Renamed heading");
		var enabled = BlazorGridColumn<Row>.For(row => row.Enabled);
		var source = new ProbeSource<Row>(Enumerable.Range(0, 20).Select(index => new Row { Name = "Row " + index }).ToArray());
		var values = new BlazorGridSettings {
			ColumnWidths = new Dictionary<string, double> { ["Name"] = 999 },
			PageMode = BlazorGridPageMode.Paged, PageSize = 3, PageNumber = 2,
			SortColumnName = "Name", SortDirection = SortDirection.Descending,
			Columns = new[] {
				new ApplicationGridColumnPreferences { Key = "Name", Title = "Old heading", Width = 185 },
				new ApplicationGridColumnPreferences { Key = "Optional", Title = "Optional detail", Width = 81 },
				new ApplicationGridColumnPreferences { Key = "Hidden", Title = "Hidden detail" }
			}
		};
		var changes = new List<BlazorGridSettings>();
		var parameters = GridParameters(source, name, enabled);
		parameters[nameof(BlazorGrid<Row>.SettingsKey)] = "owned";
		parameters[nameof(BlazorGrid<Row>.Preferences)] = values;
		parameters[nameof(BlazorGrid<Row>.PreferencesChanged)] = EventCallback.Factory.Create<BlazorGridSettings>(this, changes.Add);
		await RenderAsync<BlazorGrid<Row>>(parameters, async (grid, _, rendered) => {
			Assert.That(source.Requests.First(), Is.EqualTo(new SourceRequest(3, 2, "Name", SortDirection.Descending)));
			Assert.That(rendered.ToHtmlString(), Does.Contain("data-grid-key=\"Name\" style=\"width: 185px\""), "Restored geometry belongs in the first HTML, before any JS autosizing.");
			await grid.LoadJavascript();
			await grid.SetColumnWidthAsync("Name", 211);
			Assert.That(changes, Has.Count.EqualTo(1));
			Assert.That(changes[0].Columns.Single(column => column.Key == "Name").Width, Is.EqualTo(211));
			Assert.That(changes[0].Columns.Single(column => column.Key == "Enabled").Width, Is.Null);
			Assert.That(changes[0].Columns.Single(column => column.Key == "Name").Title, Is.EqualTo("Renamed heading"));
			Assert.That(changes[0].Columns.Single(column => column.Key == "Optional").Width, Is.EqualTo(81));
			Assert.That(changes[0].Columns.Single(column => column.Key == "Hidden").Width, Is.Null);
			changes[0].Columns[0].Width = 999;
			Assert.That(grid.CaptureGridPreferences().Columns[0].Width, Is.EqualTo(211), "The published record must be detached.");
			Assert.That(values.Columns[0].Width, Is.EqualTo(185));
			Assert.That(runtime.Module.Behavior.SettingsReads, Is.Empty);
			Assert.That(runtime.Module.TypedWriteKeys, Is.Empty);
		}, runtime);
	}

	/// <summary>Verifies standalone restore precedes its first data query.</summary>
	[Test]
	public async Task StandaloneRestorePrecedesItsFirstDataQuery() {
		var runtime = new GridJsRuntime();
		runtime.Module.Behavior.StoredSettings["first-read"] = new BlazorGridSettings {
			PageMode = BlazorGridPageMode.Paged, PageSize = 4, PageNumber = 1,
			SortColumnName = "Name", SortDirection = SortDirection.Ascending
		};
		var source = new ProbeSource<Row>(Enumerable.Range(0, 20).Select(index => new Row { Name = "Row " + index }).ToArray());
		var parameters = GridParameters(source);
		parameters[nameof(BlazorGrid<Row>.SettingsKey)] = "first-read";
		await RenderAsync<BlazorGrid<Row>>(parameters, async (grid, _, _) => {
			Assert.That(source.Requests, Is.Empty, "The browser value is needed before a standalone keyed grid can issue a query.");
			await grid.LoadJavascript();
			Assert.That(source.Requests, Is.EqualTo(new[] { new SourceRequest(4, 1, "Name", SortDirection.Ascending) }));
			Assert.That(runtime.Module.TypedWriteKeys, Is.Empty);
		}, runtime);
	}

	/// <summary>Verifies changing astandalone key preserves the draft and removing it stops writes.</summary>
	[Test]
	public async Task ChangingAStandaloneKeyPreservesTheDraftAndRemovingItStopsWrites() {
		var runtime = new GridJsRuntime();
		runtime.Module.Behavior.StoredSettings["old-key"] = new BlazorGridSettings { PageSize = 5 };
		runtime.Module.Behavior.StoredSettings["new-key"] = new BlazorGridSettings { PageSize = 2 };
		var column = BlazorGridColumn<Row>.For(row => row.Name);
		var source = new ProbeSource<Row>(Enumerable.Range(0, 20).Select(index => new Row { Name = "Row " + index }).ToArray());
		var parameters = GridParameters(source, column);
		parameters[nameof(BlazorGrid<Row>.SettingsKey)] = "old-key";
		await RenderAsync<BlazorGrid<Row>>(parameters, async (grid, _, _) => {
			await grid.LoadJavascript();
			var row = grid.Controller.Items[0];
			grid.BeginEdit(row);
			grid.Controller.SetEditValue(column, "Retained standalone draft");
			parameters[nameof(BlazorGrid<Row>.SettingsKey)] = "new-key";
			await grid.SetParametersAsync(ParameterView.FromDictionary(parameters));
			await grid.LoadJavascript();
			Assert.That(grid.Controller.IsEditing, Is.True);
			Assert.That(grid.Controller.GetEditValue(column), Is.EqualTo("Retained standalone draft"));
			Assert.That(source.Requests, Has.Count.EqualTo(1));
			Assert.That(await grid.SaveAsync(), Is.True);
			Assert.That(grid.Controller.ManualPageSize, Is.EqualTo(2));
			Assert.That(row.Name, Is.EqualTo("Retained standalone draft"));
			parameters[nameof(BlazorGrid<Row>.SettingsKey)] = null;
			await grid.SetParametersAsync(ParameterView.FromDictionary(parameters));
			await grid.SetPageSizeAsync(4);
			Assert.That(runtime.Module.TypedWriteKeys, Is.Empty);
			Assert.That(runtime.Module.Behavior.SettingsReads, Is.EqualTo(new[] { "old-key", "new-key" }));
		}, runtime);
	}

	/// <summary>Verifies accepted preferences preserve the row draft and defer the query until save.</summary>
	[Test]
	public async Task AcceptedPreferencesPreserveTheRowDraftAndDeferTheQueryUntilSave() {
		var row = new Row { Name = "Original" };
		var column = BlazorGridColumn<Row>.For(value => value.Name);
		var source = new ProbeSource<Row>(row, new Row { Name = "Second" });
		var parameters = GridParameters(source, column);
		parameters[nameof(BlazorGrid<Row>.Preferences)] = new BlazorGridSettings { PageSize = 10 };
		await RenderAsync<BlazorGrid<Row>>(parameters, async (grid, _, _) => {
			grid.BeginEdit(row);
			grid.Controller.SetEditValue(column, "Unsaved draft");
			await grid.ApplyGridPreferencesAsync(new BlazorGridSettings {
				PageSize = 1, PageMode = BlazorGridPageMode.Paged,
				Columns = new[] { new ApplicationGridColumnPreferences { Key = "Name", Width = 203 } }
			});
			Assert.That(grid.Controller.IsEditing, Is.True);
			Assert.That(grid.Controller.GetEditValue(column), Is.EqualTo("Unsaved draft"));
			Assert.That(row.Name, Is.EqualTo("Original"));
			Assert.That(source.Requests, Has.Count.EqualTo(1));
			Assert.That(grid.CaptureGridPreferences().Columns.Single().Width, Is.EqualTo(203));
			Assert.That(await grid.SaveAsync(), Is.True);
			Assert.That(row.Name, Is.EqualTo("Unsaved draft"));
			Assert.That(grid.Controller.ManualPageSize, Is.EqualTo(1));
			Assert.That(source.Requests.Last().Length, Is.EqualTo(1));
		});
	}

	/// <summary>Verifies completed paging publishes before the delayed read finishes.</summary>
	[Test]
	public async Task CompletedPagingPublishesBeforeTheDelayedReadFinishes() {
		var source = new ProbeSource<Row>(Enumerable.Range(0, 30).Select(index => new Row { Name = "Row " + index }).ToArray());
		var changes = new List<BlazorGridSettings>();
		var parameters = GridParameters(source);
		parameters[nameof(BlazorGrid<Row>.Preferences)] = new BlazorGridSettings { PageSize = 3 };
		parameters[nameof(BlazorGrid<Row>.PreferencesChanged)] = EventCallback.Factory.Create<BlazorGridSettings>(this, changes.Add);
		await RenderAsync<BlazorGrid<Row>>(parameters, async (grid, _, _) => {
			var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			using var release = Tools.Scope.ExecuteOnDispose(() => gate.TrySetResult());
			source.ReadGate = gate.Task;
			var change = grid.SetPageAsync(1);
			Assert.That(change.IsCompleted, Is.False);
			Assert.That(changes, Has.Count.EqualTo(1));
			Assert.That(changes[0].PageNumber, Is.EqualTo(1));
			gate.SetResult();
			await change;
		});
	}

	/// <summary>Verifies edit action text can describe draft application without changing default behavior.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task EditActionTextCanDescribeDraftApplicationWithoutChangingDefaultBehavior(bool customizeText) {
		var row = new Row { Name = "Original" };
		var source = new ProbeSource<Row>(row);
		var parameters = GridParameters(source);
		var buttonText = customizeText ? "Apply row" : "Save";
		var notice = customizeText ? "Apply this row to the draft. Save the project to persist it." : "Changes are saved only when you choose Save.";
		if (customizeText) {
			parameters[nameof(BlazorGrid<Row>.SaveButtonText)] = buttonText;
			parameters[nameof(BlazorGrid<Row>.EditNotice)] = notice;
		}
		await RenderAsync<BlazorGrid<Row>>(parameters, (grid, _, rendered) => {
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain(notice));
			grid.BeginEdit(row);
			Assert.That(rendered.ToHtmlString(), Does.Contain($">{buttonText}</button>").And.Contain(notice));
			if (customizeText)
				Assert.That(rendered.ToHtmlString(), Does.Not.Contain(">Save</button>").And.Not.Contain("Changes are saved only when you choose Save."));
			Assert.That(source.Updated, Is.Zero, "Changing editor wording does not persist a row.");
			grid.CancelEdit();
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain(notice));
			Assert.That(row.Name, Is.EqualTo("Original"));
			return Task.CompletedTask;
		});
	}

	/// <summary>Verifies commands respect both source capabilities and allowed mask.</summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task CommandsRespectBothSourceCapabilitiesAndAllowedMask(bool restrictSource) {
		var row = new Row { Name = "Visible" };
		var source = new ProbeSource<Row>(row) {
			AvailableCapabilities = restrictSource ? DataSourceCapabilities.CanRead : DataSourceCapabilities.Default
		};
		var parameters = GridParameters(source);
		parameters["AllowedCapabilities"] = restrictSource ? DataSourceCapabilities.Default : DataSourceCapabilities.CanRead;
		await RenderAsync<BlazorGrid<Row>>(parameters, (grid, _, rendered) => {
			grid.Controller.Select(row);
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain("Visible"));
			Assert.That(html, Does.Contain(">Reload</button>"));
			Assert.That(html, Does.Not.Contain(">New</button>"));
			Assert.That(html, Does.Not.Contain(">Edit</button>"));
			Assert.That(html, Does.Not.Contain(">Delete</button>"));
			Assert.That(html, Does.Not.Contain("type=\"search\""));
			Assert.That(html, Does.Not.Contain("Rows per page"));
			return Task.CompletedTask;
		});
	}

	/// <summary>Verifies pager uses accessible icons and preserves the current size alongside configured choices.</summary>
	[Test]
	public async Task PagerUsesAccessibleIconsAndPreservesTheCurrentSizeAlongsideConfiguredChoices() {
		var parameters = GridParameters(new ProbeSource<Row>(Enumerable.Range(1, 20).Select(index => new Row { Name = $"Row {index}" }).ToArray()));
		parameters["Id"] = "pager-grid";
		parameters["PageSize"] = 6;
		parameters["PageSizeOptions"] = new[] { 25, 10, 10, 5 };
		await RenderAsync<BlazorGrid<Row>>(parameters, async (grid, host, rendered) => {
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Not.Contain("<form"), "The grid must be usable inside an application edit form without introducing a nested form.");
			var selector = Regex.Match(html, "<select[^>]*id=\"pager-grid-page-size\"[^>]*>(.*?)</select>", RegexOptions.Singleline);
			Assert.That(selector.Success, Is.True);
			Assert.That(Regex.Matches(selector.Value, "<option[^>]*value=\"([0-9]+)\"").Select(match => int.Parse(match.Groups[1].Value)),
				Is.EqualTo(new[] { 5, 6, 10, 25 }));
			Assert.That(Regex.IsMatch(selector.Value, "<option(?=[^>]*value=\"6\")(?=[^>]*selected)[^>]*>"), Is.True);
			foreach (var label in new[] { "First page", "Previous page", "Next page", "Last page" }) {
				var button = Regex.Match(html, $"<button[^>]*aria-label=\"{label}\"[^>]*>(.*?)</button>", RegexOptions.Singleline);
				Assert.That(button.Success, Is.True, label);
				Assert.That(button.Value, Does.Contain($"title=\"{label}\"").And.Contain("<svg").And.Contain("aria-hidden=\"true\""));
			}
			await grid.SetPageSizeAsync(10);
			parameters["Caption"] = "Rerendered grid";
			await host.UpdateAsync(parameters);
			Assert.That(grid.Controller.PageSize, Is.EqualTo(10), "An unrelated parameter update must preserve the user's selected size.");
			Assert.That(Regex.IsMatch(rendered.ToHtmlString(), "<option(?=[^>]*value=\"10\")(?=[^>]*selected)[^>]*>"), Is.True);
		});
	}

	/// <summary>Verifies page size dropdown honors editing and automatic sizing.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task PageSizeDropdownHonorsEditingAndAutomaticSizing(bool automaticSize) {
		var row = new Row { Name = "Editable" };
		var parameters = GridParameters(new ProbeSource<Row>(row));
		parameters["AutoPageSize"] = automaticSize;
		await RenderAsync<BlazorGrid<Row>>(parameters, (grid, _, rendered) => {
			grid.BeginEdit(row);
			var html = rendered.ToHtmlString();
			var selector = Regex.Match(html, "<select[^>]*aria-label=\"Rows per page\"[^>]*>");
			Assert.That(selector.Success, Is.EqualTo(!automaticSize));
			if (!automaticSize)
				Assert.That(selector.Value, Does.Contain("disabled"));
			Assert.That(Regex.IsMatch(html, "<button(?=[^>]*aria-label=\"Next page\")(?=[^>]*disabled)[^>]*>"), Is.True);
			grid.CancelEdit();
			if (!automaticSize)
				Assert.That(Regex.Match(rendered.ToHtmlString(), "<select[^>]*aria-label=\"Rows per page\"[^>]*>").Value, Does.Not.Contain("disabled"));
			return Task.CompletedTask;
		});
	}

	/// <summary>Verifies delete command requires avisible selection.</summary>
	[Test]
	public async Task DeleteCommandRequiresAVisibleSelection() {
		var row = new Row { Name = "Selected" };
		await RenderAsync<BlazorGrid<Row>>(GridParameters(new ProbeSource<Row>(row)), (grid, _, rendered) => {
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain(">Delete</button>"));
			grid.Controller.Select(row);
			Assert.That(rendered.ToHtmlString(), Does.Contain(">Delete</button>"));
			grid.Controller.ClearSelection();
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain(">Delete</button>"));
			return Task.CompletedTask;
		});
	}

	/// <summary>Verifies typed display and checkbox remain read only until explicit editing.</summary>
	[Test]
	public async Task TypedDisplayAndCheckboxRemainReadOnlyUntilExplicitEditing() {
		var row = new Row { Name = "Ada", Enabled = true };
		var name = BlazorGridColumn<Row>.For(item => item.Name);
		name.Template = item => builder => {
			builder.OpenElement(0, "strong");
			builder.AddContent(1, item.Name);
			builder.CloseElement();
		};
		var enabled = BlazorGridColumn<Row>.For(item => item.Enabled);
		var readOnly = BlazorGridColumn<Row>.For(item => item.ReadOnly);
		var parameters = GridParameters(new ProbeSource<Row>(row), name, enabled, readOnly);
		await RenderAsync<BlazorGrid<Row>>(parameters, (grid, _, rendered) => {
			Assert.That(rendered.ToHtmlString(), Does.Contain("<strong>Ada</strong>"));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("type=\"checkbox\""));
			grid.BeginEdit(row);
			Assert.That(grid.Controller.SelectedItem, Is.SameAs(row));
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain("type=\"checkbox\""));
			Assert.That(html, Does.Contain("value=\"Ada\""));
			Assert.That(html, Does.Contain("Fixed display"));
			Assert.That(html, Does.Not.Contain("value=\"Fixed display\""));
			grid.CancelEdit();
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("type=\"checkbox\""));
			Assert.That(rendered.ToHtmlString(), Does.Contain("<strong>Ada</strong>"));
			return Task.CompletedTask;
		});
	}

	/// <summary>Verifies custom editor buffers values until save and cancel preserves the entity.</summary>
	[Test]
	public async Task CustomEditorBuffersValuesUntilSaveAndCancelPreservesTheEntity() {
		var row = new Row { Name = "Original" };
		var source = new ProbeSource<Row>(row);
		BlazorGridCellEditContext<Row> editor = null;
		var name = BlazorGridColumn<Row>.For(item => item.Name);
		name.EditorTemplate = CaptureEditor(context => editor = context);
		await RenderAsync<BlazorGrid<Row>>(GridParameters(source, name), async (grid, _, rendered) => {
			grid.BeginEdit(row);
			await editor.ValueChanged.InvokeAsync("Cancelled");
			Assert.That(row.Name, Is.EqualTo("Original"));
			Assert.That(rendered.ToHtmlString(), Does.Contain("Cancelled"));
			grid.CancelEdit();
			Assert.That(source.Updated, Is.Zero);
			Assert.That(row.Name, Is.EqualTo("Original"));
			grid.BeginEdit(row);
			await editor.ValueChanged.InvokeAsync("Saved");
			Assert.That(await grid.SaveAsync(), Is.True);
			Assert.That(row.Name, Is.EqualTo("Saved"));
			Assert.That(source.Updated, Is.EqualTo(1));
			Assert.That(grid.Controller.IsEditing, Is.False);
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("aria-label=\"Edit item\""));
		});
	}

	/// <summary>Verifies custom reference editor keeps selected object identity and supports clear.</summary>
	[Test]
	public async Task CustomReferenceEditorKeepsSelectedObjectIdentityAndSupportsClear() {
		var original = new Row { Name = "Original reference" };
		var replacement = new Row { Name = "Selected reference" };
		var row = new Row { Name = "Owner", Reference = original };
		BlazorGridCellEditContext<Row> editor = null;
		var reference = BlazorGridColumn<Row>.For(item => item.Reference);
		reference.CanEditCell = true;
		reference.EditorTemplate = CaptureEditor(context => editor = context);
		await RenderAsync<BlazorGrid<Row>>(GridParameters(new ProbeSource<Row>(row), reference), async (grid, _, _) => {
			grid.BeginEdit(row);
			await editor.ValueChanged.InvokeAsync(replacement);
			Assert.That(row.Reference, Is.SameAs(original));
			Assert.That(grid.Controller.GetEditValue(reference), Is.SameAs(replacement));
			Assert.That(await grid.SaveAsync(), Is.True);
			Assert.That(row.Reference, Is.SameAs(replacement));
			grid.BeginEdit(row);
			await editor.ValueChanged.InvokeAsync(null);
			Assert.That(await grid.SaveAsync(), Is.True);
			Assert.That(row.Reference, Is.Null);
		});
	}

	/// <summary>Verifies validation errors render while the draft remains editable and uncommitted.</summary>
	[Test]
	public async Task ValidationErrorsRenderWhileTheDraftRemainsEditableAndUncommitted() {
		var row = new Row { Name = "Original" };
		var source = new ProbeSource<Row>(row) {
			Validation = (item, _) => string.IsNullOrWhiteSpace(item.Name) ? Result.Error("A name is required.") : Result.Success
		};
		BlazorGridCellEditContext<Row> editor = null;
		var name = BlazorGridColumn<Row>.For(item => item.Name);
		name.EditorTemplate = CaptureEditor(context => editor = context);
		await RenderAsync<BlazorGrid<Row>>(GridParameters(source, name), async (grid, _, rendered) => {
			grid.BeginEdit(row);
			await editor.ValueChanged.InvokeAsync(string.Empty);
			Assert.That(await grid.SaveAsync(), Is.False);
			Assert.That(source.Updated, Is.Zero);
			Assert.That(row.Name, Is.EqualTo("Original"));
			Assert.That(grid.Controller.IsEditing, Is.True);
			Assert.That(rendered.ToHtmlString(), Does.Contain("role=\"alert\""));
			Assert.That(rendered.ToHtmlString(), Does.Contain("A name is required."));
			await editor.ValueChanged.InvokeAsync("Corrected");
			Assert.That(await grid.SaveAsync(), Is.True);
			Assert.That(row.Name, Is.EqualTo("Corrected"));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("A name is required."));
		});
	}

	/// <summary>Verifies creating adraft does not persist until save and cancel discards it.</summary>
	[Test]
	public async Task CreatingADraftDoesNotPersistUntilSaveAndCancelDiscardsIt() {
		var source = new ProbeSource<Row> { NewItem = () => new Row { Name = "New draft" } };
		Row notification = null;
		var parameters = GridParameters(source, BlazorGridColumn<Row>.For(item => item.Name));
		parameters["ItemCreated"] = EventCallback.Factory.Create<Row>(new object(), item => notification = item);
		await RenderAsync<BlazorGrid<Row>>(parameters, async (grid, _, rendered) => {
			grid.BeginCreate();
			Assert.That(rendered.ToHtmlString(), Does.Contain("New item"));
			Assert.That(rendered.ToHtmlString(), Does.Contain("value=\"New draft\""));
			Assert.That(source.Created, Is.Zero);
			Assert.That(source.Count, Is.Zero);
			grid.CancelEdit();
			Assert.That(source.Count, Is.Zero);
			Assert.That(notification, Is.Null);
			grid.BeginCreate();
			var draft = grid.Controller.EditingItem;
			Assert.That(await grid.SaveAsync(), Is.True);
			Assert.That(source.Created, Is.EqualTo(1));
			Assert.That(source.Count, Is.EqualTo(1));
			Assert.That(notification, Is.SameAs(draft));
		});
	}

	/// <summary>Verifies rows without properties can render legacy actions.</summary>
	[Test]
	public async Task RowsWithoutPropertiesCanRenderLegacyActions() {
		var source = new ProbeSource<EmptyRow>(new EmptyRow());
		var parameters = new Dictionary<string, object> {
			["DataSource"] = source,
			["AllowedCapabilities"] = DataSourceCapabilities.CanRead,
			["Actions"] = new GridAction<EmptyRow>[] { new("Inspect", item => item, null) }
		};
		await RenderAsync<BlazorGrid<EmptyRow>>(parameters, (_, _, rendered) => {
			Assert.That(rendered.ToHtmlString(), Does.Contain("Actions</th>"));
			Assert.That(rendered.ToHtmlString(), Does.Contain(">Inspect</button>"));
			Assert.That(rendered.ToHtmlString(), Does.Contain("1 items"));
			return Task.CompletedTask;
		});
	}

	/// <summary>Verifies unrelated parameter renders preserve buffered values and editing row.</summary>
	[Test]
	public async Task UnrelatedParameterRendersPreserveBufferedValuesAndEditingRow() {
		var row = new Row { Name = "Original" };
		var source = new ProbeSource<Row>(row);
		var name = BlazorGridColumn<Row>.For(item => item.Name);
		var parameters = GridParameters(source, name);
		await RenderAsync<BlazorGrid<Row>>(parameters, async (grid, host, rendered) => {
			grid.BeginEdit(row);
			grid.Controller.SetEditValue(name, "Retained draft");
			parameters["Caption"] = "Updated caption";
			parameters["Columns"] = new[] { name };
			await host.UpdateAsync(parameters);
			await rendered.QuiescenceTask;
			Assert.That(grid.Controller.IsEditing, Is.True);
			Assert.That(grid.Controller.EditingItem, Is.SameAs(row));
			Assert.That(grid.Controller.GetEditValue(name), Is.EqualTo("Retained draft"));
			Assert.That(row.Name, Is.EqualTo("Original"));
			Assert.That(rendered.ToHtmlString(), Does.Contain("value=\"Retained draft\""));
			Assert.That(rendered.ToHtmlString(), Does.Contain("Updated caption"));
		});
	}

	/// <summary>Verifies source replacement or permission revocation clears selection and draft.</summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task SourceReplacementOrPermissionRevocationClearsSelectionAndDraft(bool replaceSource) {
		var row = new Row { Name = "Original" };
		var source = new ProbeSource<Row>(row);
		var name = BlazorGridColumn<Row>.For(item => item.Name);
		var parameters = GridParameters(source, name);
		await RenderAsync<BlazorGrid<Row>>(parameters, async (grid, host, rendered) => {
			grid.BeginEdit(row);
			grid.Controller.SetEditValue(name, "Discarded draft");
			if (replaceSource)
				parameters["DataSource"] = new ProbeSource<Row>(new Row { Name = "Replacement" });
			else
				parameters["AllowedCapabilities"] = DataSourceCapabilities.CanRead;
			await host.UpdateAsync(parameters);
			await rendered.QuiescenceTask;
			Assert.That(grid.Controller.IsEditing, Is.False);
			Assert.That(grid.Controller.HasSelection, Is.False);
			Assert.That(source.Updated, Is.Zero);
			Assert.That(row.Name, Is.EqualTo("Original"));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("Discarded draft"));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("aria-label=\"Edit item\""));
			if (replaceSource)
				Assert.That(rendered.ToHtmlString(), Does.Contain("Replacement"));
		});
	}

	/// <summary>Verifies reference picker masks writes and cancel preserves the original reference.</summary>
	[Test]
	public async Task ReferencePickerMasksWritesAndCancelPreservesTheOriginalReference() {
		var original = new Row { Name = "Existing reference", Enabled = true };
		var changes = new List<Row>();
		var parameters = new Dictionary<string, object> {
			["DataSource"] = new ProbeSource<Row>(original, new Row { Name = "Eligible reference" }),
			["Value"] = original,
			["DisplayText"] = (Func<Row, string>)(item => item.Name),
			["ValueChanged"] = EventCallback.Factory.Create<Row>(new object(), item => changes.Add(item)),
			["Label"] = "manager"
		};
		await RenderAsync<BlazorGridReferencePicker<Row>>(parameters, async (picker, host, rendered) => {
			picker.Open();
			await host.UpdateAsync(parameters);
			await rendered.QuiescenceTask;
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain("Eligible reference"));
			Assert.That(html, Does.Contain("type=\"search\""));
			Assert.That(html, Does.Not.Contain(">New</button>"));
			Assert.That(html, Does.Not.Contain(">Edit</button>"));
			Assert.That(html, Does.Not.Contain(">Delete</button>"));
			Assert.That(html, Does.Not.Contain("type=\"checkbox\""));
			await picker.ChooseAsync();
			Assert.That(changes, Is.Empty, "Choosing requires an explicit selection.");
			picker.Cancel();
			await host.UpdateAsync(parameters);
			Assert.That(changes, Is.Empty);
			Assert.That(picker.Value, Is.SameAs(original));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("sphere10-reference-panel"));
			await picker.ClearAsync();
			Assert.That(changes, Is.EqualTo(new Row[] { null }));
			parameters["AllowNull"] = false;
			await host.UpdateAsync(parameters);
			await picker.ClearAsync();
			Assert.That(changes.Count, Is.EqualTo(1));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("Clear manager"));
		});
	}

	/// <summary>Verifies concurrent javascript loads share one initialization and callback.</summary>
	[Test]
	public async Task ConcurrentJavascriptLoadsShareOneInitializationAndCallback() {
		var runtime = new GridJsRuntime();
		var initialized = new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
		runtime.Module.Initialize = () => initialized.Task;
		await RenderAsync<BlazorGrid<Row>>(GridParameters(new ProbeSource<Row>()), async (grid, _, _) => {
			using var completeInitialization = Tools.Scope.ExecuteOnDispose(() => initialized.TrySetResult(runtime.Module.Behavior));
			var first = grid.LoadJavascript();
			var second = grid.LoadJavascript();
			Assert.That(runtime.Imports, Is.EqualTo(1));
			Assert.That(runtime.Module.Callbacks.Count, Is.EqualTo(1));
			Assert.That(first.IsCompleted, Is.False);
			Assert.That(second.IsCompleted, Is.False);
			initialized.SetResult(runtime.Module.Behavior);
			await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
			await grid.LoadJavascript();
			Assert.That(runtime.Imports, Is.EqualTo(1));
			Assert.That(runtime.Module.Callbacks.Count, Is.EqualTo(1));
			Assert.That(runtime.Module.Callbacks[0].Value, Is.SameAs(grid));
			Assert.That(runtime.Module.Behavior.Updates, Is.EqualTo(3));
			Assert.That(runtime.ImportPath, Does.EndWith("/UI/Controls/BlazorGrid/BlazorGrid.razor.js"));
		}, runtime);
	}

	/// <summary>Verifies disconnected initialization releases its callback and can retry.</summary>
	[Test]
	public async Task DisconnectedInitializationReleasesItsCallbackAndCanRetry() {
		var runtime = new GridJsRuntime();
		var attempts = 0;
		runtime.Module.Initialize = () => ++attempts == 1
			? Task.FromException<IJSObjectReference>(new JSDisconnectedException("Circuit temporarily disconnected."))
			: Task.FromResult<IJSObjectReference>(runtime.Module.Behavior);
		await RenderAsync<BlazorGrid<Row>>(GridParameters(new ProbeSource<Row>()), async (grid, _, _) => {
			await Assert.ThatAsync(async () => await grid.LoadJavascript(), Throws.TypeOf<JSDisconnectedException>());
			var failedCallback = runtime.Module.Callbacks.Single();
			Assert.That(() => failedCallback.Value, Throws.TypeOf<ObjectDisposedException>());
			Assert.That(runtime.Module.Behavior.Updates, Is.Zero);
			await grid.LoadJavascript();
			Assert.That(attempts, Is.EqualTo(2));
			Assert.That(runtime.Imports, Is.EqualTo(1));
			Assert.That(runtime.Module.Callbacks.Count, Is.EqualTo(2));
			Assert.That(runtime.Module.Callbacks[1], Is.Not.SameAs(failedCallback));
			Assert.That(runtime.Module.Callbacks[1].Value, Is.SameAs(grid));
			Assert.That(runtime.Module.Behavior.Updates, Is.EqualTo(1));
		}, runtime);
	}

	/// <summary>Verifies disposal waits for initialization then releases interop without updating the removed grid.</summary>
	[Test]
	public async Task DisposalWaitsForInitializationThenReleasesInteropWithoutUpdatingTheRemovedGrid() {
		var runtime = new GridJsRuntime();
		var initialized = new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
		runtime.Module.Initialize = () => initialized.Task;
		await RenderAsync<BlazorGrid<Row>>(GridParameters(new ProbeSource<Row>()), async (grid, _, _) => {
			using var completeInitialization = Tools.Scope.ExecuteOnDispose(() => initialized.TrySetResult(runtime.Module.Behavior));
			var load = grid.LoadJavascript();
			var callback = runtime.Module.Callbacks.Single();
			var disposal = grid.DisposeAsync().AsTask();
			Assert.That(disposal.IsCompleted, Is.False);
			Assert.That(runtime.Module.Disposed, Is.False);
			initialized.SetResult(runtime.Module.Behavior);
			await Task.WhenAll(load, disposal).WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(() => callback.Value, Throws.TypeOf<ObjectDisposedException>());
			Assert.That(runtime.Module.Disposed, Is.True);
			Assert.That(runtime.Module.Behavior.Disposed, Is.True);
			Assert.That(runtime.Module.Behavior.Updates, Is.Zero);
			await grid.LoadJavascript();
			Assert.That(runtime.Module.Callbacks.Count, Is.EqualTo(1));
			Assert.That(runtime.Module.Behavior.Updates, Is.Zero);
		}, runtime);
	}

	/// <summary>Verifies stretch columns reserve configured widths before sharing space and keep editors aligned.</summary>
	[TestCase(false, 340)]
	[TestCase(true, 460)]
	public async Task StretchColumnsReserveConfiguredWidthsBeforeSharingSpaceAndKeepEditorsAligned(bool showActions, int minimumWidth) {
		var row = new Row { Name = "Editable name" };
		var name = BlazorGridColumn<Row>.For(item => item.Name);
		name.Width = 200;
		name.ExpandsToFit = true;
		var enabled = BlazorGridColumn<Row>.For(item => item.Enabled);
		enabled.Width = 80;
		var reference = BlazorGridColumn<Row>.For(item => item.Reference);
		reference.Width = 60;
		reference.ExpandsToFit = true;
		var parameters = GridParameters(new ProbeSource<Row>(row), name, enabled, reference);
		if (showActions)
			parameters["Actions"] = new GridAction<Row>[] { new("Inspect", item => item, string.Empty) };
		await RenderAsync<BlazorGrid<Row>>(parameters, (grid, _, rendered) => {
			var html = WebUtility.HtmlDecode(rendered.ToHtmlString());
			Assert.That(html, Does.Contain($"min-width: {minimumWidth}px"));
			Assert.That(html, Does.Contain("data-grid-width=\"200\" data-grid-expands=\"true\""));
			Assert.That(html, Does.Contain("data-grid-width=\"60\" data-grid-expands=\"true\""));
			Assert.That(html, Does.Contain("width: 80px"));
			grid.Controller.BeginEdit(row);
			html = WebUtility.HtmlDecode(rendered.ToHtmlString());
			Assert.That(html, Does.Contain("<colgroup").And.Contain($"min-width: {minimumWidth}px"));
			Assert.That(html, Does.Contain("value=\"Editable name\""));
			return Task.CompletedTask;
		});
	}

	/// <summary>Verifies changing column widths recomputes the scrollable minimum.</summary>
	[Test]
	public async Task ChangingColumnWidthsRecomputesTheScrollableMinimum() {
		var name = BlazorGridColumn<Row>.For(item => item.Name);
		name.Width = 0;
		name.ExpandsToFit = true;
		var enabled = BlazorGridColumn<Row>.For(item => item.Enabled);
		enabled.Width = 80;
		var parameters = GridParameters(new ProbeSource<Row>(), name, enabled);
		await RenderAsync<BlazorGrid<Row>>(parameters, async (grid, _, rendered) => {
			Assert.That(WebUtility.HtmlDecode(rendered.ToHtmlString()), Does.Contain("min-width: 120px"));
			name.Width = 240;
			await grid.SetParametersAsync(ParameterView.FromDictionary(parameters));
			Assert.That(WebUtility.HtmlDecode(rendered.ToHtmlString()), Does.Contain("min-width: 320px"));
			Assert.That(WebUtility.HtmlDecode(rendered.ToHtmlString()), Does.Contain("data-grid-width=\"240\" data-grid-expands=\"true\""));
		});
	}

	/// <summary>Verifies browser shutdown cancellation still releases every owned interop object.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task BrowserShutdownCancellationStillReleasesEveryOwnedInteropObject(bool cancelBehavior) {
		var runtime = new GridJsRuntime();
		runtime.Module.CancelDisposal = !cancelBehavior;
		runtime.Module.Behavior.CancelDisposal = cancelBehavior;
		await RenderAsync<BlazorGrid<Row>>(GridParameters(new ProbeSource<Row>()), async (grid, _, _) => {
			await grid.LoadJavascript();
			var callback = runtime.Module.Callbacks.Single();
			await grid.DisposeAsync();
			Assert.That(runtime.Module.Disposed, Is.True);
			Assert.That(runtime.Module.Behavior.Disposed, Is.True);
			Assert.That(() => callback.Value, Throws.TypeOf<ObjectDisposedException>());
		}, runtime);
	}

	/// <summary>Verifies caller column array changes do not invalidate the rendered draft.</summary>
	[Test]
	public async Task CallerColumnArrayChangesDoNotInvalidateTheRenderedDraft() {
		var row = new Row { Name = "Original" };
		BlazorGridCellEditContext<Row> editor = null;
		var name = BlazorGridColumn<Row>.For(item => item.Name);
		name.EditorTemplate = CaptureEditor(context => editor = context);
		var columns = new[] { name };
		await RenderAsync<BlazorGrid<Row>>(GridParameters(new ProbeSource<Row>(row), columns), async (grid, _, rendered) => {
			grid.BeginEdit(row);
			columns[0] = null;
			await editor.ValueChanged.InvokeAsync("Saved");
			Assert.That(rendered.ToHtmlString(), Does.Contain("Saved"));
			Assert.That(grid.Controller.Columns[0], Is.SameAs(name));
			Assert.That(await grid.SaveAsync(), Is.True);
			Assert.That(row.Name, Is.EqualTo("Saved"));
		});
	}

	// Creates the ordinary column and source parameters for a rendered grid.
	private static Dictionary<string, object> GridParameters(ProbeSource<Row> source, params BlazorGridColumn<Row>[] columns) {
		var parameters = new Dictionary<string, object> { ["DataSource"] = source };
		if (columns.Length > 0)
			parameters["Columns"] = columns;
		return parameters;
	}

	// Captures the actual editor context without replacing its behavior.
	private static RenderFragment<BlazorGridCellEditContext<Row>> CaptureEditor(Action<BlazorGridCellEditContext<Row>> capture) {
		return context => builder => {
			capture(context);
			builder.OpenElement(0, "output");
			builder.AddContent(1, context.Value?.ToString());
			builder.CloseElement();
		};
	}

	// Renders the component inside an isolated user-settings scope.
	private static async Task RenderAsync<TComponent>(
		Dictionary<string, object> parameters,
		Func<TComponent, ComponentHost, HtmlRootComponent, Task> assertions,
		IJSRuntime runtime = null
	)
		where TComponent : IComponent {
		await using var provider = new ServiceCollection().AddLogging().AddSingleton(runtime ?? new GalleryRenderingTests.TestJsRuntime()).BuildServiceProvider();
		await using var settings = new BlazorSettingsProvider(provider, "grid-rendering-fixture");
		using var settingsScope = UserSettings.UseProvider(settings);
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ComponentHost host = null;
			var rendered = await renderer.RenderComponentAsync<ComponentHost>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ComponentHost.ComponentType)] = typeof(TComponent),
				[nameof(ComponentHost.Parameters)] = parameters,
				[nameof(ComponentHost.Capture)] = (Action<ComponentHost>)(value => host = value)
			}));
			await assertions((TComponent)host.Component, host, rendered);
		});
	}

	#region Inner Types

	/// <summary>An editable row used by the rendering scenarios.</summary>
	public sealed class Row {

		/// <summary>The editable row name.</summary>
		public string Name { get; set; }

		/// <summary>The editable checkbox value.</summary>
		public bool Enabled { get; set; }

		/// <summary>The row's selected reference.</summary>
		public Row Reference { get; set; }

		/// <summary>The row's immutable display value.</summary>
		public string ReadOnly {
			get {
				return "Fixed display";
			}
		}
	}

	/// <summary>A row with no declared properties for legacy-column coverage.</summary>
	public sealed class EmptyRow {
	}

	// Emulates browser module imports for the real grid and settings provider.
	private sealed class GridJsRuntime : IJSRuntime {

		/// <summary>The fixture's shared browser module.</summary>
		public GridJsModule Module { get; } = new();

		/// <summary>The number of grid module imports.</summary>
		public int Imports { get; private set; }

		/// <summary>The last grid module path.</summary>
		public string ImportPath { get; private set; }

		/// <summary>Implements the fixture's browser or renderer invocation boundary.</summary>
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			Assert.That(identifier, Is.EqualTo("import"));
			var path = (string)args.Single();
			if (path.EndsWith("/js/ApplicationSettings.js", StringComparison.Ordinal))
				return ValueTask.FromResult((TValue)(object)Module);
			Imports++;
			ImportPath = path;
			return ValueTask.FromResult((TValue)(object)Module);
		}

		/// <summary>Implements the fixture's browser or renderer invocation boundary.</summary>
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) {
			return InvokeAsync<TValue>(identifier, args);
		}
	}

	// Emulates browser storage and grid behavior creation.
	private sealed class GridJsModule : IJSObjectReference {

		/// <summary>Creates the grid js module with its fixture dependencies.</summary>
		public GridJsModule() {
			Initialize = () => Task.FromResult<IJSObjectReference>(Behavior);
		}

		// Browser records remain ordinary strings; the real settings provider owns their typed serialization.
		public Dictionary<string, string> TypedRecords { get; } = new();

		/// <summary>The typed record keys written by the real settings provider.</summary>
		public List<string> TypedWriteKeys { get; } = new();

		/// <summary>The callback objects passed to grid initialization.</summary>
		public List<DotNetObjectReference<BlazorGrid<Row>>> Callbacks { get; } = new();

		/// <summary>The current simulated grid behavior.</summary>
		public GridJsBehavior Behavior { get; } = new();

		/// <summary>Optional control over delayed behavior creation.</summary>
		public Func<Task<IJSObjectReference>> Initialize { get; set; }

		/// <summary>Simulates browser cancellation during cleanup.</summary>
		public bool CancelDisposal { get; set; }

		/// <summary>Records whether the fixture resource was released.</summary>
		public bool Disposed { get; private set; }

		/// <summary>Implements the fixture's browser or renderer invocation boundary.</summary>
		public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			if (identifier == "GetAnonymousProfile")
				return (TValue)(object)"grid-browser";
			if (identifier == "Read")
				return TypedRecords.TryGetValue((string)args[0], out var value) ? (TValue)(object)value : default;
			if (identifier == "Write") {
				var key = (string)args[0];
				TypedRecords[key] = (string)args[1];
				TypedWriteKeys.Add(key);
				return default;
			}
			if (identifier == "ReadLegacySettings")
				return await Behavior.InvokeAsync<TValue>("ReadSettings", args);
			Assert.That(identifier, Is.EqualTo("Initialize"));
			Assert.That(args[0], Is.TypeOf<ElementReference>());
			Callbacks.Add((DotNetObjectReference<BlazorGrid<Row>>)args[1]);
			return (TValue)await Initialize();
		}

		/// <summary>Implements the fixture's browser or renderer invocation boundary.</summary>
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) {
			return InvokeAsync<TValue>(identifier, args);
		}

		/// <summary>Releases the fixture-owned asynchronous resources.</summary>
		public ValueTask DisposeAsync() {
			Disposed = true;
			return CancelDisposal ? ValueTask.FromCanceled(new CancellationToken(true)) : ValueTask.CompletedTask;
		}
	}

	// Records grid behavior calls without writing standalone browser records.
	private sealed class GridJsBehavior : IJSObjectReference {

		/// <summary>Existing untyped browser values available for legacy reads.</summary>
		public Dictionary<string, BlazorGridSettings> StoredSettings { get; } = new();

		/// <summary>The legacy keys read by the grid.</summary>
		public List<string> SettingsReads { get; } = new();

		/// <summary>The view-state updates sent to the browser.</summary>
		public int Updates { get; private set; }

		/// <summary>Simulates browser cancellation during cleanup.</summary>
		public bool CancelDisposal { get; set; }

		/// <summary>Records whether the fixture resource was released.</summary>
		public bool Disposed { get; private set; }

		/// <summary>Implements the fixture's browser or renderer invocation boundary.</summary>
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			if (identifier == "ApplyWidths")
				return ValueTask.FromResult(default(TValue));
			if (identifier == "Dispose")
				return ValueTask.FromResult(default(TValue));
			if (identifier == "ReadSettings") {
				SettingsReads.Add((string)args[0]);
				return ValueTask.FromResult(StoredSettings.TryGetValue((string)args[0], out var settings) ? (TValue)(object)settings : default);
			}

			Assert.That(identifier, Is.EqualTo("Update"));
			Assert.That(args.Single(), Is.TypeOf<bool>());
			Assert.That(Disposed, Is.False, "A disposed grid behavior must not receive updates.");
			Updates++;
			return ValueTask.FromResult(default(TValue));
		}

		/// <summary>Implements the fixture's browser or renderer invocation boundary.</summary>
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) {
			return InvokeAsync<TValue>(identifier, args);
		}

		/// <summary>Releases the fixture-owned asynchronous resources.</summary>
		public ValueTask DisposeAsync() {
			Disposed = true;
			return CancelDisposal ? ValueTask.FromCanceled(new CancellationToken(true)) : ValueTask.CompletedTask;
		}
	}

	// Records data-source operations and preserves the declared capabilities.
	private sealed class ProbeSource<T> : ListDataSource<T> {

		/// <summary>Creates the probe source with its fixture dependencies.</summary>
		public ProbeSource(params T[] items)
			: base(new ExtendedList<T>()) {
			Future.Value.AddRange(items);
		}

		/// <summary>The entities accepted by create operations.</summary>
		public int Created { get; private set; }

		/// <summary>The entities accepted by update operations.</summary>
		public int Updated { get; private set; }

		/// <summary>The next draft returned by the source.</summary>
		public Func<T> NewItem { get; set; }

		/// <summary>The validation result supplied by the source.</summary>
		public Func<T, CrudAction, Result> Validation { get; set; }

		/// <summary>Optional delay before the source completes a query.</summary>
		public Task ReadGate { get; set; }

		/// <summary>The actual arguments passed to the source.</summary>
		public List<SourceRequest> Requests { get; } = new();

		/// <summary>The source operations enabled for this scenario.</summary>
		public DataSourceCapabilities AvailableCapabilities { get; set; } = DataSourceCapabilities.Default;

		/// <summary>The source's current synchronous capability mask.</summary>
		public override DataSourceCapabilities Capabilities {
			get {
				return AvailableCapabilities;
			}
		}

		/// <summary>The source's current asynchronous capability mask.</summary>
		public override Task<DataSourceCapabilities> CapabilitiesAsync {
			get {
				return Task.FromResult(AvailableCapabilities);
			}
		}

		/// <summary>Records a query before reading the requested rows.</summary>
		public override async Task<DataSourceItems<T>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			Requests.Add(new SourceRequest(pageLength, page, sortProperty, sortDirection));
			if (ReadGate != null)
				await ReadGate;
			return ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection);
		}

		/// <summary>Records entities accepted by the source.</summary>
		public override void CreateRange(IEnumerable<T> entities) {
			var items = entities.ToArray();
			Created += items.Length;
			base.CreateRange(items);
		}

		/// <summary>Records entities accepted by the source.</summary>
		public override void UpdateRange(IEnumerable<T> entities) {
			var items = entities.ToArray();
			Updated += items.Length;
			base.UpdateRange(items);
		}

		/// <summary>Returns the scenario's declared validation result.</summary>
		public override Result ValidateRange(IEnumerable<(T entity, CrudAction action)> actions) {
			return Result.Combine(actions.Select(action => Validation?.Invoke(action.entity, action.action) ?? Result.Success));
		}

		/// <summary>Returns the scenario's next new-row draft.</summary>
		protected override T NewMethod() {
			return NewItem == null ? base.NewMethod() : NewItem();
		}
	}

	#endregion
}
