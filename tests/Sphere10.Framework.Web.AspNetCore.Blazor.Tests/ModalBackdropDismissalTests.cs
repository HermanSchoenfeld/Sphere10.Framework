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
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Modal;
using Sphere10.Framework.Web.AspNetCore.Blazor.Wizard;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Dialogs;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Wizard;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>
/// Verifies what a click outside a current-generation modal, or the Escape key, does for each dialog kind and policy.
/// </summary>
/// <remarks>
/// The static renderer runs no script, so each case calls <see cref="ModalTemplate.RequestDismissAsync"/>, the member
/// the modal script reaches for those gestures. <c>WizardBrowserTests</c> covers the gestures themselves.
/// </remarks>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ModalBackdropDismissalTests {
	private const string Question = "Are you sure you want to close?";

	[TestCase("confirm", null, ModalBackdropDismissal.Close)]
	[TestCase("information", null, ModalBackdropDismissal.Close)]
	[TestCase("wizard", null, ModalBackdropDismissal.Confirm)]
	[TestCase("confirm", ModalBackdropDismissal.Confirm, ModalBackdropDismissal.Confirm)]
	[TestCase("information", ModalBackdropDismissal.Ignore, ModalBackdropDismissal.Ignore)]
	[TestCase("wizard", ModalBackdropDismissal.Close, ModalBackdropDismissal.Close)]
	[TestCase("wizard", ModalBackdropDismissal.Ignore, ModalBackdropDismissal.Ignore)]
	public async Task PolicyComesFromTheCallerOtherwiseFromTheDialogKind(string kind, ModalBackdropDismissal? requested, ModalBackdropDismissal expected) {
		var parameters = new Dictionary<string, object> { [nameof(ModalComponent.Options)] = new ModalOptions { BackdropDismissal = requested } };
		var type = typeof(Confirm);
		if (kind == "information") {
			type = typeof(Dialog);
			parameters[nameof(Dialog.Body)] = (RenderFragment)(builder => builder.AddContent(0, "Detail"));
			parameters[nameof(Dialog.ButtonsText)] = new[] { "Close" };
		} else if (kind == "wizard") {
			type = typeof(BlazorWizardModal);
			parameters[nameof(BlazorWizardModal.Wizard)] = CreateWizard(new DraftModel());
		}
		await using var provider = CreateServices(new ModalRuntime());
		var components = provider.GetRequiredService<CapturingActivator>();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync(type, ParameterView.FromDictionary(parameters));
			Assert.That(components.All<ModalTemplate>().Single().BackdropDismissal, Is.EqualTo(expected));
			Assert.That(rendered.ToHtmlString().Contains("class=\"modal-dismiss\""), Is.EqualTo(expected != ModalBackdropDismissal.Close),
				"Only a dialog that asks or ignores takes the gesture away from its close button.");
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public Task ClickOutsideAWizardAsksAndYesCancelsItAsItsCloseButtonDoes(bool throughModalService) => RunAsync(async scene => {
		var cancellations = 0;
		var wizard = CreateWizard(new DraftModel(), cancel: _ => {
			cancellations++;
			return Task.FromResult<Result<bool>>(true);
		});
		var interaction = throughModalService
			? scene.Modals.ShowAsync<BlazorWizardModal>(ParameterView.FromDictionary(new Dictionary<string, object> { [nameof(BlazorWizardModal.Wizard)] = wizard }))
			: scene.Views.WizardDialogAsync(wizard);
		await scene.ShownAsync<BlazorWizardModal>(1);
		Assert.That(scene.Html, Does.Not.Contain("probe-modal-confirmation"));
		var dismissal = scene.Components.All<ModalTemplate>()[0].RequestDismissAsync();
		await scene.ShownAsync<Confirm>(2);

		// The question is the library's confirmation, shown by its own host above the wizard.
		var question = scene.Components.All<Confirm>().Single();
		Assert.That(question.Message, Is.EqualTo(Question));
		Assert.That(scene.Html, Does.Contain("id=\"probe-modal-confirmation\"").And.Contain(">Yes</button>").And.Contain(">No</button>"));
		Assert.That(scene.Html, Does.Contain("class=\"wizard-content\""), "The wizard stays rendered beneath its question.");
		Assert.That(interaction.IsCompleted, Is.False);
		Assert.That(cancellations, Is.Zero);
		await question.Ok();
		Assert.That(await dismissal.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That((await interaction.WaitAsync(TimeSpan.FromSeconds(5))).ResultType, Is.EqualTo(ModalResultType.Exit));
		Assert.That(cancellations, Is.EqualTo(1));
		Assert.That(wizard.State, Is.EqualTo(WizardState.Cancelled));
		Assert.That(scene.Runtime.Calls, Is.EqualTo(new[] { "Show", "Show", "Hide", "Hide" }));
	});

	[TestCase(false)]
	[TestCase(true)]
	public Task AnsweringNoReturnsToTheWizardWithItsEnteredData(bool closeTheQuestion) => RunAsync(async scene => {
		var cancellations = 0;
		var model = new DraftModel { Name = "Draft wallet" };
		var wizard = CreateWizard(model, cancel: _ => {
			cancellations++;
			return Task.FromResult<Result<bool>>(true);
		});
		var interaction = scene.Views.WizardDialogAsync(wizard);
		await scene.ShownAsync<BlazorWizardModal>(1);
		await scene.Components.All<BlazorWizardHost>().Single().NextAsync();
		var step = scene.Components.All<ReviewStep>().Single();
		var template = scene.Components.All<ModalTemplate>()[0];
		var dismissal = template.RequestDismissAsync();
		await scene.ShownAsync<Confirm>(2);
		Assert.That(await template.RequestDismissAsync(), Is.False, "A second gesture must not ask a second question.");
		var question = scene.Components.All<Confirm>().Single();
		if (closeTheQuestion)
			Assert.That(await question.RequestCloseAsync(), Is.True);
		else
			question.Cancel();
		Assert.That(await dismissal.WaitAsync(TimeSpan.FromSeconds(5)), Is.False);

		// The same step instance and model remain, and only the question was hidden.
		Assert.That(interaction.IsCompleted, Is.False);
		Assert.That(cancellations, Is.Zero);
		Assert.That(wizard.State, Is.EqualTo(WizardState.Active));
		Assert.That(wizard.CurrentStepIndex, Is.EqualTo(1));
		Assert.That(scene.Components.All<ReviewStep>().Single(), Is.SameAs(step));
		Assert.That(scene.Html, Does.Contain("Draft wallet").And.Not.Contain(Question));
		Assert.That(scene.Runtime.Calls, Is.EqualTo(new[] { "Show", "Show", "Hide" }));

		// The wizard can be asked about again, and Yes then closes it.
		dismissal = template.RequestDismissAsync();
		await scene.ShownAsync<Confirm>(3);
		await scene.Components.All<Confirm>().Last().Ok();
		Assert.That(await dismissal.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That((await interaction.WaitAsync(TimeSpan.FromSeconds(5))).ResultType, Is.EqualTo(ModalResultType.Exit));
		Assert.That(cancellations, Is.EqualTo(1));
		Assert.That(model.Name, Is.EqualTo("Draft wallet"));
	});

	[TestCase(false, true)]
	[TestCase(true, false)]
	public Task AWizardThatRefusesCancellationIsNeitherAskedNorClosed(bool wizardCancellable, bool stepCancellable) => RunAsync(async scene => {
		var cancellations = 0;
		var wizard = CreateWizard(new DraftModel(), wizardCancellable, stepCancellable, _ => {
			cancellations++;
			return Task.FromResult<Result<bool>>(true);
		});
		var interaction = scene.Views.WizardDialogAsync(wizard);
		await scene.ShownAsync<BlazorWizardModal>(1);
		var template = scene.Components.All<ModalTemplate>().Single();
		Assert.That(template.BackdropDismissal, Is.EqualTo(ModalBackdropDismissal.Confirm));
		Assert.That(scene.Html, Does.Not.Contain("modal-dismiss").And.Not.Contain("aria-label=\"Close\""));
		Assert.That(await template.RequestDismissAsync(), Is.False);
		Assert.That(await scene.Components.All<BlazorWizardModal>().Single().RequestCloseAsync(), Is.False);
		Assert.That(scene.Components.All<Confirm>(), Is.Empty, "No question may be shown for a wizard that cannot be cancelled.");
		Assert.That(scene.Html, Does.Not.Contain("probe-modal-confirmation"));
		Assert.That(interaction.IsCompleted, Is.False);
		Assert.That(cancellations, Is.Zero);
		Assert.That(wizard.State, Is.EqualTo(WizardState.Active));
		Assert.That(scene.Runtime.Calls, Is.EqualTo(new[] { "Show" }));
	});

	[Test]
	public Task ARejectedCancellationKeepsTheWizardOpenAfterYes() => RunAsync(async scene => {
		var cancellations = 0;
		var wizard = CreateWizard(new DraftModel(), cancel: _ => {
			cancellations++;
			var rejection = new Result<bool>(false);
			rejection.AddError("Save the draft first.");
			return Task.FromResult(rejection);
		});
		var interaction = scene.Views.WizardDialogAsync(wizard);
		await scene.ShownAsync<BlazorWizardModal>(1);
		var dismissal = scene.Components.All<ModalTemplate>()[0].RequestDismissAsync();
		await scene.ShownAsync<Confirm>(2);
		await scene.Components.All<Confirm>().Single().Ok();
		Assert.That(await dismissal.WaitAsync(TimeSpan.FromSeconds(5)), Is.False);
		Assert.That(cancellations, Is.EqualTo(1), "Yes runs the wizard's own cancellation callback.");
		Assert.That(interaction.IsCompleted, Is.False);
		Assert.That(wizard.State, Is.EqualTo(WizardState.Active));
		Assert.That(scene.Html, Does.Contain("Save the draft first.").And.Not.Contain(Question));
	});

	[Test]
	public Task TheCloseButtonCancelsAWizardWithoutAsking() => RunAsync(async scene => {
		var cancellations = 0;
		var wizard = CreateWizard(new DraftModel(), cancel: _ => {
			cancellations++;
			return Task.FromResult<Result<bool>>(true);
		});
		var interaction = scene.Views.WizardDialogAsync(wizard);
		await scene.ShownAsync<BlazorWizardModal>(1);
		Assert.That(scene.Html, Does.Contain("aria-label=\"Close\""));

		// The close button calls the wizard modal's close request directly.
		Assert.That(await scene.Components.All<BlazorWizardModal>().Single().RequestCloseAsync(), Is.True);
		Assert.That((await interaction.WaitAsync(TimeSpan.FromSeconds(5))).ResultType, Is.EqualTo(ModalResultType.Exit));
		Assert.That(cancellations, Is.EqualTo(1));
		Assert.That(scene.Components.All<Confirm>(), Is.Empty);
		Assert.That(scene.Runtime.Calls, Is.EqualTo(new[] { "Show", "Hide" }));
	});

	[TestCase(ModalBackdropDismissal.Close)]
	[TestCase(ModalBackdropDismissal.Ignore)]
	public Task TheCallerCanMakeAWizardCloseOrStayWithoutAsking(ModalBackdropDismissal policy) => RunAsync(async scene => {
		var wizard = CreateWizard(new DraftModel());
		var interaction = scene.Views.WizardDialogAsync(wizard, new ModalOptions { Size = ModalSize.Large, BackdropDismissal = policy });
		await scene.ShownAsync<BlazorWizardModal>(1);
		var closes = policy == ModalBackdropDismissal.Close;
		Assert.That(await scene.Components.All<ModalTemplate>().Single().RequestDismissAsync(), Is.EqualTo(closes));
		Assert.That(scene.Components.All<Confirm>(), Is.Empty);
		Assert.That(interaction.IsCompleted, Is.EqualTo(closes));
		Assert.That(wizard.State, Is.EqualTo(closes ? WizardState.Cancelled : WizardState.Active));
		if (closes)
			Assert.That((await interaction).ResultType, Is.EqualTo(ModalResultType.Exit));
	});

	[TestCase(false)]
	[TestCase(true)]
	public Task OtherDialogsStillCloseWithoutAsking(bool information) => RunAsync(async scene => {
		var interaction = information ? scene.Views.DialogAsync("Notice", "Readable detail", CancellationToken.None) : scene.Views.ConfirmDialogAsync("Delete", "Delete this record?", "Delete");
		if (information)
			await scene.ShownAsync<Dialog>(1);
		else
			await scene.ShownAsync<Confirm>(1);
		var template = scene.Components.All<ModalTemplate>().Single();
		Assert.That(template.BackdropDismissal, Is.EqualTo(ModalBackdropDismissal.Close));
		Assert.That(scene.Html, Does.Not.Contain("modal-dismiss"));
		Assert.That(await template.RequestDismissAsync(), Is.True);
		await interaction.WaitAsync(TimeSpan.FromSeconds(5));
		if (!information)
			Assert.That(await (Task<bool>)interaction, Is.False, "Dismissing a confirmation answers it negatively, as before.");
		Assert.That(scene.Html, Does.Not.Contain("probe-modal-confirmation"));
		Assert.That(scene.Runtime.Calls, Is.EqualTo(new[] { "Show", "Hide" }));
	});

	[TestCase(ModalBackdropDismissal.Confirm)]
	[TestCase(ModalBackdropDismissal.Ignore)]
	public Task ADialogFollowsThePolicyItsCallerSets(ModalBackdropDismissal policy) => RunAsync(async scene => {
		var parameters = new Dictionary<string, object> { [nameof(Confirm.Message)] = "Delete this record?" };
		var interaction = scene.Modals.ShowAsync<Confirm>(ParameterView.FromDictionary(parameters), new ModalOptions { BackdropDismissal = policy });
		await scene.ShownAsync<Confirm>(1);
		var template = scene.Components.All<ModalTemplate>()[0];
		Assert.That(scene.Html, Does.Contain("class=\"modal-dismiss\""));
		if (policy == ModalBackdropDismissal.Ignore) {
			Assert.That(await template.RequestDismissAsync(), Is.False);
			Assert.That(scene.Components.All<Confirm>(), Has.Length.EqualTo(1));
			Assert.That(interaction.IsCompleted, Is.False);
			return;
		}

		// No keeps the caller waiting, Yes ends the dialog as its close button would.
		var dismissal = template.RequestDismissAsync();
		await scene.ShownAsync<Confirm>(2);
		scene.Components.All<Confirm>().Last().Cancel();
		Assert.That(await dismissal.WaitAsync(TimeSpan.FromSeconds(5)), Is.False);
		Assert.That(interaction.IsCompleted, Is.False);
		dismissal = template.RequestDismissAsync();
		await scene.ShownAsync<Confirm>(3);
		await scene.Components.All<Confirm>().Last().Ok();
		Assert.That(await dismissal.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That((await interaction.WaitAsync(TimeSpan.FromSeconds(5))).ResultType, Is.EqualTo(ModalResultType.Exit));
	});

	[Test]
	public Task RemovingTheDialogWithdrawsItsQuestion() => RunAsync(async scene => {
		using var cancellation = new CancellationTokenSource();
		var parameters = new Dictionary<string, object> { [nameof(BlazorWizardModal.Wizard)] = CreateWizard(new DraftModel()) };
		var interaction = scene.Modals.ShowAsync<BlazorWizardModal>(ParameterView.FromDictionary(parameters), null, cancellation.Token);
		await scene.ShownAsync<BlazorWizardModal>(1);
		var dismissal = scene.Components.All<ModalTemplate>()[0].RequestDismissAsync();
		await scene.ShownAsync<Confirm>(2);
		cancellation.Cancel();
		Assert.That((await interaction.WaitAsync(TimeSpan.FromSeconds(5))).ResultType, Is.EqualTo(ModalResultType.Cancel));
		Assert.That(await dismissal.WaitAsync(TimeSpan.FromSeconds(5)), Is.False);
		Assert.That(scene.Html, Does.Not.Contain(Question));
		Assert.That(scene.Runtime.Calls.Count(call => call == "Hide"), Is.EqualTo(2));
	});

	// Renders a real modal host with the real services and runs the case on the renderer's dispatcher.
	private static async Task RunAsync(Func<Scene, Task> test) {
		var runtime = new ModalRuntime();
		await using var provider = CreateServices(runtime);
		await using var scope = provider.CreateAsyncScope();
		await using var renderer = new HtmlRenderer(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<HostHarness>();
			await test(new Scene(scope.ServiceProvider, runtime, rendered));
		});
	}

	private static ServiceProvider CreateServices(ModalRuntime runtime) {
		var services = new ServiceCollection().AddLogging().AddSphere10Blazor();
		services.AddSingleton<IJSRuntime>(runtime);
		services.AddSingleton<CapturingActivator>();
		services.AddSingleton<IComponentActivator>(provider => provider.GetRequiredService<CapturingActivator>());
		return services.BuildServiceProvider();
	}

	private static IBlazorWizard<DraftModel> CreateWizard(DraftModel model, bool cancellable = true, bool stepCancellable = true, Func<DraftModel, Task<Result<bool>>> cancel = null) {
		var builder = new BlazorWizardBuilder<DraftModel>()
			.NewWizard("Draft")
			.WithModel(model)
			.WithCancellation(cancellable)
			.AddStep(stepCancellable ? typeof(NameStep) : typeof(LockedStep))
			.AddStep<ReviewStep>();
		return cancel == null ? builder.Build() : builder.OnCancelled(cancel).Build();
	}

	#region Inner Types

	public class DraftModel {
		public string Name { get; set; }
	}

	public class NameStep : BlazorWizardStep<DraftModel> {
		public override string Title => "Name";
	}

	public class LockedStep : NameStep {
		public LockedStep() => IsCancellable = false;
	}

	public class ReviewStep : BlazorWizardStep<DraftModel> {
		public override string Title => "Review";

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Model.Name);
	}

	/// <summary>Renders the library's modal host and gives it to the scoped modal service, as an application layout does.</summary>
	public sealed class HostHarness : ComponentBase {
		[Inject] public ModalService Modals { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<ModalHost>(0);
			builder.AddAttribute(1, nameof(ModalHost.Id), "probe-modal");
			builder.AddComponentReferenceCapture(2, host => Modals.Initialize((ModalHost)host));
			builder.CloseComponent();
		}
	}

	/// <summary>The services, script double and rendered output of one case.</summary>
	private sealed class Scene {
		private readonly HtmlRootComponent _rendered;

		public Scene(IServiceProvider services, ModalRuntime runtime, HtmlRootComponent rendered) {
			Views = services.GetRequiredService<ViewService>();
			Modals = services.GetRequiredService<ModalService>();
			Components = services.GetRequiredService<CapturingActivator>();
			Runtime = runtime;
			_rendered = rendered;
		}

		public ViewService Views { get; }

		public ModalService Modals { get; }

		public CapturingActivator Components { get; }

		public ModalRuntime Runtime { get; }

		public string Html => _rendered.ToHtmlString();

		// The static renderer never reports a completed render, so the case reports it for the newest modal as a browser would.
		public async Task ShownAsync<TModal>(int shows)
			where TModal : ModalComponent {
			await ((IHandleAfterRender)Components.All<TModal>().Last()).OnAfterRenderAsync();
			await Runtime.ShownAsync(shows);
		}
	}

	private sealed class CapturingActivator(IServiceProvider provider) : IComponentActivator {
		private readonly List<IComponent> _components = new();

		public IComponent CreateInstance(Type componentType) {
			var component = (IComponent)ActivatorUtilities.CreateInstance(provider, componentType);
			_components.Add(component);
			return component;
		}

		public T[] All<T>()
			where T : IComponent => _components.OfType<T>().ToArray();
	}

	/// <summary>Stands in for the modal script module and records the calls the hosts make to it.</summary>
	private sealed class ModalRuntime : IJSRuntime, IJSObjectReference {
		private readonly List<string> _calls = new();
		private readonly Dictionary<int, TaskCompletionSource> _shown = new();

		public string[] Calls => _calls.ToArray();

		public Task ShownAsync(int shows) {
			if (_calls.Count(call => call == "Show") >= shows)
				return Task.CompletedTask;
			if (!_shown.TryGetValue(shows, out var shown))
				_shown.Add(shows, shown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
			return shown.Task.WaitAsync(TimeSpan.FromSeconds(5));
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) {
			if (identifier == "import")
				return ValueTask.FromResult((TValue)(object)this);
			Assert.That(args.Single(), Is.TypeOf<ElementReference>());
			_calls.Add(identifier);
			if (identifier == "Show" && _shown.Remove(_calls.Count(call => call == "Show"), out var shown))
				shown.SetResult();
			return ValueTask.FromResult<TValue>(default);
		}

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	#endregion
}
