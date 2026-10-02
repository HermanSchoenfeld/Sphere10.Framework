// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
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
public class BlazorGridReferencePickerTests {
	[Test]
	public async Task CompactTriggerOpensAnAccessibleReadOnlyGridPopover() {
		var parameters = Parameters(out var original, out _);
		await RenderAsync(parameters, async (picker, host, rendered, runtime) => {
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain("sphere10-reference-trigger").And.Contain("aria-label=\"Choose manager\""));
			Assert.That(html, Does.Contain("aria-haspopup=\"dialog\"").And.Contain("aria-expanded=\"false\""));
			Assert.That(html, Does.Contain("Existing manager"));
			Assert.That(html, Does.Not.Contain(">Choose manager</button>").And.Not.Contain(">Clear manager</button>"));
			picker.Open();
			await host.UpdateAsync(parameters);
			await picker.SynchronizeBrowserAsync();
			await rendered.QuiescenceTask;
			html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain("popover=\"auto\"").And.Contain("role=\"dialog\"").And.Contain("aria-expanded=\"true\""));
			Assert.That(html, Does.Contain("Eligible manager").And.Contain("type=\"search\"").And.Contain(">Use selected</button>"));
			Assert.That(html, Does.Not.Contain(">New</button>").And.Not.Contain(">Edit</button>").And.Not.Contain(">Delete</button>"));
			Assert.That(picker.Value, Is.SameAs(original));
			Assert.That(runtime.Module.Shown, Is.EqualTo(1));
			Assert.That(runtime.ImportPath, Does.EndWith("/BlazorGridReferencePicker.razor.js"));
		});
	}

	[Test]
	public async Task BrowserDismissalPreservesValueAndIgnoresAnOlderOpening() {
		var parameters = Parameters(out var original, out var changes);
		await RenderAsync(parameters, async (picker, host, rendered, runtime) => {
			picker.Open();
			await host.UpdateAsync(parameters);
			await picker.SynchronizeBrowserAsync();
			var oldGeneration = runtime.Module.Generation;
			picker.Cancel();
			await host.UpdateAsync(parameters);
			await picker.SynchronizeBrowserAsync();
			Assert.That(runtime.Module.FocusReturns, Is.EqualTo(1));
			picker.Open();
			await host.UpdateAsync(parameters);
			await picker.SynchronizeBrowserAsync();
			var currentGeneration = runtime.Module.Generation;
			await picker.DismissAsync(oldGeneration);
			Assert.That(rendered.ToHtmlString(), Does.Contain("sphere10-reference-panel"));
			await picker.DismissAsync(currentGeneration);
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("sphere10-reference-panel"));
			Assert.That(picker.Value, Is.SameAs(original));
			Assert.That(changes, Is.Empty);
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task RebindingOrDisablingClosesThePanelWithoutChangingTheReference(bool disable) {
		var parameters = Parameters(out var original, out var changes);
		await RenderAsync(parameters, async (picker, host, rendered, _) => {
			picker.Open();
			await host.UpdateAsync(parameters);
			if (disable)
				parameters["Disabled"] = true;
			else
				parameters["DataSource"] = Source(new Person { Name = "Replacement source" });
			await host.UpdateAsync(parameters);
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("sphere10-reference-panel"));
			Assert.That(picker.Value, Is.SameAs(original));
			Assert.That(changes, Is.Empty);
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task DisposalReleasesTheBrowserCallbackAndImportedModule(bool cancelDisposal) {
		var parameters = Parameters(out _, out _);
		await RenderAsync(parameters, async (picker, host, _, runtime) => {
			picker.Open();
			await host.UpdateAsync(parameters);
			await picker.SynchronizeBrowserAsync();
			var callback = runtime.Module.Callback;
			runtime.Module.CancelDisposal = cancelDisposal;
			await picker.DisposeAsync();
			Assert.That(runtime.Module.Disposed, Is.True);
			Assert.That(() => callback.Value, Throws.TypeOf<ObjectDisposedException>());
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task DisposalWaitsForPendingImportWithoutShowingAnAbandonedPopup(bool cancelImport) {
		var parameters = Parameters(out _, out _);
		var runtime = new PickerJsRuntime();
		var imported = new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
		runtime.Import = imported.Task;
		await RenderAsync(parameters, async (picker, host, _, _) => {
			picker.Open();
			await host.UpdateAsync(parameters);
			using var releaseImport = Tools.Scope.ExecuteOnDispose(() => imported.TrySetResult(runtime.Module));
			var loading = picker.SynchronizeBrowserAsync();
			var disposal = picker.DisposeAsync().AsTask();
			Assert.That(disposal.IsCompleted, Is.False);
			if (cancelImport)
				imported.SetCanceled();
			else
				imported.SetResult(runtime.Module);
			await Task.WhenAll(loading, disposal).WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(runtime.Module.Disposed, Is.EqualTo(!cancelImport));
			Assert.That(runtime.Module.Shown, Is.Zero);
			Assert.That(runtime.Module.Callback, Is.Null);
		}, runtime);
	}

	private static Dictionary<string, object> Parameters(out Person original, out List<Person> changes) {
		original = new Person { Name = "Existing manager" };
		var received = new List<Person>();
		changes = received;
		return new Dictionary<string, object> {
			["DataSource"] = Source(original, new Person { Name = "Eligible manager" }),
			["Value"] = original,
			["Label"] = "manager",
			["DisplayText"] = (Func<Person, string>)(person => person.Name),
			["ValueChanged"] = EventCallback.Factory.Create<Person>(new object(), person => received.Add(person))
		};
	}

	private static IDataSource<Person> Source(params Person[] people) => new PickerSource(people);

	private static async Task RenderAsync(
		Dictionary<string, object> parameters,
		Func<ProbePicker, ComponentHost, HtmlRootComponent, PickerJsRuntime, Task> assertions,
		PickerJsRuntime runtime = null
	) {
		runtime ??= new PickerJsRuntime();
		await using var provider = new ServiceCollection().AddLogging().AddSingleton<IJSRuntime>(runtime).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ComponentHost host = null;
			var rendered = await renderer.RenderComponentAsync<ComponentHost>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ComponentHost.ComponentType)] = typeof(ProbePicker),
				[nameof(ComponentHost.Parameters)] = parameters,
				[nameof(ComponentHost.Capture)] = (Action<ComponentHost>)(instance => host = instance)
			}));
			await assertions((ProbePicker)host.Component, host, rendered, runtime);
		});
	}

	public sealed class Person {
		public string Name { get; set; }
	}

	public sealed class ProbePicker : BlazorGridReferencePicker<Person> {
		public Task SynchronizeBrowserAsync() => base.OnAfterRenderAsync(false);
	}

	private sealed class PickerSource : ListDataSource<Person> {
		public PickerSource(Person[] people)
			: base(new ExtendedList<Person>(people)) {
		}

		public override Task<DataSourceCapabilities> CapabilitiesAsync => Task.FromResult(DataSourceCapabilities.Default);

		public override Task<DataSourceItems<Person>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) =>
			Task.FromResult(ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection));
	}
	private sealed class PickerJsRuntime : IJSRuntime {
		public PickerJsRuntime() {
			Import = Task.FromResult<IJSObjectReference>(Module);
		}

		public PickerJsModule Module { get; } = new();

		public Task<IJSObjectReference> Import { get; set; }

		public string ImportPath { get; private set; }

		public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			Assert.That(identifier, Is.EqualTo("import"));
			ImportPath = (string)args[0];
			return (TValue)(object)await Import;
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);
	}

	private sealed class PickerJsModule : IJSObjectReference {
		public int Shown { get; private set; }

		public int FocusReturns { get; private set; }

		public long Generation { get; private set; }

		public DotNetObjectReference<BlazorGridReferencePicker<Person>> Callback { get; private set; }

		public bool CancelDisposal { get; set; }

		public bool Disposed { get; private set; }

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			if (identifier == "Show") {
				Shown++;
				Generation = (long)args[3];
				Callback = (DotNetObjectReference<BlazorGridReferencePicker<Person>>)args[2];
			} else {
				Assert.That(identifier, Is.EqualTo("Focus"));
				FocusReturns++;
			}
			return ValueTask.FromResult(default(TValue));
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);

		public ValueTask DisposeAsync() {
			Disposed = true;
			return CancelDisposal ? ValueTask.FromCanceled(new CancellationToken(true)) : ValueTask.CompletedTask;
		}
	}
}
