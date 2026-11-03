// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationSettingsTests {

	[Test]
	public void StaticDeclarationsDiscoverSettingsWithoutConstructingScreensOrReadingTheirProperties() {
		var definition = new ApplicationScreenDefinition(typeof(DiscoveryScreen));

		Assert.That(ApplicationHelper.GetScreenSettingsType(typeof(DiscoveryScreen)), Is.EqualTo(typeof(DiscoveryScreen.Preferences)));
		Assert.That(definition.Settings, Is.TypeOf<DiscoveryScreen.Preferences>());
		Assert.That(((DiscoveryScreen.Preferences)definition.Settings).Number, Is.EqualTo(10));
		Assert.That(() => new ApplicationScreenBuilder<DiscoveryScreen>().UseSettings<OtherPreferences>().Build(), Throws.ArgumentException);
	}

	[Test]
	public void ExistingAttributeDeclarationsAndCovariantPropertiesIdentifyTheSameSettingsType() {
		Assert.That(ApplicationHelper.GetScreenSettingsType(typeof(AttributeScreen)), Is.EqualTo(typeof(DiscoveryScreen.Preferences)));
		Assert.That(new ApplicationScreenDefinition(typeof(AttributeScreen)).Settings, Is.TypeOf<DiscoveryScreen.Preferences>());
		Assert.That(ApplicationHelper.GetScreenSettingsType(typeof(MatchingScreen)), Is.EqualTo(typeof(DiscoveryScreen.Preferences)));
		Assert.That(() => new ApplicationScreenDefinition(typeof(ConflictingScreen)), Throws.ArgumentException);
	}

	// Attribute-only registrations retain normal settings activation without operational screen construction.
	[TestCase(typeof(SettingsAttributeScreen))]
	[TestCase(typeof(InheritedSettingsAttributeScreen))]
	[TestCase(typeof(MatchingSettingsPropertyScreen))]
	public void SettingsAttributesDiscoverInheritedSchemasAndActivateDeclaredDefaults(Type screenType) {
		var definition = new ApplicationScreenBuilder()
			.WithScreen(screenType)
			.Build();

		Assert.That(ApplicationHelper.GetScreenSettingsType(screenType), Is.EqualTo(typeof(DiscoveryScreen.Preferences)));
		Assert.That(definition.Settings, Is.TypeOf<DiscoveryScreen.Preferences>());
		Assert.That(((DiscoveryScreen.Preferences)definition.Settings).Number, Is.EqualTo(10));
	}

	// Matching explicit values keep their owner identity; a different schema is a registration error.
	[Test]
	public void SettingsAttributesAndBuilderDeclarationsMustAgreeWithoutReplacingSuppliedValues() {
		var supplied = new DiscoveryScreen.Preferences { Number = 37 };
		var definition = new ApplicationScreenBuilder<SettingsAttributeScreen>()
			.UseSettings(supplied)
			.Build();

		Assert.That(definition.Settings, Is.SameAs(supplied));
		Assert.That(supplied.Number, Is.EqualTo(37));
		Assert.That(() => new ApplicationScreenBuilder<SettingsAttributeScreen>().UseSettings<OtherPreferences>().Build(), Throws.ArgumentException);
	}

	// The new spelling shares the existing attribute contract rather than adding a second declaration mechanism.
	[Test]
	public void MatchingSettingsAndLegacyAttributeDeclarationsUseTheSameSchema() {
		var definition = new ApplicationScreenDefinition(typeof(MatchingLegacySettingsScreen));

		Assert.That(definition.Settings, Is.TypeOf<DiscoveryScreen.Preferences>());
	}

	// Invalid schemas and contradictory declarations fail while building the registration, before screen activation.
	[TestCase(typeof(NullSettingsScreen))]
	[TestCase(typeof(NonSettingsScreen))]
	[TestCase(typeof(AbstractSettingsScreen))]
	[TestCase(typeof(OpenGenericSettingsScreen))]
	[TestCase(typeof(NoDefaultConstructorSettingsScreen))]
	[TestCase(typeof(ConflictingSettingsPropertyScreen))]
	[TestCase(typeof(ConflictingLegacySettingsScreen))]
	public void SettingsAttributesRejectInvalidOrConflictingSchemas(Type screenType) {
		Assert.That(() => new ApplicationScreenBuilder().WithScreen(screenType).Build(), Throws.ArgumentException);
	}

	// Compatible subclasses remain valid explicit values for an existing declared base schema.
	[Test]
	public void SettingsAttributesPreserveCompatibleDerivedBuilderValues() {
		var supplied = new PolymorphicSettingsScreen.VariantPreferences { Number = 41 };
		var definition = new ApplicationScreenBuilder<PolymorphicSettingsScreen>()
			.UseSettings(supplied)
			.Build();

		Assert.That(definition.Settings, Is.SameAs(supplied));
		Assert.That(supplied.Number, Is.EqualTo(41));
	}

	// Abstract schema declarations retain their existing support for an explicitly supplied concrete root.
	[TestCase(typeof(AbstractSettingsScreen))]
	[TestCase(typeof(LegacyAbstractSettingsScreen))]
	public void AbstractSettingsSchemasAcceptCompatibleExplicitBuilderValues(Type screenType) {
		var supplied = new AbstractSettingsScreen.SuppliedPreferences();
		var definition = new ApplicationScreenBuilder()
			.WithScreen(screenType)
			.UseSettings(supplied)
			.Build();

		Assert.That(definition.Settings, Is.SameAs(supplied));
	}

	[Test]
	public void SuppliedSettingsRemainTheActualInstancesOnDefinitionsBlocksAndPlugins() {
		var screenSettings = new DiscoveryScreen.Preferences { Number = 21 };
		var blockSettings = new OtherPreferences();
		var pluginSettings = new OtherPreferences();
		var definition = new ApplicationScreenBuilder<DiscoveryScreen>().UseSettings(screenSettings).Build();
		var block = new TestApplicationBlock("Inspect", definition) { BlockSettings = blockSettings };
		var plugin = new ApplicationPluginBuilder()
			.WithName("Sampling")
			.UseSettings(pluginSettings)
			.AddBlock(block)
			.Build();

		Assert.That(definition.Settings, Is.SameAs(screenSettings));
		Assert.That(definition.Clone().Settings, Is.SameAs(screenSettings));
		Assert.That(block.BlockSettings, Is.SameAs(blockSettings));
		Assert.That(plugin.PluginSettings, Is.SameAs(pluginSettings));
	}

	[Test]
	public void SuppliedInstancesKeepIndependentVariantsForTheSameScreenType() {
		var cpu = new ApplicationScreenBuilder<DiscoveryScreen>()
			.WithStableKey("cpu")
			.UseSettings(new DiscoveryScreen.Preferences { Number = 21 })
			.Build();
		var gpu = new ApplicationScreenBuilder<DiscoveryScreen>()
			.WithStableKey("gpu")
			.UseSettings(new DiscoveryScreen.Preferences { Number = 32 })
			.Build();
		var block = new TestApplicationBlock("Inspect", cpu);
		block.AddScreenDefinition(gpu);

		Assert.That(((DiscoveryScreen.Preferences)cpu.Settings).Number, Is.EqualTo(21));
		Assert.That(((DiscoveryScreen.Preferences)gpu.Settings).Number, Is.EqualTo(32));
		Assert.That(gpu.Settings, Is.Not.SameAs(cpu.Settings));
		Assert.That(ApplicationHelper.GetScreenSettingsId("application", block, gpu), Is.Not.EqualTo(ApplicationHelper.GetScreenSettingsId("application", block, cpu)));
	}
	[Test]
	public void ResetUsesTheExistingDefaultValueAttributesOnTheActualSettings() {
		var provider = new RecordingProvider();
		var settings = new DiscoveryScreen.Preferences { Number = 42, ID = "explicit", Provider = provider };
		var changes = 0;
		settings.OnChanged += () => changes++;

		settings.RestoreDefaultValues();

		Assert.That(settings.Number, Is.EqualTo(10));
		Assert.That(settings.ID, Is.EqualTo("explicit"));
		Assert.That(settings.Provider, Is.SameAs(provider));
		Assert.That(changes, Is.Zero, "Reset is an edit; persistence publishes its eventual acceptance.");
	}

	[Test]
	public void ScreenRecordIdsPreserveTheExistingHashAndIgnoreDisplayedCaptions() {
		var definition = new ApplicationScreenDefinition(typeof(DiscoveryScreen), "Original", stableKey: "preferences");
		var block = new TestApplicationBlock("Inspect", definition);
		var renamed = definition.WithTitle("Renamed");
		block.DefaultScreenDefinition = null;
		block.RemoveScreenDefinition(definition);
		block.AddScreenDefinition(renamed);

		Assert.That(ApplicationHelper.GetScreenSettingsKey("application", block, renamed), Is.EqualTo("application/screen/standalone/Inspect/preferences"));
		Assert.That(ApplicationHelper.GetScreenSettingsId("application", block, renamed),
			Is.EqualTo("1888ec4cba1007baa12506b40aefb57ad2f6e080aefbf4e833d9ff5f2fa4eda6"));
	}

	[Test]
	public void ScreenRecordKeysEscapeOwnerSegmentsAndSupportAStableBlockKey() {
		var definition = new ApplicationScreenDefinition(typeof(DiscoveryScreen), stableKey: "cpu/gpu");
		var block = new TestApplicationBlock("Renamed block", definition) {
			Source = new ApplicationPluginBuilder().WithName("Diagnostics/plugin").Build()
		};

		Assert.That(ApplicationHelper.GetScreenSettingsKey("app/key", block, definition, "stable/block"),
			Is.EqualTo("app%2Fkey/screen/Diagnostics%2Fplugin/stable%2Fblock/cpu%2Fgpu"));
	}

	[Test]
	public void ExplicitRecordIdsKeepTheirOriginalTypeAndRepeatedRegistrationsNeedStableKeys() {
		var identity = Guid.NewGuid();
		var definition = new ApplicationScreenDefinition(typeof(DiscoveryScreen), settings: new DiscoveryScreen.Preferences { ID = identity });
		var block = new TestApplicationBlock("Inspect", definition);

		Assert.That(ApplicationHelper.GetScreenSettingsId("application", block, definition), Is.EqualTo(identity).And.TypeOf<Guid>());
		definition.Settings.ID = null;
		block.AddScreenDefinition(new ApplicationScreenDefinition(typeof(DiscoveryScreen)));
		Assert.That(() => ApplicationHelper.GetScreenSettingsId("application", block, definition), Throws.ArgumentException);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task SavingNotifiesConsumersOnTheActualSettingsAfterProviderAcceptance(bool asynchronous) {
		var provider = new RecordingProvider();
		var settings = new DiscoveryScreen.Preferences { Provider = provider, Number = 42 };
		var definition = new ApplicationScreenDefinition(typeof(DiscoveryScreen), settings: settings);
		var changes = 0;
		SettingsObject observed = null;
		definition.Settings.OnChanged += () => {
			changes++;
			observed = provider.Saved;
		};

		if (asynchronous)
			await settings.SaveAsync();
		else
			settings.Save();

		Assert.That(changes, Is.EqualTo(1));
		Assert.That(observed, Is.SameAs(settings));
		Assert.That(provider.SaveCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task PendingAsynchronousSaveDoesNotNotifyBeforePersistenceFinishes() {
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var provider = new RecordingProvider {
			BeforeSave = async () => {
				entered.SetResult();
				await release.Task;
			}
		};
		var settings = new DiscoveryScreen.Preferences { Provider = provider };
		var changes = 0;
		settings.OnChanged += () => changes++;

		var save = settings.SaveAsync();
		await entered.Task;
		Assert.That(save.IsCompleted, Is.False);
		Assert.That(changes, Is.Zero);
		release.SetResult();
		await save;
		Assert.That(changes, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task FailedSavesDoNotNotifyConsumersAndCanBeRetried(bool asynchronous) {
		var provider = new RecordingProvider { SaveError = new IOException("Rejected") };
		var settings = new DiscoveryScreen.Preferences { Provider = provider };
		var changes = 0;
		settings.OnChanged += () => changes++;

		if (asynchronous)
			await Assert.ThatAsync(async () => await settings.SaveAsync(), Throws.TypeOf<IOException>());
		else
			Assert.That(() => settings.Save(), Throws.TypeOf<IOException>());

		Assert.That(changes, Is.Zero);
		provider.SaveError = null;
		await settings.SaveAsync();
		Assert.That(changes, Is.EqualTo(1));
	}

	[Test]
	public async Task CancelledSavesDoNotWriteOrNotify() {
		var provider = new RecordingProvider();
		var settings = new DiscoveryScreen.Preferences { Provider = provider };
		var changes = 0;
		settings.OnChanged += () => changes++;
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		await Assert.ThatAsync(async () => await settings.SaveAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());

		Assert.That(provider.SaveCalls, Is.Zero);
		Assert.That(changes, Is.Zero);
	}

	[Test]
	public void SettingsSerializationExcludesRuntimeOwnershipAndTransientValues() {
		var source = new RuntimePreferences { ID = "record", Number = 25, HiddenColumn = "stable", RuntimeValue = "discard", Provider = new RecordingProvider() };
		source.OnChanged += () => throw new InvalidOperationException("Events must not be serialized.");
		var serializer = new SettingsObjectJsonSerializer();
		using var writer = new StringWriter();
		serializer.Serialize(writer, source);
		using var reader = new StringReader(writer.ToString());
		var restored = (RuntimePreferences)serializer.Deserialize(reader, typeof(RuntimePreferences));

		Assert.That(restored.ID, Is.EqualTo("record"));
		Assert.That(restored.Number, Is.EqualTo(25));
		Assert.That(restored.HiddenColumn, Is.EqualTo("stable"));
		Assert.That(restored.Provider, Is.Null);
		Assert.That(restored.RuntimeValue, Is.Null);
		Assert.That(writer.ToString(), Does.Not.Contain(nameof(RuntimePreferences.Callback)).And.Not.Contain(nameof(SettingsObject.OnChanged)));
		Assert.That(ApplicationHelper.IsSettingsMember(typeof(RuntimePreferences).GetProperty(nameof(RuntimePreferences.Callback))), Is.False);
		Assert.That(ApplicationHelper.IsSettingsMember(typeof(RuntimePreferences).GetProperty(nameof(RuntimePreferences.HiddenColumn))), Is.True);
	}

	[Test]
	public void ApplicationAndDecoratorExposeTheExistingUserSettingsProvider() {
		var provider = new RecordingProvider();
		using var services = new ServiceCollection().AddSingleton<ISettingsServices>(new TestSettingsServices(provider)).BuildServiceProvider();
		using var application = new SettingsApplication(services);
		var decorator = new SettingsApplicationDecorator(application);

		Assert.That(application.Settings, Is.SameAs(provider));
		Assert.That(decorator.Settings, Is.SameAs(provider));
	}

	#region Inner Types

	// Storage can be delayed or rejected without introducing another settings ownership model.
	private sealed class RecordingProvider : AsyncSettingsProviderBase {

		/// <summary>The accepted settings instance observed by persistence.</summary>
		public SettingsObject Saved { get; private set; }

		/// <summary>The number of attempted storage writes.</summary>
		public int SaveCalls { get; private set; }

		/// <summary>An optional asynchronous write barrier.</summary>
		public Func<Task> BeforeSave { get; init; }

		/// <summary>An optional storage failure raised before acceptance.</summary>
		public Exception SaveError { get; set; }

		/// <inheritdoc />
		public override bool EncryptSettings {
			get {
				return false;
			}
		}

		/// <inheritdoc />
		protected override Task<SettingsObject> LoadInternalAsync(Type settingsObjectType, object id, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(Saved);
		}

		/// <inheritdoc />
		protected override async Task SaveInternalAsync(SettingsObject settings, CancellationToken cancellationToken) {
			SaveCalls++;
			if (BeforeSave != null)
				await BeforeSave();
			cancellationToken.ThrowIfCancellationRequested();
			if (SaveError != null)
				throw SaveError;
			Saved = settings;
		}

		/// <inheritdoc />
		public override Task<bool> ContainsSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(Saved != null && Saved.GetType() == settingsObjectType && Equals(Saved.ID, id));
		}

		/// <inheritdoc />
		public override Task DeleteSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			Saved = null;
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public override Task ClearSettingsAsync(CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			Saved = null;
			return Task.CompletedTask;
		}
	}

	// Metadata discovery must never activate the operational screen.
	public class ScreenSurface : IApplicationScreen {

		// Fail immediately if metadata discovery constructs this screen.
		public ScreenSurface() {
			throw new InvalidOperationException("Metadata discovery must not construct a screen.");
		}

		/// <inheritdoc />
		public IApplicationScreenHost Host { get; set; }

		/// <inheritdoc />
		public IApplicationBlock Block { get; set; }

		/// <inheritdoc />
		public virtual SettingsObject ScreenSettings {
			get {
				throw new InvalidOperationException("Metadata discovery must not read a screen property.");
			}
		}
	}

	// The declared property identifies this screen's own nested settings schema.
	public sealed class DiscoveryScreen : ScreenSurface {

		/// <inheritdoc />
		public override Preferences ScreenSettings {
			get {
				throw new InvalidOperationException("Metadata discovery must not read a screen property.");
			}
		}

		/// <summary>The settings schema belongs to its screen and uses the existing default-value support.</summary>
		public sealed class Preferences : SettingsObject {

			/// <summary>A remembered value with its activation and Reset default.</summary>
			[DefaultValue(10)]
			public int Number { get; set; }
		}
	}

	// Existing attributes can declare a settings type without a covariant property.
	[UseSettings(typeof(DiscoveryScreen.Preferences))]
	public class AttributeScreen : ScreenSurface {
	}

	// New declarations use the same discovery path without an operational settings getter.
	[Settings(typeof(DiscoveryScreen.Preferences))]
	public class SettingsAttributeScreen : ScreenSurface {
	}

	// Derived screens inherit their base screen's attribute declaration.
	public sealed class InheritedSettingsAttributeScreen : SettingsAttributeScreen {
	}

	// Attribute and covariant-property declarations must agree on the same schema.
	public sealed class MatchingSettingsPropertyScreen : SettingsAttributeScreen {

		/// <inheritdoc />
		public override DiscoveryScreen.Preferences ScreenSettings {
			get {
				throw new InvalidOperationException("Metadata discovery must not read a screen property.");
			}
		}
	}

	// A derived property cannot silently override the attributed schema.
	public sealed class ConflictingSettingsPropertyScreen : SettingsAttributeScreen {

		/// <inheritdoc />
		public override OtherPreferences ScreenSettings {
			get {
				throw new InvalidOperationException("Metadata discovery must not read a screen property.");
			}
		}
	}

	// Both public attribute spellings can identify the same schema without ambiguity.
	[UseSettings(typeof(DiscoveryScreen.Preferences))]
	public sealed class MatchingLegacySettingsScreen : SettingsAttributeScreen {
	}

	// Conflicting old and new attribute spellings cannot hide one another.
	[UseSettings(typeof(OtherPreferences))]
	public sealed class ConflictingLegacySettingsScreen : SettingsAttributeScreen {
	}

	// An explicit null does not declare a usable settings type.
	[Settings(null)]
	public sealed class NullSettingsScreen : ScreenSurface {
	}

	// Types outside the settings hierarchy cannot act as settings roots.
	[Settings(typeof(string))]
	public sealed class NonSettingsScreen : ScreenSurface {
	}

	// An abstract settings schema requires an explicitly supplied compatible value.
	[Settings(typeof(AbstractSettingsScreen.Preferences))]
	public sealed class AbstractSettingsScreen : ScreenSurface {

		#region Inner Types

		/// <summary>An incomplete schema used to verify declaration validation.</summary>
		public abstract class Preferences : SettingsObject {
		}

		/// <summary>A concrete value supplied for this abstract schema through the existing builder.</summary>
		public sealed class SuppliedPreferences : Preferences {
		}

		#endregion
	}

	// The original attribute spelling retains the same abstract-schema contract.
	[UseSettings(typeof(AbstractSettingsScreen.Preferences))]
	public sealed class LegacyAbstractSettingsScreen : ScreenSurface {
	}

	// An open generic schema cannot supply a registration-owned value.
	[Settings(typeof(OpenGenericSettingsScreen.Preferences<>))]
	public sealed class OpenGenericSettingsScreen : ScreenSurface {

		#region Inner Types

		/// <summary>An unbound schema used to verify declaration validation.</summary>
		public sealed class Preferences<TValue> : SettingsObject {
		}

		#endregion
	}

	// Attribute-created settings retain the existing public parameterless-constructor requirement.
	[Settings(typeof(NoDefaultConstructorSettingsScreen.Preferences))]
	public sealed class NoDefaultConstructorSettingsScreen : ScreenSurface {

		#region Inner Types

		/// <summary>A schema that requires explicit construction values.</summary>
		public sealed class Preferences : SettingsObject {

			/// <summary>Requires a supplied initial value instead of inventing activation machinery.</summary>
			public Preferences(int number) {
				Number = number;
			}

			/// <summary>The required construction-time value.</summary>
			public int Number { get; }
		}

		#endregion
	}

	// Existing builder compatibility permits a derived value of the declared concrete schema.
	[Settings(typeof(PolymorphicSettingsScreen.Preferences))]
	public sealed class PolymorphicSettingsScreen : ScreenSurface {

		#region Inner Types

		/// <summary>A concrete base schema supporting compatible specialized values.</summary>
		public class Preferences : SettingsObject {

			/// <summary>A normal remembered value.</summary>
			public int Number { get; set; }
		}

		/// <summary>A compatible variant supplied through the existing builder overload.</summary>
		public sealed class VariantPreferences : Preferences {
		}

		#endregion
	}

	// An inherited attribute may agree with a screen's declared property type.
	public sealed class MatchingScreen : AttributeScreen {

		/// <summary>The same schema declared by the inherited attribute.</summary>
		public override DiscoveryScreen.Preferences ScreenSettings {
			get {
				throw new InvalidOperationException("Metadata discovery must not read a screen property.");
			}
		}
	}

	// Ambiguous settings declarations are rejected before control activation.
	public sealed class ConflictingScreen : AttributeScreen {

		/// <summary>An incompatible schema used to verify declaration rejection.</summary>
		public override OtherPreferences ScreenSettings {
			get {
				throw new InvalidOperationException("Metadata discovery must not read a screen property.");
			}
		}
	}

	// This fixture schema is independent of a screen and needs no custom defaults machinery.
	public sealed class OtherPreferences : SettingsObject {
	}

	// Serializable value metadata does not make runtime services, callbacks or transient state persistent.
	public sealed class RuntimePreferences : SettingsObject {

		/// <summary>A normal persisted value.</summary>
		public int Number { get; set; }

		/// <summary>Hidden value metadata remains persistent.</summary>
		[Browsable(false), ReadOnly(true)]
		public string HiddenColumn { get; set; }

		/// <summary>Transient values do not enter persistent settings.</summary>
		[Transient]
		public string RuntimeValue { get; set; }

		/// <summary>Callbacks never enter settings storage.</summary>
		public Action Callback { get; set; } = () => throw new InvalidOperationException("Runtime callbacks must not run.");
	}

	// Project the real provider through the existing settings-services contract.
	private sealed class TestSettingsServices(ISettingsProvider provider) : BaseSettingsServices {

		/// <inheritdoc />
		public override ISettingsProvider UserSettings { get; } = provider;

		/// <inheritdoc />
		public override ISettingsProvider SystemSettings { get; } = provider;
	}

	// The application consumes the configured settings services without a catalog.
	private sealed class SettingsApplication(IServiceProvider services) : AsyncApplicationBase(services) {

		/// <inheritdoc />
		public override IApplicationPlugin[] LoadedPlugins { get; } = Array.Empty<IApplicationPlugin>();

		/// <inheritdoc />
		public override IApplicationScreenHost ScreenHost {
			get {
				return null;
			}
		}
	}

	// Preserve the wrapped application's provider identity through decoration.
	private sealed class SettingsApplicationDecorator(SettingsApplication application) : ApplicationDecorator<SettingsApplication>(application);
	#endregion

}
