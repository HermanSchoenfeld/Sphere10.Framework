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
using FormsUI = global::Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Checks actual provider restoration before native screen activation.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationSettingsStartupTests {
	private bool _startedFramework;
	private string _directory;

	// Start only the Framework lifetime required by this fixture.
	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance.Build().Start();
	}

	// End only the Framework lifetime started by this fixture.
	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	// Create isolated storage and reset activation observations.
	[SetUp]
	public void CreateOwnedSettings() {
		_directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "ApplicationSettingsStartup-" + Guid.NewGuid().ToString("N"))).FullName;
		SettingsBlock.Created = 0;
		SettingsProbe.CreationCount = 0;
		SettingsBlock.Activating = null;
	}

	// Remove only this fixture's records and activation callback.
	[TearDown]
	public void RemoveOwnedSettings() {
		SettingsBlock.Activating = null;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(_directory);
	}

	// Verify provider restoration precedes activation without resolving transient blocks twice.
	[Test]
	public async Task SettingsRestoreBeforeDefaultActivationAndKeepTheApplicationsCanonicalTransientBlocks() {
		ISettingsProvider store = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		using (var seedBlock = new SettingsBlock()) {
			var definition = seedBlock.DefaultScreenDefinition;
			var settings = (Preferences)definition.Settings;
			settings.ID = ApplicationHelper.GetScreenSettingsId("SettingsTests", seedBlock, definition);
			settings.Provider = store;
			settings.Name = "Restored before query";
			await settings.SaveAsync();
			Assert.That(SettingsProbe.CreationCount, Is.Zero, "Reading declarations must not activate operational screens.");
		}
		SettingsBlock.Activating = context => {
			Assert.That(context.Application.Settings, Is.SameAs(store));
			Assert.That(((Preferences)context.Definition.Settings).Name, Is.EqualTo("Restored before query"));
		};
		var services = new ServiceCollection();
		services.AddSingleton(new Local<ISettingsProvider>(store));
		services.AddMainForm<BlockMainForm>();
		services.AddApplicationBlock<SettingsBlock>();
		services.AddApplicationSettings("SettingsTests");
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<FormsUI.IApplication>();
		await application.InitializeAsync();
		Assert.That(application.Settings, Is.SameAs(store));
		Assert.That(application.Blocks.Single().ScreenDefinitions.Single().Settings.Provider, Is.SameAs(store));
		Assert.That(SettingsBlock.Created, Is.EqualTo(2), "Settings restoration must not resolve transient block services a second time.");
		Assert.That(SettingsProbe.CreationCount, Is.EqualTo(1));
	}

	// Verify a block uses its ordinary typed provider record without a synthetic screen.
	[Test]
	public async Task BlockPreferencesUseTheExistingLegacyUserRecordWithoutAnArtificialScreen() {
		using var block = new ApplicationBlock { Name = "Preferences", BlockSettings = new Preferences() };
		var services = new ServiceCollection();
		services.AddSingleton(new Local<ISettingsProvider>(new DirectoryFileSettingsProvider(_directory)));
		services.AddMainForm<BlockMainForm>();
		services.AddApplicationBlock(block);
		services.AddApplicationSettings("PlainSettingsTests");
		await using var provider = services.BuildServiceProvider();
		await provider.GetRequiredService<FormsUI.IApplication>().InitializeAsync();
		Assert.That(block.ScreenDefinitions, Is.Empty);
		Assert.That(Directory.GetFiles(_directory), Is.Empty);
		var settings = (Preferences)block.BlockSettings;
		settings.Name = "Accepted";
		await settings.SaveAsync();
		ISettingsProvider fresh = new DirectoryFileSettingsProvider(_directory);
		Assert.That(fresh.Get<Preferences>().Name, Is.EqualTo("Accepted"));
	}

	// Verify configured user settings services take precedence over a fallback provider.
	[Test]
	public async Task NativeApplicationUsesTheExistingSettingsServicesUserProvider() {
		var intendedDirectory = Directory.CreateDirectory(Path.Combine(_directory, "intended")).FullName;
		var unrelatedDirectory = Directory.CreateDirectory(Path.Combine(_directory, "unrelated")).FullName;
		ISettingsProvider intended = new DirectoryFileSettingsProvider(intendedDirectory);
		ISettingsProvider unrelated = new DirectoryFileSettingsProvider(unrelatedDirectory);
		using var block = new ApplicationBlock { Name = "Preferences", BlockSettings = new Preferences() };
		var services = new ServiceCollection();
		services.AddSingleton<ISettingsServices>(new ExistingSettingsServices(intended));
		services.AddSingleton(new Local<ISettingsProvider>(unrelated));
		services.AddMainForm<BlockMainForm>();
		services.AddApplicationBlock(block);
		services.AddApplicationSettings("Existing native user provider");
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<FormsUI.IApplication>();
		await application.InitializeAsync();
		var values = (Preferences)block.BlockSettings;
		values.Name = "Existing provider accepted";
		await values.SaveAsync();
		ISettingsProvider fresh = new DirectoryFileSettingsProvider(intendedDirectory);
		Assert.That(fresh.Get<Preferences>().Name, Is.EqualTo("Existing provider accepted"));
		Assert.That(Directory.GetFiles(unrelatedDirectory), Is.Empty);
	}

	#region Inner Types

	public class Preferences : SettingsObject {
		public string Name { get; set; } = "Default";
	}

	public class SettingsBlock : ApplicationBlock {
		public static int Created;

		public static Action<ApplicationScreenActivationContext> Activating;

		public SettingsBlock() {
			Created++;
			Name = "Workspace";
			var definition = new FormsUI.ApplicationScreenDefinition(typeof(SettingsProbe), "Settings probe", stableKey: "probe",
				activator: context => {
					Activating?.Invoke(context);
					return new SettingsProbe();
				});
			AddScreenDefinition(definition);
			DefaultScreenDefinition = definition;
		}
	}

	public class SettingsProbe : ApplicationScreen {
		public static int CreationCount;

		public override Preferences ScreenSettings => (Preferences)base.ScreenSettings;

		public SettingsProbe() {
			CreationCount++;
		}
	}

	private sealed class ExistingSettingsServices(ISettingsProvider provider) : BaseSettingsServices {
		public override ISettingsProvider UserSettings => provider;

		public override ISettingsProvider SystemSettings => provider;
	}

	#endregion

}
