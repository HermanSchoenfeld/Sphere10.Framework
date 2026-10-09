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
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Theming;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ThemeTests {
	[Test]
	public void ThemesAreSharedWithinOneScopeAndIsolatedBetweenScopes() {
		using var provider = new ServiceCollection().AddSphere10Blazor().BuildServiceProvider();
		using var firstScope = provider.CreateScope();
		using var secondScope = provider.CreateScope();
		var first = firstScope.ServiceProvider.GetRequiredService<IThemeService>();
		var second = secondScope.ServiceProvider.GetRequiredService<IThemeService>();
		Assert.That(first.CurrentTheme, Is.EqualTo(ThemeMode.Light));
		Assert.That(firstScope.ServiceProvider.GetRequiredService<IThemeService>(), Is.SameAs(first));
		first.Toggle();
		Assert.That(first.CurrentTheme, Is.EqualTo(ThemeMode.Dark));
		Assert.That(second.CurrentTheme, Is.EqualTo(ThemeMode.Light));
	}

	[Test]
	public void ChangesNotifyOnceAfterNewThemeIsVisible() {
		var service = new ThemeService();
		var observed = new List<ThemeMode>();
		service.Changed += () => observed.Add(service.CurrentTheme);
		service.SetTheme(ThemeMode.Light);
		service.SetTheme(ThemeMode.Dark);
		service.SetTheme(ThemeMode.Dark);
		service.Toggle();
		Assert.That(observed, Is.EqualTo(new[] { ThemeMode.Dark, ThemeMode.Light }));
	}

	[Test]
	public void InvalidThemeDoesNotChangeStateOrNotify() {
		var service = new ThemeService();
		service.SetTheme(ThemeMode.Dark);
		var notifications = 0;
		service.Changed += () => notifications++;
		Assert.That(() => service.SetTheme((ThemeMode)99), Throws.ArgumentException);
		Assert.That(service.CurrentTheme, Is.EqualTo(ThemeMode.Dark));
		Assert.That(notifications, Is.Zero);
	}

	[Test]
	public void RegistrationPreservesCustomThemeService() {
		var custom = new ForwardingThemeService(new ThemeService());
		using var provider = new ServiceCollection().AddScoped<IThemeService>(_ => custom).AddSphere10Blazor().BuildServiceProvider();
		using var scope = provider.CreateScope();
		Assert.That(scope.ServiceProvider.GetRequiredService<IThemeService>(), Is.SameAs(custom));
	}

	[Test]
	public void DecoratorForwardsOperationsAndEventUnsubscription() {
		var inner = new ThemeService();
		var decorated = new ForwardingThemeService(inner);
		var notifications = 0;
		EventHandlerEx handler = () => notifications++;
		decorated.Changed += handler;
		decorated.SetTheme(ThemeMode.Dark);
		Assert.That(inner.CurrentTheme, Is.EqualTo(ThemeMode.Dark));
		inner.Toggle();
		Assert.That(decorated.CurrentTheme, Is.EqualTo(ThemeMode.Light));
		Assert.That(notifications, Is.EqualTo(2));
		decorated.Changed -= handler;
		decorated.Toggle();
		Assert.That(inner.CurrentTheme, Is.EqualTo(ThemeMode.Dark));
		Assert.That(notifications, Is.EqualTo(2));
	}

	[Test]
	public void DecoratorRejectsMissingService() => Assert.That(() => new ForwardingThemeService(null), Throws.ArgumentNullException);

	[Test]
	public void ConstructorAcceptsEverySupportedInitialSkin([Values] ThemeMode initialTheme) {
		var service = new ThemeService(initialTheme);
		Assert.That(service.CurrentTheme, Is.EqualTo(initialTheme));
	}

	[Test]
	public void ConstructorRejectsUnsupportedInitialSkin() =>
		Assert.That(() => new ThemeService((ThemeMode)99), Throws.ArgumentException);

	[TestCase(ThemeMode.Blue)]
	[TestCase(ThemeMode.ClassicBlue)]
	public void BlueThemesCanBeSelectedExplicitlyAndCompatibilityToggleSelectsDarkThenLight(ThemeMode theme) {
		var service = new ThemeService();
		var observed = new List<ThemeMode>();
		service.Changed += () => observed.Add(service.CurrentTheme);
		service.SetTheme(theme);
		service.SetTheme(theme);
		service.Toggle();
		service.Toggle();
		Assert.That(observed, Is.EqualTo(new[] { theme, ThemeMode.Dark, ThemeMode.Light }));
	}

	[Test]
	public void CatalogIncludesEveryThemeWithUniqueIdsAndTemplateStylesheets() {
		var themes = ThemeCatalog.All;
		Assert.That(themes.Select(theme => theme.Mode), Is.EquivalentTo(Tools.Enums.GetValues<ThemeMode>()));
		Assert.That(themes.Select(theme => theme.Id), Is.Unique);
		Assert.That(themes.Select(theme => theme.Name), Is.Unique);
		Assert.That(themes.All(theme => !string.IsNullOrWhiteSpace(theme.Name)), Is.True);
		Assert.That(themes.All(theme => theme.StylesheetPath.StartsWith("_content/Sphere10.Framework.Web.AspNetCore.Blazor/css/themes/", StringComparison.Ordinal)), Is.True);
		Assert.That(themes.All(theme => theme.StylesheetPath.EndsWith(".css", StringComparison.Ordinal)), Is.True);
	}

	[Test]
	public void CatalogLookupsRoundTripEveryTheme([Values] ThemeMode mode) {
		var definition = ThemeCatalog.Get(mode);
		Assert.That(ThemeCatalog.TryGet(definition.Id, out var selected), Is.True);
		Assert.That(selected, Is.SameAs(definition));
		Assert.That(selected.Mode, Is.EqualTo(mode));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("unknown-theme")]
	public void CatalogRejectsUnknownSelection(string id) {
		Assert.That(ThemeCatalog.TryGet(id, out var definition), Is.False);
		Assert.That(definition, Is.Null);
	}

	[Test]
	public void CatalogMembershipCannotBeChangedByConsumers() {
		var themes = ThemeCatalog.All;
		var original = themes[0];
		themes[0] = null;
		Assert.That(ThemeCatalog.All[0], Is.SameAs(original));
		Assert.That(ThemeCatalog.Get(original.Mode), Is.SameAs(original));
	}

	[Test]
	public void EachThemeCanBeSelectedAndToggledAccordingToItsColorScheme([Values] ThemeMode mode) {
		var service = new ThemeService(mode == ThemeMode.Light ? ThemeMode.Dark : ThemeMode.Light);
		var observed = new List<ThemeMode>();
		service.Changed += () => observed.Add(service.CurrentTheme);
		service.SetTheme(mode);
		service.SetTheme(mode);
		Assert.That(observed, Is.EqualTo(new[] { mode }), "Selecting an unchanged theme must not notify consumers again.");
		service.Toggle();
		Assert.That(service.CurrentTheme, Is.EqualTo(ThemeCatalog.Get(mode).IsDark ? ThemeMode.Light : ThemeMode.Dark));
	}

	[Test]
	public void HostConfiguredBlueDefaultIsStillIndependentPerScope() {
		using var provider = new ServiceCollection().AddScoped<IThemeService>(_ => new ThemeService(ThemeMode.Blue)).AddSphere10Blazor().BuildServiceProvider();
		using var firstScope = provider.CreateScope();
		using var secondScope = provider.CreateScope();
		var first = firstScope.ServiceProvider.GetRequiredService<IThemeService>();
		var second = secondScope.ServiceProvider.GetRequiredService<IThemeService>();
		Assert.That(first.CurrentTheme, Is.EqualTo(ThemeMode.Blue));
		first.SetTheme(ThemeMode.Dark);
		Assert.That(second.CurrentTheme, Is.EqualTo(ThemeMode.Blue));
	}
	private sealed class ForwardingThemeService : ThemeServiceDecorator {
		public ForwardingThemeService(IThemeService service)
			: base(service) {
		}
	}
}
