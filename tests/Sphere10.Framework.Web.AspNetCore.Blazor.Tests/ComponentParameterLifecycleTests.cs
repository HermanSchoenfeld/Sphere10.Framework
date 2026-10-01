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
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Modal;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;
using Sphere10.Framework.Web.AspNetCore.Blazor.Models;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ComponentParameterLifecycleTests {
	[TestCase(typeof(PageSizeSelector), 5)]
	[TestCase(typeof(PageSizeSelector), 7)]
	[TestCase(typeof(PageSizeSelector), 10)]
	[TestCase(typeof(UI.Grids.PageSizeSelector), 5)]
	[TestCase(typeof(UI.Grids.PageSizeSelector), 7)]
	[TestCase(typeof(UI.Grids.PageSizeSelector), 10)]
	public async Task PageSizeSelectorIncludesItsCurrentValueAndDoesNotDuplicatePresetOptions(Type selectorType, int pageSize) {
		await using var provider = CreateServices();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var parameters = new Dictionary<string, object> {
				["Value"] = pageSize,
				["ValueExpression"] = (Expression<Func<int>>)(() => pageSize)
			};
			if (selectorType == typeof(PageSizeSelector))
				parameters["Model"] = new object();
			var rendered = await renderer.RenderComponentAsync<ComponentHost>(HostParameters(selectorType, parameters, _ => { }));
			var html = rendered.ToHtmlString();
			var options = Regex.Matches(html, "<option[^>]*value=\"(?<value>[0-9]+)\"")
				.Select(match => int.Parse(match.Groups["value"].Value)).ToArray();
			Assert.That(options, Does.Contain(pageSize));
			Assert.That(options, Does.Contain(5));
			Assert.That(options, Is.Unique);
			Assert.That(Regex.IsMatch(html, $"<option(?=[^>]*value=\"{pageSize}\")(?=[^>]*selected)[^>]*>"), Is.True,
				"The browser must have a selected option for the active page size.");
		});
	}

	[TestCase(typeof(PagedTable<int>))]
	[TestCase(typeof(VirtualPagedTable<int>))]
	[TestCase(typeof(UI.Grids.PagedTable<int>))]
	[TestCase(typeof(UI.Grids.VirtualPagedTable<int>))]
	public async Task TablePagerExposesCurrentPageWithoutVisibleScreenReaderMarker(Type tableType) {
		await using var provider = CreateServices();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var parameters = TableParameters();
			if (tableType == typeof(VirtualPagedTable<int>))
				parameters["ItemsProvider"] = (VirtualPagedTable<int>.ItemsProviderDelegate)(_ => Task.FromResult(new ItemsResponse<int>(Array.Empty<int>(), 0)));
			else if (tableType == typeof(UI.Grids.VirtualPagedTable<int>))
				parameters["ItemsProvider"] = (UI.Grids.VirtualPagedTable<int>.ItemsProviderDelegate)(_ => Task.FromResult(new UI.Grids.ItemsResponse<int>(Array.Empty<int>(), 0)));
			else
				parameters["Items"] = Array.Empty<int>();
			var rendered = await renderer.RenderComponentAsync<ComponentHost>(HostParameters(tableType, parameters, _ => { }));
			var html = rendered.ToHtmlString();
			var currentPage = Regex.Match(html, "<span[^>]*aria-current=\"page\"[^>]*>(?<text>.*?)</span>", RegexOptions.Singleline);
			Assert.That(currentPage.Success, Is.True);
			Assert.That(currentPage.Groups["text"].Value.Trim(), Is.EqualTo("1"));
			Assert.That(html, Does.Contain("aria-label=\"Table pages\""));
			if (tableType.Namespace == typeof(UI.Grids.PagedTable<int>).Namespace)
				Assert.That(Regex.Replace(html, "\\s+", " "), Does.Contain("Showing 0 to 0 of 0 entries"));
		});
	}
	[TestCase(typeof(PagedTable<int>))]
	[TestCase(typeof(UI.Grids.PagedTable<int>))]
	public async Task PageSizeAssignedBeforeItemsStartsAtFirstPageAndShrinkingItemsClampsPage(Type tableType) {
		await using var provider = CreateServices();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var parameters = TableParameters();
			parameters["Items"] = Enumerable.Range(0, 30).ToArray();
			ComponentHost host = null;
			var rendered = await renderer.RenderComponentAsync<ComponentHost>(HostParameters(tableType, parameters, value => host = value));
			Assert.That(rendered.ToHtmlString(), Does.Contain("<td>0</td>"));
			Assert.That(rendered.ToHtmlString(), Does.Contain("<td>4</td>"));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("<td>5</td>"));
			if (host.Component is PagedTable<int> original)
				original.ViewModel.CurrentPage = 5;
			else
				((UI.Grids.PagedTable<int>)host.Component).CurrentPage = 5;
			parameters["Items"] = new[] { 51, 52, 53 };
			await host.UpdateAsync(parameters);
			Assert.That(rendered.ToHtmlString(), Does.Contain("<td>51</td>"));
			Assert.That(rendered.ToHtmlString(), Does.Contain("<td>53</td>"));
		});
	}

	[TestCase(typeof(PagedTable<int>))]
	[TestCase(typeof(UI.Grids.PagedTable<int>))]
	public async Task UserPageSizeSurvivesUnrelatedParentRender(Type tableType) {
		await using var provider = CreateServices();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var parameters = TableParameters();
			parameters["Items"] = Enumerable.Range(0, 30).ToArray();
			ComponentHost host = null;
			var rendered = await renderer.RenderComponentAsync<ComponentHost>(HostParameters(tableType, parameters, value => host = value));
			if (host.Component is PagedTable<int> original)
				original.ViewModel.PageSize = 10;
			else
				await ((UI.Grids.PagedTable<int>)host.Component).SetPageSizeAsync(10);
			parameters["Class"] = "updated";
			await host.UpdateAsync(parameters);
			Assert.That(rendered.ToHtmlString(), Does.Contain("<td>9</td>"));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("<td>10</td>"));
		});
	}

	[TestCase(typeof(VirtualPagedTable<int>))]
	[TestCase(typeof(UI.Grids.VirtualPagedTable<int>))]
	public async Task ReplacingVirtualProviderReloadsWithoutReloadingOnUnrelatedParentRender(Type tableType) {
		await using var provider = CreateServices();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var requests = 0;
			var parameters = TableParameters();
			if (tableType == typeof(VirtualPagedTable<int>))
				parameters["ItemsProvider"] = (VirtualPagedTable<int>.ItemsProviderDelegate)(request => {
					requests++;
					return Task.FromResult(new ItemsResponse<int>(new[] { 1 }, 1));
				});
			else
				parameters["ItemsProvider"] = (UI.Grids.VirtualPagedTable<int>.ItemsProviderDelegate)(request => {
					requests++;
					return Task.FromResult(new UI.Grids.ItemsResponse<int>(new[] { 1 }, 1));
				});
			ComponentHost host = null;
			var rendered = await renderer.RenderComponentAsync<ComponentHost>(HostParameters(tableType, parameters, value => host = value));
			Assert.That(requests, Is.EqualTo(1));
			parameters["Class"] = "updated";
			await host.UpdateAsync(parameters);
			Assert.That(requests, Is.EqualTo(1));
			if (tableType == typeof(VirtualPagedTable<int>))
				parameters["ItemsProvider"] = (VirtualPagedTable<int>.ItemsProviderDelegate)(request => Task.FromResult(new ItemsResponse<int>(new[] { 99 }, 1)));
			else
				parameters["ItemsProvider"] = (UI.Grids.VirtualPagedTable<int>.ItemsProviderDelegate)(request => Task.FromResult(new UI.Grids.ItemsResponse<int>(new[] { 99 }, 1)));
			await host.UpdateAsync(parameters);
			Assert.That(rendered.ToHtmlString(), Does.Contain("<td>99</td>"));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("<td>1</td>"));
		});
	}

	[Test]
	public async Task DelayedVirtualResponseCannotOverwriteNewerProviderResults() {
		var delayed = new TaskCompletionSource<ItemsResponse<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var model = new VirtualPagedTableViewModel<int> { ItemsProvider = _ => delayed.Task };
		var previous = model.RefreshAsync();
		model.ItemsProvider = _ => Task.FromResult(new ItemsResponse<int>(new[] { 99 }, 1));
		await model.RefreshAsync();
		delayed.SetResult(new ItemsResponse<int>(new[] { 1, 2, 3 }, 3));
		await previous;
		Assert.That(model.Page, Is.EqualTo(new[] { 99 }));
		Assert.That(model.TotalItems, Is.EqualTo(1));
	}

	[Test]
	public async Task StreamReplacementCancelsOldEnumeratorAndClearsOldRows() {
		var oldStopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var newStopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var source = WaitingStream(1, oldStopped);
		await using var model = new RapidTableViewModel<int> { Source = source };
		await model.InitAsync();
		await model.SetSourceAsync(source);
		Assert.That(oldStopped.Task.IsCompleted, Is.False, "An unchanged source must remain subscribed.");
		await model.SetSourceAsync(WaitingStream(99, newStopped));
		Assert.That(oldStopped.Task.IsCompletedSuccessfully, Is.True);
		Assert.That(model.Items, Is.EqualTo(new[] { 99 }));
		await model.DisposeAsync();
		Assert.That(newStopped.Task.IsCompletedSuccessfully, Is.True);
	}

	[Test]
	public async Task DisposingModalHostReturnsCancelAndDisposesItsModule() {
		var runtime = new ModalJsRuntime();
		await using var provider = CreateServices(runtime);
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ComponentHost host = null;
			await renderer.RenderComponentAsync<ComponentHost>(HostParameters(typeof(ModalProbe), new Dictionary<string, object>(), value => host = value));
			var modal = (ModalProbe)host.Component;
			var interaction = modal.ShowAsync();
			await Task.WhenAny(interaction, runtime.Module.Shown.Task).WaitAsync(TimeSpan.FromSeconds(5));
			if (interaction.IsCompleted)
				await interaction;
			await modal.DisposeAsync();
			Assert.That(await interaction, Is.EqualTo("cancelled"));
			Assert.That(interaction.IsCompletedSuccessfully, Is.True);
			Assert.That(runtime.Module.Disposed, Is.True);
			Assert.That(runtime.ImportPath, Is.EqualTo("./_content/Sphere10.Framework.Web.AspNetCore.Blazor/js/modal.js"));
		});
	}


	[TestCase("render")]
	[TestCase("import")]
	[TestCase("show")]
	public async Task HostDisposalDuringModalStartupReturnsCancellationWithoutAnException(string stage) {
		var runtime = new ModalJsRuntime { DelayImport = stage == "import" };
		runtime.Module.DelayShow = stage == "show";
		await using var provider = CreateServices(runtime);
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ComponentHost host = null;
			await renderer.RenderComponentAsync<ComponentHost>(HostParameters(typeof(ModalProbe), new Dictionary<string, object>(), value => host = value));
			var modal = (ModalProbe)host.Component;
			if (stage == "render")
				modal.RenderCompletion = new TaskCompletionSource().Task;
			var interaction = modal.ShowAsync();
			if (stage == "import")
				await runtime.ImportStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			else if (stage == "show")
				await runtime.Module.Shown.Task.WaitAsync(TimeSpan.FromSeconds(5));
			await modal.DisposeAsync();
			Assert.That(await interaction.WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo("cancelled"));
			Assert.That(interaction.IsCompletedSuccessfully, Is.True);
			Assert.That(runtime.Module.Disposed, Is.EqualTo(stage == "show"));
		});
	}

	[Test]
	public async Task UnrelatedModalCancellationStillPropagatesAndClearsTheDialog() {
		var runtime = new ModalJsRuntime();
		await using var provider = CreateServices(runtime);
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ComponentHost host = null;
			await renderer.RenderComponentAsync<ComponentHost>(HostParameters(typeof(ModalProbe), new Dictionary<string, object>(), value => host = value));
			var modal = (ModalProbe)host.Component;
			var interaction = modal.ShowAsync();
			await runtime.Module.Shown.Task.WaitAsync(TimeSpan.FromSeconds(5));
			modal.Result.TrySetCanceled();
			await Assert.ThatAsync(async () => await interaction, Throws.InstanceOf<OperationCanceledException>());
			Assert.That(runtime.Module.Calls, Is.EqualTo(new[] { "show", "hide" }));
		});
	}

	[Test]
	public async Task ModalHostReturnsResultAndHidesOnlyItsElement() {
		var runtime = new ModalJsRuntime();
		await using var provider = CreateServices(runtime);
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ComponentHost host = null;
			await renderer.RenderComponentAsync<ComponentHost>(HostParameters(typeof(ModalProbe), new Dictionary<string, object>(), value => host = value));
			var modal = (ModalProbe)host.Component;
			var interaction = modal.ShowAsync();
			await Task.WhenAny(interaction, runtime.Module.Shown.Task).WaitAsync(TimeSpan.FromSeconds(5));
			if (interaction.IsCompleted)
				await interaction;
			modal.Result.SetResult("accepted");
			Assert.That(await interaction, Is.EqualTo("accepted"));
			Assert.That(runtime.Module.Calls, Is.EqualTo(new[] { "show", "hide" }));
		});
	}

	[Test]
	public async Task DefaultModalCloseCallbackCompletesPendingDialog() {
		var runtime = new ModalJsRuntime();
		await using var provider = CreateServices(runtime);
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ComponentHost host = null;
			await renderer.RenderComponentAsync<ComponentHost>(HostParameters(typeof(ModalProbe), new Dictionary<string, object>(), value => host = value));
			var modal = (ModalProbe)host.Component;
			var interaction = modal.ShowAsync();
			await Task.WhenAny(interaction, runtime.Module.Shown.Task).WaitAsync(TimeSpan.FromSeconds(5));
			if (interaction.IsCompleted)
				await interaction;
			Assert.That(await modal.CurrentContent.CloseModal(), Is.True);
			Assert.That(await interaction, Is.EqualTo("closed"));
			Assert.That(runtime.Module.Calls, Is.EqualTo(new[] { "show", "hide" }));
		});
	}

	[Test]
	public async Task WizardReplacementUpdatesStepModelAndFinalStepValidatesBeforeFinishing() {
		await using var provider = CreateServices();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var finishes = 0;
			var firstWizard = new DefaultWizard<string>("First", new List<Type> { typeof(WizardProbeStep) }, "first model", null, null);
			var secondWizard = new DefaultWizard<string>("Second", new List<Type> { typeof(WizardProbeStep) }, "second model", _ => {
				finishes++;
				return Task.FromResult(new Result<bool>(true));
			}, null);
			var parameters = new Dictionary<string, object> { [nameof(WizardContainer.Wizard)] = firstWizard };
			ComponentHost host = null;
			var rendered = await renderer.RenderComponentAsync<ComponentHost>(HostParameters(typeof(WizardContainer), parameters, value => host = value));
			var container = (WizardContainer)host.Component;
			var originalStep = container.Host.ViewModel.CurrentStepInstance;
			Assert.That(rendered.ToHtmlString(), Does.Contain("first model"));
			parameters[nameof(WizardContainer.Wizard)] = secondWizard;
			await host.UpdateAsync(parameters);
			Assert.That(rendered.ToHtmlString(), Does.Contain("second model"));
			Assert.That(container.Host.ViewModel.CurrentStepInstance, Is.Not.SameAs(originalStep));
			await container.Host.ViewModel.FinishAsync();
			Assert.That(finishes, Is.Zero);
			Assert.That(container.Host.ViewModel.ErrorMessages, Does.Contain("Complete the final step."));
			((WizardProbeStep)container.Host.ViewModel.CurrentStepInstance).ViewModel.IsValid = true;
			await container.Host.ViewModel.FinishAsync();
			await container.Host.ViewModel.FinishAsync();
			Assert.That(finishes, Is.EqualTo(1));
		});
	}

	private static ServiceProvider CreateServices(IJSRuntime runtime = null) {
		var services = new ServiceCollection().AddLogging().AddSphere10Blazor();
		services.AddTransient<WizardProbeViewModel>();
		if (runtime != null)
			services.AddSingleton(runtime);
		return services.BuildServiceProvider();
	}

	private static Dictionary<string, object> TableParameters() => new() {
		["PageSize"] = 5,
		["HeaderTemplate"] = (RenderFragment)(builder => builder.AddMarkupContent(0, "<tr><th>Value</th></tr>")),
		["ItemTemplate"] = (RenderFragment<int>)(item => builder => {
			builder.OpenElement(0, "tr");
			builder.OpenElement(1, "td");
			builder.AddContent(2, item);
			builder.CloseElement();
			builder.CloseElement();
		})
	};

	private static ParameterView HostParameters(Type componentType, Dictionary<string, object> parameters, Action<ComponentHost> capture) => ParameterView.FromDictionary(new Dictionary<string, object> {
		[nameof(ComponentHost.ComponentType)] = componentType,
		[nameof(ComponentHost.Parameters)] = parameters,
		[nameof(ComponentHost.Capture)] = capture
	});

	private static async IAsyncEnumerable<int> WaitingStream(int value, TaskCompletionSource<bool> stopped, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
		using var scope = Tools.Scope.ExecuteOnDispose(() => stopped.TrySetResult(true));
		yield return value;
		await Task.Delay(Timeout.Infinite, cancellationToken);
	}

	public class ComponentHost : ComponentBase {
		private Dictionary<string, object> _parameters;

		[Parameter] public Type ComponentType { get; set; }

		[Parameter] public Dictionary<string, object> Parameters { get; set; }

		[Parameter] public Action<ComponentHost> Capture { get; set; }

		public object Component { get; private set; }

		public Task UpdateAsync(Dictionary<string, object> parameters) {
			_parameters = parameters;
			return InvokeAsync(StateHasChanged);
		}

		protected override void OnInitialized() {
			_parameters = Parameters;
			Capture(this);
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent(0, ComponentType);
			builder.AddMultipleAttributes(1, _parameters);
			builder.AddComponentReferenceCapture(2, component => Component = component);
			builder.CloseComponent();
		}
	}

	public class WizardContainer : ComponentBase {
		[Parameter] public IWizard Wizard { get; set; }

		public WizardHost Host { get; private set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<CascadingValue<IWizard>>(0);
			builder.AddAttribute(1, nameof(CascadingValue<IWizard>.Value), Wizard);
			builder.AddAttribute(2, nameof(CascadingValue<IWizard>.ChildContent), (RenderFragment)(content => {
				content.OpenComponent<WizardHost>(0);
				content.AddComponentReferenceCapture(1, component => Host = (WizardHost)component);
				content.CloseComponent();
			}));
			builder.CloseComponent();
		}
	}

	public class WizardProbeStep : WizardStep<string, WizardProbeViewModel> {
		public override string Title => "Final step";

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, ViewModel.Model);
	}

	public class WizardProbeViewModel : WizardStepViewModelBase<string> {
		public bool IsValid { get; set; }

		public override Task<Result> OnNextAsync() => Task.FromResult(IsValid ? Result.Success : Result.Error("Complete the final step."));

		public override Task<Result> OnPreviousAsync() => Task.FromResult(Result.Success);
	}

	public class ModalProbeContent : ComponentBase {
		[CascadingParameter(Name = "CloseModal")]
		public Func<Task<bool>> CloseModal { get; set; }
	}

	public class ModalProbe : ModalHostBase<ComponentBase, string> {
		public TaskCompletionSource<string> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public Task RenderCompletion { get; set; } = Task.CompletedTask;

		protected override string CanceledResult => "cancelled";

		public Task<string> ShowAsync() => ShowCoreAsync<ModalProbeContent>(new Dictionary<string, object>());

		protected override Task WaitUntilRenderedAsync(ComponentBase component) => RenderCompletion;

		public ModalProbeContent CurrentContent { get; private set; }

		protected override Task<string> GetResultAsync(ComponentBase component) {
			CurrentContent = (ModalProbeContent)component;
			return Result.Task;
		}

		protected override Task<bool> RequestCloseAsync(ComponentBase component) {
			Result.TrySetResult("closed");
			return Task.FromResult(true);
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenElement(0, "div");
			builder.AddElementReferenceCapture(1, element => ModalElement = element);
			builder.AddContent(2, DisplayedContent);
			builder.CloseElement();
		}
	}

	private sealed class ModalJsRuntime : IJSRuntime {
		public ModalJsModule Module { get; } = new();

		public TaskCompletionSource ImportStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource<IJSObjectReference> PendingImport { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public bool DelayImport { get; set; }

		public string ImportPath { get; private set; }

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

		public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) {
			Assert.That(identifier, Is.EqualTo("import"));
			ImportPath = (string)args[0];
			ImportStarted.TrySetResult();
			var module = DelayImport ? await PendingImport.Task.WaitAsync(cancellationToken) : Module;
			return (TValue)(object)module;
		}
	}

	private sealed class ModalJsModule : IJSObjectReference {
		public TaskCompletionSource<bool> Shown { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource ShowCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public List<string> Calls { get; } = new();

		public bool DelayShow { get; set; }

		public bool Disposed { get; private set; }

		public ValueTask DisposeAsync() {
			Disposed = true;
			return ValueTask.CompletedTask;
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

		public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) {
			Assert.That(args.Single(), Is.TypeOf<ElementReference>());
			Calls.Add(identifier);
			if (identifier == "show") {
				Shown.TrySetResult(true);
				if (DelayShow)
					await ShowCompletion.Task.WaitAsync(cancellationToken);
			}
			return default;
		}
	}
}
