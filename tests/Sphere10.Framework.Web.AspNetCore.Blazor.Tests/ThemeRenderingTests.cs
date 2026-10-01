// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Theming;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Theming;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ThemeRenderingTests {
	[Test]
	public async Task ThemeChangesUpdateBothAttributesAndToggleWithoutReplacingStatefulChildren() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var theme = provider.GetRequiredService<IThemeService>();
		var state = provider.GetRequiredService<ProbeState>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<ThemeHarness>();
			var originalChild = state.Child;
			Assert.That(rendered.ToHtmlString(), Does.Contain("data-sphere10-theme=\"light\"").And.Contain("data-bs-theme=\"light\""));
			Assert.That(rendered.ToHtmlString(), Does.Contain("aria-pressed=\"false\"").And.Contain("Dark theme"));
			await originalChild.SetDraftAsync("Unsaved draft");
			theme.SetTheme(ThemeMode.Dark);
			Assert.That(rendered.ToHtmlString(), Does.Contain("data-sphere10-theme=\"dark\"").And.Contain("data-bs-theme=\"dark\""));
			Assert.That(rendered.ToHtmlString(), Does.Contain("aria-pressed=\"true\"").And.Contain("Unsaved draft"));
			Assert.That(state.Child, Is.SameAs(originalChild));
			Assert.That(state.Created, Is.EqualTo(1));
			Assert.That(state.Disposed, Is.Zero);
			theme.Toggle();
			Assert.That(rendered.ToHtmlString(), Does.Contain("data-sphere10-theme=\"light\"").And.Contain("Unsaved draft"));
			theme.SetTheme(ThemeMode.Blue);
			Assert.That(rendered.ToHtmlString(), Does.Contain("data-sphere10-theme=\"blue\"").And.Contain("data-bs-theme=\"light\""));
			Assert.That(rendered.ToHtmlString(), Does.Contain("aria-label=\"Theme\"").And.Contain("value=\"blue\" selected").And.Contain(">Blue</option>"));
			theme.SetTheme(ThemeMode.ClassicBlue);
			Assert.That(rendered.ToHtmlString(), Does.Contain("data-sphere10-theme=\"classic-blue\"").And.Contain("data-bs-theme=\"light\""));
			Assert.That(rendered.ToHtmlString(), Does.Contain("value=\"classic-blue\" selected").And.Contain(">Classic blue</option>"));
			Assert.That(rendered.ToHtmlString(), Does.Contain("Unsaved draft"));
			Assert.That(state.Child, Is.SameAs(originalChild));
			Assert.That(state.Created, Is.EqualTo(1));
		});
	}

	[Test]
	public async Task StandaloneToggleObservesServiceChangesDispatchedOutsideRenderer() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var theme = provider.GetRequiredService<IThemeService>();
		var rendered = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<ThemeToggle>());
		await Task.Run(() => theme.SetTheme(ThemeMode.Dark));
		await renderer.Dispatcher.InvokeAsync(() => {
			Assert.That(rendered.ToHtmlString(), Does.Contain("type=\"button\"").And.Contain("aria-pressed=\"true\"").And.Contain("Dark theme"));
		});
	}

	[Test]
	public async Task RendererDisposalReleasesProviderAndToggleSubscriptions() {
		var theme = new TrackingThemeService(new ThemeService());
		await using var provider = CreateServices(theme).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<ThemeHarness>());
		Assert.That(theme.SubscriberCount, Is.EqualTo(3));
		await renderer.DisposeAsync();
		Assert.That(theme.SubscriberCount, Is.Zero);
		Assert.That(provider.GetRequiredService<ProbeState>().Disposed, Is.EqualTo(1));
		Assert.That(() => theme.Toggle(), Throws.Nothing);
	}

	private static IServiceCollection CreateServices(IThemeService theme = null) {
		var services = new ServiceCollection();
		services.AddLogging();
		if (theme != null)
			services.AddSingleton(theme);
		services.AddSphere10Blazor();
		services.AddSingleton<ProbeState>();
		return services;
	}

	public sealed class ProbeState {
		public ProbeComponent Child { get; set; }

		public int Created { get; set; }

		public int Disposed { get; set; }
	}

	public sealed class ThemeHarness : ComponentBase {
		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<ThemeProvider>(0);
			builder.AddAttribute(1, nameof(ThemeProvider.ChildContent), (RenderFragment)(content => {
				content.OpenComponent<ThemeToggle>(0);
				content.CloseComponent();
				content.OpenComponent<ThemeSelector>(1);
				content.CloseComponent();
				content.OpenComponent<ProbeComponent>(2);
				content.CloseComponent();
			}));
			builder.CloseComponent();
		}
	}

	public sealed class ProbeComponent : ComponentBase, IDisposable {
		private string _draft = "Original draft";

		[Inject]
		public ProbeState State { get; set; }

		public Task SetDraftAsync(string draft) {
			_draft = draft;
			return InvokeAsync(StateHasChanged);
		}

		public void Dispose() => State.Disposed++;

		protected override void OnInitialized() {
			State.Child = this;
			State.Created++;
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, _draft);
	}

	private sealed class TrackingThemeService : ThemeServiceDecorator {
		public override event EventHandlerEx Changed {
			add {
				SubscriberCount++;
				InternalThemeService.Changed += value;
			}
			remove {
				SubscriberCount--;
				InternalThemeService.Changed -= value;
			}
		}

		public TrackingThemeService(IThemeService service)
			: base(service) {
		}

		public int SubscriberCount { get; private set; }
	}
}
