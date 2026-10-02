// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class IdentityControlTests {
	[TestCase(true, "Ada Lovelace", null, "Ada Lovelace")]
	[TestCase(true, null, null, "Signed-in user")]
	[TestCase(true, "", null, "Signed-in user")]
	[TestCase(false, "Unauthenticated name", null, "Guest")]
	[TestCase(false, null, "Visitor", "Visitor")]
	public async Task ClaimsAndOverridesLabelTheAvatarAndShowIdentityDetailsOnlyInTheDropdown(bool authenticated, string name, string displayName, string expected) {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<ProbeIdentity>(Parameters(new() {
				[nameof(IdentityControl.User)] = Principal(authenticated, name),
				[nameof(IdentityControl.DisplayName)] = displayName
			}));
			Assert.That(rendered.ToHtmlString(), Does.Contain($"aria-label=\"{expected}\"").And.Contain("aria-haspopup=\"menu\"").And.Contain("aria-expanded=\"false\""));
			Assert.That(rendered.ToHtmlString(), Does.Contain($"title=\"{expected}\"").And.Contain("sphere10-identity-silhouette").And.Contain("focusable=\"false\""));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain($">{expected}</strong>").And.Not.Contain("sphere10-identity-caption"));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("person@example.test"), "Secondary identity text belongs inside the dropdown, keeping the trigger compact.");
			await provider.GetRequiredService<ProbeState>().Control.OpenAsync();
			Assert.That(rendered.ToHtmlString(), Does.Contain("role=\"menu\"").And.Contain("aria-expanded=\"true\"").And.Contain($">{expected}</strong>"));
			if (authenticated)
				Assert.That(rendered.ToHtmlString(), Does.Contain("person@example.test"));
		});
	}

	[Test]
	public async Task TemplatesReplacePresentationAndReceiveTheCurrentPrincipal() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var user = Principal(true, "Ada");
			var rendered = await renderer.RenderComponentAsync<ProbeIdentity>(Parameters(new() {
				[nameof(IdentityControl.User)] = user,
				[nameof(IdentityControl.DisplayName)] = "Display override",
				[nameof(IdentityControl.SecondaryText)] = "Team override",
				[nameof(IdentityControl.AvatarUrl)] = "img/avatar.svg"
			}));
			var control = provider.GetRequiredService<ProbeState>().Control;
			await control.OpenAsync();
			Assert.That(rendered.ToHtmlString(), Does.Contain("src=\"img/avatar.svg\"").And.Contain(" alt").And.Contain("Team override").And.Not.Contain("sphere10-identity-silhouette"));
			Assert.That(control.EffectiveUser, Is.SameAs(user));
			await control.SetParametersAsync(Parameters(new() {
				[nameof(IdentityControl.AvatarTemplate)] = Template("Avatar"),
				[nameof(IdentityControl.IdentityTemplate)] = Template("Identity"),
				[nameof(IdentityControl.MenuFooter)] = Template("Footer")
			}));
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain("Avatar:Ada").And.Contain("Identity:Ada").And.Contain("Footer:Ada"));
			Assert.That(html, Does.Not.Contain("img/avatar.svg").And.Not.Contain("Team override"));
			Assert.That(html, Does.Contain("aria-label=\"Display override\""), "Custom visible templates must retain the configured accessible name.");
			await control.DismissAsync();
			Assert.That(rendered.ToHtmlString(), Does.Contain("Avatar:Ada").And.Not.Contain("Identity:Ada"), "IdentityTemplate belongs in the dropdown, leaving the trigger as the avatar.");
			await control.SetParametersAsync(Parameters(new() { [nameof(IdentityControl.MenuHeader)] = Template("Header") }));
			await control.OpenAsync();
			Assert.That(rendered.ToHtmlString(), Does.Contain("Header:Ada").And.Contain("Footer:Ada").And.Not.Contain("Identity:Ada"), "MenuHeader overrides the complete identity header.");
		});
	}

	[Test]
	public async Task IdentityAndMenuChangesDismissStaleContentAndOnlyDispatchCurrentItems() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var profile = Command("profile", "Profile");
			var signIn = Command("sign-in", "Use account");
			IBlazorApplicationMenuItem selected = null;
			var rendered = await renderer.RenderComponentAsync<ProbeIdentity>(Parameters(new() {
				[nameof(IdentityControl.User)] = Principal(true, "Ada"),
				[nameof(IdentityControl.Items)] = new[] { profile },
				[nameof(IdentityControl.OnSelect)] = EventCallback.Factory.Create<IBlazorApplicationMenuItem>(new object(), item => selected = item)
			}));
			var control = provider.GetRequiredService<ProbeState>().Control;
			await control.OpenAsync();
			await control.SetParametersAsync(Parameters(new() {
				[nameof(IdentityControl.User)] = new ClaimsPrincipal(new ClaimsIdentity()),
				[nameof(IdentityControl.Items)] = new[] { signIn }
			}));
			Assert.That(rendered.ToHtmlString(), Does.Contain("aria-label=\"Guest\"").And.Not.Contain("role=\"menu\""));
			await control.SelectAsync(profile);
			Assert.That(selected, Is.Null);
			await control.OpenAsync();
			Assert.That(rendered.ToHtmlString(), Does.Contain("Use account").And.Not.Contain("Profile"));
			await control.SelectAsync(signIn);
			Assert.That(selected, Is.SameAs(signIn));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("role=\"menu\""));
		});
	}

	[Test]
	public async Task OverridableAsyncSelectionPreventsDuplicateExecutionAndRecoversAfterCompletion() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var command = Command("profile", "Profile");
			var rendered = await renderer.RenderComponentAsync<ProbeIdentity>(Parameters(new() { [nameof(IdentityControl.Items)] = new[] { command } }));
			var control = provider.GetRequiredService<ProbeState>().Control;
			var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			using var unblock = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
			control.Handler = async _ => {
				entered.TrySetResult();
				await release.Task;
			};
			await control.OpenAsync();
			var pending = control.SelectAsync(command);
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(rendered.ToHtmlString(), Does.Contain(" disabled").And.Not.Contain("role=\"menu\""));
			await control.SelectAsync(command);
			Assert.That(control.Calls, Is.EqualTo(1));
			release.SetResult();
			await pending;
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain(" disabled"));
			await control.OpenAsync();
			Assert.That(rendered.ToHtmlString(), Does.Contain("role=\"menu\""));
		});
	}

	[Test]
	public async Task SeparatorsHoverAndDismissalUseTheExistingMenuDefinitions() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var hovered = 0;
			var profile = Command("profile", "Profile");
			profile.Hover += () => hovered++;
			var rendered = await renderer.RenderComponentAsync<ProbeIdentity>(Parameters(new() {
				[nameof(IdentityControl.Items)] = new IBlazorApplicationMenuItem[] { profile, new BlazorMenuSeparator(), Command("sign-out", "Sign out") }
			}));
			var control = provider.GetRequiredService<ProbeState>().Control;
			await control.OpenAsync();
			control.Hover(profile);
			Assert.That(hovered, Is.EqualTo(1));
			Assert.That(rendered.ToHtmlString(), Does.Contain("role=\"separator\"").And.Contain("role=\"menuitem\"").And.Contain("Dismiss identity menu"));
			await control.EscapeAsync();
			Assert.That(rendered.ToHtmlString(), Does.Contain("aria-expanded=\"false\"").And.Not.Contain("Dismiss identity menu"));
			await control.OpenAsync();
			await control.DismissAsync();
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("role=\"menu\""));
		});
	}

	[Test]
	public async Task FailedActionsRemainObservableAndDoNotLeaveTheControlBusy() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var item = Command("retry", "Retry");
			var rendered = await renderer.RenderComponentAsync<ProbeIdentity>(Parameters(new() { [nameof(IdentityControl.Items)] = new[] { item } }));
			var control = provider.GetRequiredService<ProbeState>().Control;
			control.Handler = _ => Task.FromException(new InvalidOperationException("Command failed"));
			await Assert.ThatAsync(async () => await control.SelectAsync(item), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Command failed"));
			control.Handler = null;
			await control.OpenAsync();
			await control.SelectAsync(item);
			Assert.That(control.Calls, Is.EqualTo(2));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain(" disabled"));
		});
	}

	[Test]
	public async Task KeyboardInteropImportsOnceAndDisposesItsListenerAndModuleOnce() {
		var runtime = new IdentityJsRuntime();
		await using var provider = Services(runtime).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			await renderer.RenderComponentAsync<ProbeIdentity>();
			var control = provider.GetRequiredService<ProbeState>().Control;
			await Task.WhenAll(control.SynchronizeBrowserAsync(), control.SynchronizeBrowserAsync());
			Assert.That(runtime.Imports, Is.EqualTo(1));
			Assert.That(runtime.ImportPath, Does.EndWith("/IdentityControl/IdentityControl.razor.js"));
			Assert.That(runtime.Module.Initialized, Is.EqualTo(1));
			await control.DisposeAsync();
			await control.DisposeAsync();
			Assert.That(runtime.Module.ListenersRemoved, Is.EqualTo(1));
			Assert.That(runtime.Module.Disposals, Is.EqualTo(1));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task DisposalWaitsForAnImportWithoutInitializingADetachedControl(bool cancelImport) {
		var runtime = new IdentityJsRuntime();
		var imported = new TaskCompletionSource<IJSObjectReference>(TaskCreationOptions.RunContinuationsAsynchronously);
		runtime.Import = imported.Task;
		using var completeImport = Tools.Scope.ExecuteOnDispose(() => imported.TrySetResult(runtime.Module));
		await using var provider = Services(runtime).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			await renderer.RenderComponentAsync<ProbeIdentity>();
			var control = provider.GetRequiredService<ProbeState>().Control;
			var initialization = control.SynchronizeBrowserAsync();
			var disposal = control.DisposeAsync().AsTask();
			Assert.That(disposal.IsCompleted, Is.False);
			if (cancelImport)
				imported.SetCanceled();
			else
				imported.SetResult(runtime.Module);
			await Task.WhenAll(initialization, disposal).WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(runtime.Module.Initialized, Is.Zero);
			Assert.That(runtime.Module.Disposals, Is.EqualTo(cancelImport ? 0 : 1));
		});
	}

	[Test]
	public async Task CanceledOwnedModuleTeardownDoesNotEscapeDisposal() {
		var runtime = new IdentityJsRuntime();
		runtime.Module.CancelDisposal = true;
		await using var provider = Services(runtime).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			await renderer.RenderComponentAsync<ProbeIdentity>();
			var control = provider.GetRequiredService<ProbeState>().Control;
			await control.SynchronizeBrowserAsync();
			await control.DisposeAsync();
			Assert.That(runtime.Module.ListenersRemoved, Is.EqualTo(1));
			Assert.That(runtime.Module.Disposals, Is.EqualTo(1));
		});
	}

	private static ServiceCollection Services(IdentityJsRuntime runtime = null) {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<ProbeState>();
		services.AddSingleton<IJSRuntime>(runtime ?? new IdentityJsRuntime());
		return services;
	}

	private static ParameterView Parameters(Dictionary<string, object> parameters) => ParameterView.FromDictionary(parameters);

	private static ClaimsPrincipal Principal(bool authenticated, string name) {
		var claims = new List<Claim> { new(ClaimTypes.Email, "person@example.test") };
		if (name != null)
			claims.Add(new Claim(ClaimTypes.Name, name));
		return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticated ? "Test" : null));
	}

	private static BlazorActionMenuItem Command(string id, string title) => new() { Id = id, Title = title, Action = () => { } };

	private static RenderFragment<ClaimsPrincipal> Template(string label) => user => builder => builder.AddContent(0, label + ":" + user.Identity?.Name);

	public sealed class ProbeState {
		public ProbeIdentity Control { get; set; }
	}

	public class ProbeIdentity : IdentityControl {
		[Inject] public ProbeState State { get; set; }

		public Func<IBlazorApplicationMenuItem, Task> Handler { get; set; }

		public int Calls { get; private set; }

		protected override void OnInitialized() => State.Control = this;

		public Task OpenAsync() => InvokeAsync(() => { OpenMenu(); StateHasChanged(); });

		public Task DismissAsync() => InvokeAsync(() => { CloseMenu(); StateHasChanged(); });

		public Task EscapeAsync() => InvokeAsync(() => { OnMenuKeyDown(new KeyboardEventArgs { Key = "Escape" }); StateHasChanged(); });

		public Task SynchronizeBrowserAsync() => base.OnAfterRenderAsync(false);

		public void Hover(IBlazorApplicationMenuItem item) => OnItemHover(item);

		public async Task SelectAsync(IBlazorApplicationMenuItem item) {
			using var render = Tools.Scope.ExecuteOnDispose(StateHasChanged);
			await SelectItemAsync(item);
		}

		protected override async Task OnItemSelectedAsync(IBlazorApplicationMenuItem item) {
			Calls++;
			if (Handler != null)
				await Handler(item);
			else
				await base.OnItemSelectedAsync(item);
		}
	}

	private sealed class IdentityJsRuntime : IJSRuntime {
		public IdentityJsRuntime() {
			Import = Task.FromResult<IJSObjectReference>(Module);
		}

		public IdentityJsModule Module { get; } = new();

		public Task<IJSObjectReference> Import { get; set; }

		public int Imports { get; private set; }

		public string ImportPath { get; private set; }

		public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			Assert.That(identifier, Is.EqualTo("import"));
			Imports++;
			ImportPath = (string)args[0];
			return (TValue)(object)await Import;
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);
	}

	private sealed class IdentityJsModule : IJSObjectReference {
		public int Initialized { get; private set; }

		public int ListenersRemoved { get; private set; }

		public int Disposals { get; private set; }

		public bool CancelDisposal { get; set; }

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			if (identifier == "Initialize")
				Initialized++;
			else {
				Assert.That(identifier, Is.EqualTo("Dispose"));
				ListenersRemoved++;
			}
			return ValueTask.FromResult(default(TValue));
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);

		public ValueTask DisposeAsync() {
			Disposals++;
			return CancelDisposal ? ValueTask.FromCanceled(new CancellationToken(true)) : ValueTask.CompletedTask;
		}
	}

}
