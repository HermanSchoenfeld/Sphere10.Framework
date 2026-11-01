// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationSettingsStartupTests {
	private bool _startedFramework;
	private string _directory;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance.Build().Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[SetUp]
	public void CreateOwnedSettings() {
		_directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ApplicationSettingsStartup-" + Guid.NewGuid().ToString("N"))).FullName;
		SettingsBlock.Created = 0;
		SettingsProbe.CreationCount = 0;
		SettingsBlock.Activating = null;
	}

	[TearDown]
	public void RemoveOwnedSettings() {
		SettingsBlock.Activating = null;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(_directory);
	}

	[Test]
	public async Task CatalogRestoresBeforeDefaultActivationAndKeepsTheApplicationsCanonicalTransientBlocks() {
		var store = new WinFormsApplicationSettingsStore(new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory)));
		using (var seedBlock = new SettingsBlock()) {
			var seed = new ApplicationSettingsCatalog("SettingsTests", store, new[] { seedBlock });
			await seed.InitializeAsync();
			var edit = seed.GetProfile(seedBlock, seedBlock.DefaultScreenDefinition.Registration).BeginEdit();
			((Preferences)edit.Draft).Name = "Restored before query";
			Assert.That((await edit.ApplyAsync()).IsSuccess, Is.True);
			Assert.That(SettingsProbe.CreationCount, Is.Zero, "Catalog discovery and editing must not activate operational screens.");
		}
		SettingsBlock.Activating = context => {
			Assert.That(context.Application.Settings.IsInitialized, Is.True);
			Assert.That(context.Application.Settings.GetProfile(context.Block, context.Definition.Registration).GetValues<Preferences>().Name,
				Is.EqualTo("Restored before query"));
		};
		var services = new ServiceCollection();
		services.AddSingleton<IApplicationSettingsStore>(store);
		services.AddMainForm<BlockMainForm>();
		services.AddApplicationBlock<SettingsBlock>();
		services.AddApplicationSettings("SettingsTests");
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IWinFormsApplication>();
		await application.InitializeAsync();
		Assert.That(application.Settings, Is.SameAs(provider.GetRequiredService<ApplicationSettingsCatalog>()));
		Assert.That(application.Settings.Profiles.Single().Block, Is.SameAs(application.Blocks.Single()));
		Assert.That(SettingsBlock.Created, Is.EqualTo(2), "The catalog must not resolve transient block services a second time.");
		Assert.That(SettingsProbe.CreationCount, Is.EqualTo(1));
	}

	[Test]
	public async Task PlainMainFormUsesOneApplicationRootCatalogWithoutAnArtificialScreenOrApplication() {
		var definition = new ApplicationSettingsDefinition("preferences", "Preferences", new Preferences()).WithStorageId(null);
		var services = new ServiceCollection();
		services.AddSingleton<IApplicationSettingsStore>(new WinFormsApplicationSettingsStore(new DirectoryFileSettingsProvider(_directory)));
		services.AddMainForm<MainForm>();
		services.AddApplicationSettings("PlainSettingsTests", definition);
		await using var provider = services.BuildServiceProvider();
		var catalog = provider.GetRequiredService<ApplicationSettingsCatalog>();
		await catalog.InitializeAsync();
		Assert.That(provider.GetService<IApplication>(), Is.Null);
		Assert.That(catalog, Is.SameAs(provider.GetRequiredService<ApplicationSettingsCatalog>()));
		Assert.That(catalog.Profiles.Single().ScreenDefinition, Is.Null);
		Assert.That(catalog.Profiles.Single().ApplicationDefinition, Is.SameAs(definition));
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
		var edit = catalog.GetProfile("preferences").BeginEdit();
		((Preferences)edit.Draft).Name = "Accepted";
		Assert.That((await edit.ApplyAsync()).IsSuccess, Is.True);
		ISettingsProvider fresh = new DirectoryFileSettingsProvider(_directory);
		Assert.That(fresh.Get<Preferences>().Name, Is.EqualTo("Accepted"));
	}

	public class Preferences : SettingsObject {
		public string Name { get; set; } = "Default";
	}

	public class SettingsBlock : WinFormsApplicationBlock {
		public static int Created;

		public static Action<WinFormsApplicationScreenActivationContext> Activating;

		public SettingsBlock() {
			Created++;
			Name = "Workspace";
			var definition = new WinFormsApplicationScreenDefinition(typeof(SettingsProbe), "Settings probe", stableKey: "probe",
				activator: context => {
					Activating?.Invoke(context);
					return new SettingsProbe();
				}, settings: new Preferences());
			AddScreenDefinition(definition);
			DefaultScreenDefinition = definition;
		}
	}

	public class SettingsProbe : WinFormsApplicationScreen {
		public static int CreationCount;

		public SettingsProbe() {
			CreationCount++;
		}
	}
}
