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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.BlazorTester;
using Sphere10.Framework.Utils.BlazorTester.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorApplicationBuilderTests {
	[Test]
	public async Task BuildUsesExistingPluginAndScopedApplicationRegistrations() {
		var loaded = 0;
		var configured = new List<string>();
		var plugin = new BlazorPluginBuilder().WithName("Workspace")
			.AddBlock(block => block.WithId("workspace").WithName("Workspace").WithDefaultScreen<ProbeScreen>()).Build();
		plugin.Loaded += () => loaded++;
		var services = new ServiceCollection();
		var builder = services.BuildBlazorApplication()
			.WithTitle("Configured application").WithFavicon("img/application.svg", "image/svg+xml")
			.AddPlugin(plugin)
			.AddPlugin(definition => definition.WithName("Tools").AddBlock(block => block.WithId("tools").WithName("Tools")))
			.ConfigureApplication(_ => configured.Add("first"))
			.ConfigureApplication(_ => configured.Add("second"));
		Assert.That(services, Is.Empty, "Collecting builder settings must not register services or load plugins.");
		Assert.That(loaded, Is.Zero);
		Assert.That(builder.Build(), Is.SameAs(services));
		Assert.That(loaded, Is.EqualTo(1));
		await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		var options = provider.GetRequiredService<BlazorApplicationOptions>();
		Assert.That(options.Title, Is.EqualTo("Configured application"));
		Assert.That(options.FaviconUrl, Is.EqualTo("img/application.svg"));
		Assert.That(options.FaviconContentType, Is.EqualTo("image/svg+xml"));
		Assert.That(configured, Is.Empty, "Resolving singleton head metadata must not create the scoped application or screen host.");
		await using var first = provider.CreateAsyncScope();
		await using var second = provider.CreateAsyncScope();
		var application = first.ServiceProvider.GetRequiredService<IBlazorApplication>();
		Assert.That(first.ServiceProvider.GetRequiredService<IApplication>(), Is.SameAs(application));
		Assert.That(application.ScreenHost, Is.SameAs(first.ServiceProvider.GetRequiredService<IBlazorApplicationScreenHost>()));
		Assert.That(application.Blocks.Select(block => block.Id), Is.EquivalentTo(new[] { "workspace", "tools" }));
		Assert.That(application.Plugins.Select(item => item.Name), Is.EqualTo(new[] { "Workspace", "Tools" }));
		Assert.That(application.Plugins.First(), Is.SameAs(plugin));
		Assert.That(provider.GetServices<IApplicationPlugin>().ToArray(), Is.EqualTo(application.Plugins));
		Assert.That(((IApplication)application).Plugins.SelectMany(item => item.Blocks).Select(block => block.Id), Is.EquivalentTo(application.Blocks.Select(block => block.Id)));
		Assert.That(second.ServiceProvider.GetRequiredService<IBlazorApplication>(), Is.Not.SameAs(application));
		Assert.That(configured, Is.EqualTo(new[] { "first", "second", "first", "second" }));
		Assert.That(second.ServiceProvider.GetRequiredService<BlazorApplicationOptions>(), Is.SameAs(options));
		Assert.That(loaded, Is.EqualTo(1), "Starting another circuit must not rerun plugin startup.");
	}

	[Test]
	public void DefaultServicesProvideHeadMetadataWithoutScopedResolution() {
		var services = new ServiceCollection().AddSphere10Blazor();
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		var options = provider.GetRequiredService<BlazorApplicationOptions>();
		Assert.That(options.Title, Is.EqualTo(BlazorApplicationOptions.DefaultTitle));
		Assert.That(options.FaviconUrl, Is.Null);
		Assert.That(options.FaviconContentType, Is.Null);
	}

	[Test]
	public void BuildFreezesBrandingAndCannotRepeatPluginSideEffects() {
		var loaded = 0;
		var plugin = new BlazorPluginBuilder().WithName("Plugin").Build();
		plugin.Loaded += () => loaded++;
		var services = new ServiceCollection();
		var builder = services.BuildBlazorApplication().WithTitle("Original").WithFavicon("favicon.ico").AddPlugin(plugin);
		builder.Build();
		services.AddSphere10Blazor();
		using var provider = services.BuildServiceProvider();
		var options = provider.GetRequiredService<BlazorApplicationOptions>();
		Assert.That(() => builder.WithTitle("Changed"), Throws.InvalidOperationException);
		Assert.That(() => builder.WithFavicon("other.svg"), Throws.InvalidOperationException);
		Assert.That(() => builder.ConfigureApplication(_ => { }), Throws.InvalidOperationException);
		Assert.That(() => builder.AddPlugin(plugin), Throws.InvalidOperationException);
		Assert.That(() => builder.Build(), Throws.InvalidOperationException);
		Assert.That(options.Title, Is.EqualTo("Original"));
		Assert.That(options.FaviconUrl, Is.EqualTo("favicon.ico"));
		Assert.That(options.FaviconContentType, Is.Null, "The favicon's content type is optional.");
		Assert.That(loaded, Is.EqualTo(1));
		Assert.That(services.Count(descriptor => descriptor.ServiceType == typeof(BlazorApplicationOptions)), Is.EqualTo(1));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("  ")]
	public void EmptyApplicationTitleIsRejected(string title) {
		var builder = new ServiceCollection().BuildBlazorApplication();
		Assert.That(() => builder.WithTitle(title), Throws.ArgumentException);
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("  ")]
	public void EmptyFaviconUrlIsRejected(string url) {
		var builder = new ServiceCollection().BuildBlazorApplication();
		Assert.That(() => builder.WithFavicon(url), Throws.ArgumentException);
	}

	[Test]
	public void TesterStartupProvidesConfiguredBrandingAndDemoPlugins() {
		var services = new ServiceCollection();
		Program.ConfigureServices(services);
		using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
		var options = provider.GetRequiredService<BlazorApplicationOptions>();
		Assert.That(options.Title, Is.EqualTo("Sphere10 Blazor demos"));
		Assert.That(options.FaviconUrl, Is.EqualTo("img/logo.svg"));
		Assert.That(options.FaviconContentType, Is.EqualTo("image/svg+xml"));
		Assert.That(provider.GetServices<IBlazorPlugin>().Select(plugin => plugin.Name), Is.EquivalentTo(new[] { "Sphere10.Framework", "Widget Gallery", "Screen policies" }));
	}

	[Test]
	public async Task WorkspaceRendersConfiguredBrandingInHeadAndShell() {
		var services = new ServiceCollection().AddLogging();
		Program.ConfigureServices(services);
		services.BuildBlazorApplication().WithTitle("Custom & branded application").Build();
		services.AddSingleton<NavigationManager>(new TestNavigationManager());
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var html = await renderer.Dispatcher.InvokeAsync(async () => (await renderer.RenderComponentAsync<BrandingHost>()).ToHtmlString());
		Assert.That(html, Does.Contain("<title>Custom &amp; branded application</title>"));
		Assert.That(html, Does.Match("<strong[^>]*class=\"sphere10-application-title\"[^>]*>Custom &amp; branded application</strong>"));
	}

	public sealed class ProbeScreen : ComponentBase, IBlazorApplicationScreen {
	}

	public sealed class BrandingHost : ComponentBase {
		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<HeadOutlet>(0);
			builder.CloseComponent();
			builder.OpenComponent<Workspace>(1);
			builder.CloseComponent();
		}
	}
}
