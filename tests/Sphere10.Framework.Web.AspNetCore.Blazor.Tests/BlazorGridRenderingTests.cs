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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;
using ComponentHost = Sphere10.Framework.Web.AspNetCore.Blazor.Tests.ComponentParameterLifecycleTests.ComponentHost;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorGridRenderingTests {
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

	private static Dictionary<string, object> GridParameters(ProbeSource<Row> source, params BlazorGridColumn<Row>[] columns) {
		var parameters = new Dictionary<string, object> { ["DataSource"] = source };
		if (columns.Length > 0)
			parameters["Columns"] = columns;
		return parameters;
	}

	private static RenderFragment<BlazorGridCellEditContext<Row>> CaptureEditor(Action<BlazorGridCellEditContext<Row>> capture) => context => builder => {
		capture(context);
		builder.OpenElement(0, "output");
		builder.AddContent(1, context.Value?.ToString());
		builder.CloseElement();
	};

	private static async Task RenderAsync<TComponent>(
		Dictionary<string, object> parameters,
		Func<TComponent, ComponentHost, HtmlRootComponent, Task> assertions,
		IJSRuntime runtime = null
	) where TComponent : IComponent {
		await using var provider = new ServiceCollection().AddLogging().AddSingleton(runtime ?? new GalleryRenderingTests.TestJsRuntime()).BuildServiceProvider();
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

	public sealed class Row {
		public string Name { get; set; }

		public bool Enabled { get; set; }

		public Row Reference { get; set; }

		public string ReadOnly => "Fixed display";
	}

	public sealed class EmptyRow { }

	private sealed class GridJsRuntime : IJSRuntime {
		public GridJsModule Module { get; } = new();

		public int Imports { get; private set; }

		public string ImportPath { get; private set; }

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			Assert.That(identifier, Is.EqualTo("import"));
			Imports++;
			ImportPath = (string)args.Single();
			return ValueTask.FromResult((TValue)(object)Module);
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);
	}

	private sealed class GridJsModule : IJSObjectReference {
		public GridJsModule() {
			Initialize = () => Task.FromResult<IJSObjectReference>(Behavior);
		}

		public List<DotNetObjectReference<BlazorGrid<Row>>> Callbacks { get; } = new();

		public GridJsBehavior Behavior { get; } = new();

		public Func<Task<IJSObjectReference>> Initialize { get; set; }

		public bool CancelDisposal { get; set; }

		public bool Disposed { get; private set; }

		public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			Assert.That(identifier, Is.EqualTo("Initialize"));
			Assert.That(args[0], Is.TypeOf<ElementReference>());
			Callbacks.Add((DotNetObjectReference<BlazorGrid<Row>>)args[1]);
			return (TValue)await Initialize();
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);

		public ValueTask DisposeAsync() {
			Disposed = true;
			return CancelDisposal ? ValueTask.FromCanceled(new CancellationToken(true)) : ValueTask.CompletedTask;
		}
	}

	private sealed class GridJsBehavior : IJSObjectReference {
		public int Updates { get; private set; }

		public bool CancelDisposal { get; set; }

		public bool Disposed { get; private set; }

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			Assert.That(identifier, Is.EqualTo("Update"));
			Assert.That(args.Single(), Is.TypeOf<bool>());
			Assert.That(Disposed, Is.False, "A disposed grid behavior must not receive updates.");
			Updates++;
			return ValueTask.FromResult(default(TValue));
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);

		public ValueTask DisposeAsync() {
			Disposed = true;
			return CancelDisposal ? ValueTask.FromCanceled(new CancellationToken(true)) : ValueTask.CompletedTask;
		}
	}
	private sealed class ProbeSource<T> : ListDataSource<T> {
		public ProbeSource(params T[] items)
			: base(new ExtendedList<T>()) {
			Future.Value.AddRange(items);
		}

		public int Created { get; private set; }

		public int Updated { get; private set; }

		public Func<T> NewItem { get; set; }

		public Func<T, CrudAction, Result> Validation { get; set; }

		public DataSourceCapabilities AvailableCapabilities { get; set; } = DataSourceCapabilities.Default;

		public override DataSourceCapabilities Capabilities => AvailableCapabilities;

		public override Task<DataSourceCapabilities> CapabilitiesAsync => Task.FromResult(AvailableCapabilities);

		public override Task<DataSourceItems<T>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) =>
			Task.FromResult(ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection));

		public override void CreateRange(IEnumerable<T> entities) {
			var items = entities.ToArray();
			Created += items.Length;
			base.CreateRange(items);
		}

		public override void UpdateRange(IEnumerable<T> entities) {
			var items = entities.ToArray();
			Updated += items.Length;
			base.UpdateRange(items);
		}

		public override Result ValidateRange(IEnumerable<(T entity, CrudAction action)> actions) =>
			Result.Combine(actions.Select(action => Validation?.Invoke(action.entity, action.action) ?? Result.Success));

		protected override T NewMethod() => NewItem == null ? base.NewMethod() : NewItem();
	}
}