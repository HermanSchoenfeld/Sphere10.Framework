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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.BlazorTester;
using Sphere10.Framework.Utils.BlazorTester.Application;
using Sphere10.Framework.Utils.BlazorTester.Layouts;
using Sphere10.Framework.Utils.BlazorTester.Modern.UI.Controls;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
public class GallerySettingsMigrationTests {
	[Test]
	public async Task DelayedLegacyImportPrecedesChildGridConstructionAndTheirFirstQueries() {
		var store = new EmptyStore();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var runtime = new LegacyRuntime { ReadGate = release.Task };
		var services = new ServiceCollection().AddLogging();
		Program.ConfigureServices(services);
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost/components/grid"));
		services.AddSingleton<IJSRuntime>(runtime);
		services.AddSingleton<IApplicationSettingsStore>(store);
		services.AddSingleton<IComponentActivator, QueryCaptureActivator>();
		await using var provider = services.BuildServiceProvider();
		await provider.GetRequiredService<IBlazorApplication>().InitializeAsync();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = renderer.BeginRenderingComponent<RouteView>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(RouteView.RouteData)] = new RouteData(typeof(Workspace), new Dictionary<string, object>()),
				[nameof(RouteView.DefaultLayout)] = typeof(DemoLayout)
			}));
			try {
				await runtime.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
				var capture = (QueryCaptureActivator)provider.GetRequiredService<IComponentActivator>();
				Assert.That(rendered.QuiescenceTask.IsCompleted, Is.False);
				Assert.That(capture.Grids, Is.Empty, "Pending migration must keep child grids unconstructed, so no source can query defaults.");
				Assert.That(rendered.ToHtmlString(), Does.Not.Contain("data-demo=\"grid\""));
				release.SetResult();
				await rendered.QuiescenceTask;

				Assert.That(capture.Queries.Single(query => query.Key == "tester.editable-grid"),
					Is.EqualTo(("tester.editable-grid", 7, 1, "Name", SortDirection.Descending)));
				Assert.That(capture.Queries.Single(query => query.Key == "tester.live-grid"),
					Is.EqualTo(("tester.live-grid", 4, 1, "Name", SortDirection.Descending)));
				Assert.That(store.Writes, Is.Zero);
			} finally {
				release.TrySetResult();
			}
		});
	}

	[Test]
	public async Task PagingOnlyLegacyRecordsKeepBothDeclaredColumnSchemasBeforeAnyAcceptedCapture() {
		var store = new EmptyStore();
		var runtime = new LegacyRuntime();
		var services = new ServiceCollection().AddLogging();
		Program.ConfigureServices(services);
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost/components/grid"));
		services.AddSingleton<IJSRuntime>(runtime);
		services.AddSingleton<IApplicationSettingsStore>(store);
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IBlazorApplication>();
		await application.InitializeAsync();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			await renderer.RenderComponentAsync<RouteView>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(RouteView.RouteData)] = new RouteData(typeof(Workspace), new Dictionary<string, object>()),
				[nameof(RouteView.DefaultLayout)] = typeof(DemoLayout)
			}));
		});

		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		var block = host.AvailableBlocks.Single(item => item.Id == "components");
		var definition = block.ScreenDefinitions.Single(item => item.ScreenType == typeof(ComponentScreen));
		var values = application.Settings.GetProfile(block, definition.Registration).GetValues<GallerySettings>();
		var defaults = new GallerySettings();
		Assert.That(values.Grid.Columns.Select(column => (column.Key, column.Title)), Is.EqualTo(defaults.Grid.Columns.Select(column => (column.Key, column.Title))));
		Assert.That(values.LiveGrid.Columns.Select(column => (column.Key, column.Title)), Is.EqualTo(defaults.LiveGrid.Columns.Select(column => (column.Key, column.Title))));
		Assert.That(values.Grid.Columns.All(column => !column.Width.HasValue), Is.True);
		Assert.That(values.LiveGrid.Columns.All(column => !column.Width.HasValue), Is.True);
		Assert.That(values.Grid.PageSize, Is.EqualTo(7));
		Assert.That(values.Grid.PageNumber, Is.EqualTo(1));
		Assert.That(values.Grid.SortColumnName, Is.EqualTo("Name"));
		Assert.That(values.Grid.SortDirection, Is.EqualTo(SortDirection.Descending));
		Assert.That(values.LiveGrid.PageSize, Is.EqualTo(4));
		Assert.That(values.LiveGrid.PageMode, Is.EqualTo(BlazorGridPageMode.Auto));
		Assert.That(runtime.Reads, Is.EquivalentTo(new[] { "tester.editable-grid", "tester.live-grid" }));
		Assert.That(store.Writes, Is.Zero, "Importing old values preserves schema without eagerly saving or accepting a control edit.");
	}

	// Loads defaults through the actual application catalog without touching any user or browser profile.
	private sealed class EmptyStore : IApplicationSettingsStore {
		public int Writes { get; private set; }

		public Task<ApplicationSettingsStoredProfile> LoadAsync(string key, object defaults, bool hasExplicitStorageId, object storageId, CancellationToken token = default) {
			return Task.FromResult<ApplicationSettingsStoredProfile>(null);
		}

		public Task SaveAsync(string key, ApplicationSettingsStoredProfile values, bool hasExplicitStorageId, object storageId, CancellationToken token = default) {
			Writes++;
			return Task.CompletedTask;
		}
	}

	// Models old paging-only browser records, whose original schema contained no manual-width entries.
	private sealed class LegacyRuntime : IJSRuntime {
		public List<string> Reads { get; } = new();

		public Task ReadGate { get; init; } = Task.CompletedTask;

		public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			return ValueTask.FromResult(identifier == "import" ? (TValue)(object)new LegacyModule(this) : default);
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken token, object[] args) {
			return InvokeAsync<TValue>(identifier, args);
		}
	}

	// Delays actual legacy IO without delaying application/profile readiness or the renderer's interim render.
	private sealed class LegacyModule(LegacyRuntime runtime) : IJSObjectReference {
		public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			if (identifier != "ReadLegacySettings")
				return default;
			var key = (string)args[0];
			runtime.Reads.Add(key);
			runtime.ReadStarted.TrySetResult();
			await runtime.ReadGate;
			var settings = new BlazorGridSettings {
				PageMode = key == "tester.live-grid" ? BlazorGridPageMode.Auto : BlazorGridPageMode.Paged,
				PageSize = key == "tester.live-grid" ? 4 : 7,
				PageNumber = 1,
				SortColumnName = "Name",
				SortDirection = SortDirection.Descending
			};
			Assert.That(settings.ColumnWidths, Is.Empty);
			return (TValue)(object)settings;
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken token, object[] args) {
			return InvokeAsync<TValue>(identifier, args);
		}

		public ValueTask DisposeAsync() {
			return ValueTask.CompletedTask;
		}
	}

	// Observes real controller query starts without replacing the gallery's sources or operational components.
	private sealed class QueryCaptureActivator(IServiceProvider provider) : IComponentActivator {
		public List<IComponent> Grids { get; } = new();

		public List<(string Key, int PageSize, int Page, string Sort, SortDirection Direction)> Queries { get; } = new();

		public IComponent CreateInstance(Type componentType) {
			var component = (IComponent)ActivatorUtilities.CreateInstance(provider, componentType);
			if (component is BlazorGrid<TestClass> editable)
				Observe(editable);
			if (component is BlazorGrid<GridDemoSnapshotDataSource.Row> live)
				Observe(live);
			return component;
		}

		// The first loading notification carries the restored query before the source read begins.
		private void Observe<T>(BlazorGrid<T> grid) {
			Grids.Add(grid);
			var captured = false;
			grid.Controller.Changed += () => {
				if (captured || !grid.Controller.IsLoading)
					return;
				captured = true;
				Queries.Add((grid.SettingsKey, grid.Controller.ManualPageSize, grid.Controller.CurrentPage, grid.Controller.SortProperty, grid.Controller.SortDirection));
			};
		}
	}
}
