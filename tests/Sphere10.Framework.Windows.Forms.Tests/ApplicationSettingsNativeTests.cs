// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationSettingsNativeTests {
	private bool _startedFramework;
	private bool _hasRequiredDpiMode;

	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(true, true)]
	public void InitializingALaterRolePreservesAnotherLivePreferenceDraftAndItsNextCapture(bool anotherInstance, bool previousSaveFailed) {
		RunWithMessageLoop(async owner => {
			var directory = Tools.FileSystem.GetTempEmptyDirectory();
			using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
			ISettingsProvider legacy = new DirectoryFileSettingsProvider(directory);
			var storedRole = legacy.Get<VisualGridSettings>("LazyRoles.secondary");
			storedRole.PageMode = VisualGridPageMode.Paged;
			storedRole.PageSize = 29;
			storedRole.Save();
			var store = new ProbeStore();
			using var block = new WinFormsApplicationBlock { Name = "Lazy roles" };
			var defaults = new Preferences {
				Grids = new() { ["primary"] = new(), ["secondary"] = new() }
			};
			var definition = new WinFormsApplicationScreenDefinition(typeof(LazyRolesScreen), "Lazy", settings: defaults);
			block.AddScreenDefinition(definition);
			block.DefaultScreenDefinition = definition;
			var services = new ServiceCollection();
			services.AddSingleton(new Local<ISettingsProvider>(legacy));
			services.AddSingleton<IApplicationSettingsStore>(store);
			services.AddMainForm<QuietMainForm>();
			services.AddApplicationBlock(block);
			services.AddApplicationSettings("Native lazy roles");
			await using var provider = services.BuildServiceProvider();
			var app = provider.GetRequiredService<IWinFormsApplication>();
			await app.InitializeAsync();
			var main = (QuietMainForm)provider.GetRequiredService<IMainForm>();
			main.ScreenMode = ScreenMode.MultiView;
			main.Show(owner);
			var first = (LazyRolesScreen)main.ActiveScreen;
			await first.BindRoleAsync("primary");
			Assert.That(await main.ScreenHost.CloseScreenAsync(first), Is.True);
			Assert.That(store.Saves, Is.Zero, "Initializing an unchanged screen must not turn close into a preference save.");
			first = (LazyRolesScreen)await main.ScreenHost.OpenScreenAsync(block, definition);
			await first.BindRoleAsync("primary");
			first.EditName("Unaccepted live preference");
			if (previousSaveFailed) {
				store.Fail = true;
				await Assert.ThatAsync(async () => await first.SaveSettingsAsync(), Throws.TypeOf<InvalidOperationException>());
				store.Fail = false;
			}
			var later = anotherInstance ? (LazyRolesScreen)await main.ScreenHost.OpenScreenAsync(block, definition) : first;

			await later.BindRoleAsync("secondary");

			Assert.That(first.ReadName(), Is.EqualTo("Unaccepted live preference"), "A later section import must not replace unrelated working values.");
			var profile = app.Settings.GetProfile(block, definition.Registration);
			Assert.That(profile.LastSaveError != null, Is.EqualTo(previousSaveFailed), "Initialization must retain a previous failure until the user's retry succeeds.");
			await first.SaveSettingsAsync();
			Assert.That(profile.GetValues<Preferences>().Name, Is.EqualTo("Unaccepted live preference"), "Initializing another role must not supersede an existing producer's next accepted capture.");
			Assert.That(profile.GetValues<Preferences>().Grids["secondary"].PageSize, Is.EqualTo(29), "An earlier full-root draft must not discard the newly imported role.");
			Assert.That(store.Saves, Is.EqualTo(1));
			Assert.That(profile.LastSaveError, Is.Null);
			Assert.That(profile.SectionVersions, Does.ContainKey("Grids[secondary]"));
		});
	}

	[OneTimeSetUp]
	public void StartFramework() {
		_hasRequiredDpiMode = System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance.Build().Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void OptionsRetainsDetachedPageDraftsAndCancelDoesNotUndoAnEarlierApply() {
		RunWithMessageLoop(async owner => {
			var store = new ProbeStore();
			var catalog = new ApplicationSettingsCatalog("NativeOptions", store, Array.Empty<IApplicationBlock>(), new[] { Definition("one"), Definition("two") });
			await catalog.InitializeAsync();
			using var dialog = new WinFormsApplicationSettingsDialog(catalog) { ShowInTaskbar = false };
			dialog.Show(owner);
			var first = catalog.GetProfile("one");
			var second = catalog.GetProfile("two");
			SetName(dialog, "Accepted first");
			Assert.That(first.GetValues<Preferences>().Name, Is.EqualTo("Default"));
			Assert.That(store.Saves, Is.Zero);
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			dialog.SelectProfile(second);
			SetName(dialog, "Keep draft");
			dialog.SelectProfile(first);
			dialog.SelectProfile(second);
			Assert.That(GetName(dialog), Is.EqualTo("Keep draft"));
			dialog.ResetSelectedDefaults();
			Assert.That(GetName(dialog), Is.EqualTo("Default"));
			Assert.That(store.Saves, Is.EqualTo(1));
			dialog.CancelEdits();
			dialog.Close();
			Assert.That(first.GetValues<Preferences>().Name, Is.EqualTo("Accepted first"));
			Assert.That(second.GetValues<Preferences>().Name, Is.EqualTo("Default"));
			Assert.That(store.Saves, Is.EqualTo(1));
		});
	}

	[Test]
	public void InvalidAndFailedApplyLeaveTheDraftOpenAndRetryable() {
		RunWithMessageLoop(async owner => {
			var store = new ProbeStore();
			var catalog = new ApplicationSettingsCatalog("NativeValidation", store, Array.Empty<IApplicationBlock>(), new[] { Definition("one") });
			await catalog.InitializeAsync();
			using var dialog = new WinFormsApplicationSettingsDialog(catalog);
			dialog.Show(owner);
			SetName(dialog, string.Empty);
			Assert.That(await dialog.ApplySelectedAsync(), Is.False);
			Assert.That(store.Saves, Is.Zero);
			SetName(dialog, "Retry value");
			store.Fail = true;
			Assert.That(await dialog.ApplySelectedAsync(), Is.False);
			Assert.That(GetName(dialog), Is.EqualTo("Retry value"));
			Assert.That(catalog.Profiles.Single().GetValues<Preferences>().Name, Is.EqualTo("Default"));
			store.Fail = false;
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			Assert.That(catalog.Profiles.Single().GetValues<Preferences>().Name, Is.EqualTo("Retry value"));
			dialog.Close();
		});
	}

	[Test]
	public void DefaultEditorHidesStorageOwnershipOnlyForOptionsAndExpandsDeclaredGridRoles() {
		using var editor = new DefaultCrudEntityEditor {
			PropertyFilter = (owner, property) => owner is not SettingsObject
				|| property.Name is not (nameof(SettingsObject.ID) or nameof(SettingsObject.Provider))
		};
		var values = new Preferences();
		editor.SetEntity(DataSourceCapabilities.CanUpdate, values, false);
		var descriptor = Find<PropertyGrid>(editor).SelectedObject;
		var properties = TypeDescriptor.GetProperties(descriptor);
		Assert.That(properties["ID"], Is.Null);
		Assert.That(properties["Provider"], Is.Null);
		var roles = properties[nameof(Preferences.Grids)];
		Assert.That(roles.Converter.GetPropertiesSupported(), Is.True);
		Assert.That(roles.Converter.ConvertToString(roles.GetValue(descriptor)), Is.EqualTo("1 entry"));
		var role = roles.Converter.GetProperties(roles.GetValue(descriptor))["primary"];
		Assert.That(role, Is.Not.Null);
		var children = role.Converter.GetProperties(role.GetValue(null));
		Assert.That(role.Converter.ConvertToString(role.GetValue(null)), Is.EqualTo("Expand to edit"));
		Assert.That(children["ID"], Is.Null);
		Assert.That(children["Provider"], Is.Null);
		var columns = children[nameof(VisualGridSettings.Columns)];
		Assert.That(columns.Converter.ConvertToString(columns.GetValue(null)), Is.EqualTo("2 items"));
		var firstColumn = columns.Converter.GetProperties(columns.GetValue(null))["[0]"];
		Assert.That(firstColumn.DisplayName, Is.EqualTo("Process name"));
		Assert.That(firstColumn.Name, Is.EqualTo("[0]"), "Only its readable label changes, preserving existing property paths.");
		var width = firstColumn.Converter.GetProperties(firstColumn.GetValue(null))[nameof(ApplicationGridColumnPreferences.Width)];
		width.SetValue(null, 237D);
		Assert.That(values.Grids["primary"].Columns[0].Width, Is.EqualTo(237D));
		children[nameof(VisualGridSettings.PageSize)].SetValue(null, 47);
		Assert.That(values.Grids["primary"].PageSize, Is.EqualTo(47));
		Assert.That(editor.HasChanges, Is.True);
		using var ordinary = new DefaultCrudEntityEditor();
		ordinary.SetEntity(DataSourceCapabilities.CanUpdate, new Preferences(), false);
		Assert.That(TypeDescriptor.GetProperties(Find<PropertyGrid>(ordinary).SelectedObject)["ID"], Is.Not.Null);
	}

	[Test]
	public void PlainObjectOptionsKeepProductIdentifiersAndHideNestedGridPersistenceContext() {
		RunWithMessageLoop(async owner => {
			var catalog = new ApplicationSettingsCatalog("Plain grid", new ProbeStore(), Array.Empty<IApplicationBlock>(), applicationSettings: new[] {
				new ApplicationSettingsDefinition("grid", "Grid", new PlainGridPreferences())
			});
			await catalog.InitializeAsync();
			using var dialog = new WinFormsApplicationSettingsDialog(catalog);
			dialog.Show(owner);
			var descriptor = Find<PropertyGrid>(dialog).SelectedObject;
			var properties = TypeDescriptor.GetProperties(descriptor);
			Assert.That(properties[nameof(PlainGridPreferences.ID)], Is.Not.Null);
			Assert.That(properties[nameof(PlainGridPreferences.ID)].IsReadOnly, Is.True);
			Assert.That(TypeDescriptor.GetProperties(descriptor, new Attribute[] { BrowsableAttribute.Yes })[nameof(PlainGridPreferences.InternalValue)], Is.Null);
			var grid = properties[nameof(PlainGridPreferences.Grid)];
			var children = grid.Converter.GetProperties(grid.GetValue(descriptor));
			Assert.That(children[nameof(SettingsObject.ID)], Is.Null);
			Assert.That(children[nameof(SettingsObject.Provider)], Is.Null);
			Assert.That(children[nameof(VisualGridSettings.PageSize)], Is.Not.Null);
			children[nameof(VisualGridSettings.PageSize)].SetValue(null, 47);
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			var accepted = catalog.GetProfile("grid").GetValues<PlainGridPreferences>();
			Assert.That(accepted.ID, Is.EqualTo("Product identifier"));
			Assert.That(accepted.InternalValue, Is.EqualTo("Persisted internal state"));
			Assert.That(accepted.Grid.PageSize, Is.EqualTo(47));
			dialog.Close();
		});
	}

	[Test]
	public void NoOptionsAndCustomPagesDoNotActivateTheirOperationalScreen() {
		RunWithMessageLoop(async owner => {
			using var block = new WinFormsApplicationBlock { Name = "Metadata only" };
			var activated = 0;
			var editors = 0;
			var defaults = new PlainPreferences { Name = "Custom default" };
			var definition = new WinFormsApplicationScreenDefinition(typeof(ProbeScreen), "Custom", stableKey: "custom",
				activator: _ => {
					activated++;
					return new ProbeScreen();
				}, settings: defaults,
				settingsEditor: context => {
					editors++;
					Assert.That(context.Settings, Is.TypeOf<PlainPreferences>().And.Not.SameAs(defaults));
					Assert.That(((PlainPreferences)context.Settings).Name, Is.EqualTo("Custom default"));
					Assert.That(context.Owner, Is.InstanceOf<WinFormsApplicationSettingsDialog>());
					return new DefaultCrudEntityEditor();
				});
			block.AddScreenDefinition(definition);
			var none = new WinFormsApplicationScreenDefinition(typeof(ProbeScreen), "No options", stableKey: "none");
			block.AddScreenDefinition(none);
			var catalog = new ApplicationSettingsCatalog("CustomEditor", new ProbeStore(), new[] { block });
			await catalog.InitializeAsync();
			using var dialog = new WinFormsApplicationSettingsDialog(catalog);
			dialog.Show(owner);
			dialog.SelectProfile(catalog.GetProfile(block, none.Registration));
			Assert.That(dialog.SelectedProfile.HasOptions, Is.False);
			Assert.That(FindAll<DefaultCrudEntityEditor>(dialog), Is.Empty);
			Assert.That(editors, Is.EqualTo(1));
			Assert.That(activated, Is.Zero);
			dialog.Close();
		});
	}


	// Run this fixture in a fresh host so its DPI mode is established before any native HWND.
	[TestCase(9F, 96)]
	[TestCase(14F, 96)]
	[TestCase(9F, 144)]
	public void NativeOptionsKeepsAllButtonsVisibleWithLargeFontsAndActualMonitorDpi(float fontSize, int dpi) {
		if (!_hasRequiredDpiMode)
			Assert.Ignore("Run ApplicationSettingsNativeTests in a fresh testhost: another fixture already created a native HWND before PerMonitorV2 initialization.");
		RunWithMessageLoop(async owner => {
			var catalog = new ApplicationSettingsCatalog("Native presentation", new ProbeStore(), Array.Empty<IApplicationBlock>(), new[] { Definition("Preferences") });
			await catalog.InitializeAsync();
			using var awareness = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
			using var font = new Font("Segoe UI", fontSize);
			using var dialog = new KeyboardOptionsDialog(catalog) { Font = font, StartPosition = FormStartPosition.Manual, Location = new Point(40, 40) };
			dialog.Show(owner);
			foreach (var monitor in Screen.AllScreens) {
				dialog.Location = new Point(monitor.WorkingArea.Left + 24, monitor.WorkingArea.Top + 24);
				System.Windows.Forms.Application.DoEvents();
				if (dialog.DeviceDpi == dpi)
					break;
			}
			if (dialog.DeviceDpi != dpi)
				Assert.Ignore($"No available monitor renders the owned native dialog at {dpi} DPI.");
			Assert.That(WinAPI.USER32.GetDpiForWindow(dialog.Handle), Is.EqualTo((uint)dpi), "The native HWND and managed control must report the same actual monitor DPI.");

			dialog.BringToFront();
			dialog.Refresh();
			System.Windows.Forms.Application.DoEvents();
			Assert.That(Screen.FromControl(dialog).WorkingArea.Contains(dialog.Bounds), Is.True, "The complete native dialog must remain within its monitor's work area.");
			var grid = Find<PropertyGrid>(dialog);
			grid.ExpandAllGridItems();
			var buttons = FindAll<Button>(dialog).Where(button => button.Parent is FlowLayoutPanel).ToArray();
			Assert.That(buttons, Has.Length.EqualTo(4));
			foreach (var button in buttons) {
				var bounds = dialog.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
				Assert.That(button.Visible && button.Enabled, Is.True, button.Text);
				Assert.That(dialog.ClientRectangle.Contains(bounds), Is.True, $"{button.Text} {bounds} must fit in {dialog.ClientRectangle}.");
				Assert.That(button.ClientSize.Width, Is.GreaterThanOrEqualTo(TextRenderer.MeasureText(button.Text.Replace("&", string.Empty), button.Font).Width));
			}
			Assert.That(grid.ClientSize.Width, Is.GreaterThan(200));
			Assert.That(grid.ClientSize.Height, Is.GreaterThan(120));
			var file = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"native-options-{fontSize:0}pt-{dpi}dpi.png");
			using var bitmap = new Bitmap(dialog.Width, dialog.Height);
			// Render this owned HWND only, so an unrelated foreground window cannot enter the visual evidence.
			dialog.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
			bitmap.Save(file, ImageFormat.Png);
			TestContext.AddTestAttachment(file, "Owned native Options HWND render, independent of desktop occlusion");
			TestContext.Progress.WriteLine($"Native Options: DeviceDpi={dialog.DeviceDpi}, font={dialog.Font}, client={dialog.ClientSize}, image={file}");
			var navigation = Find<TreeView>(dialog);
			navigation.Focus();
			Assert.That(dialog.DispatchKey(Keys.Tab), Is.True);
			Assert.That(navigation.ContainsFocus, Is.False, "Tab must enter the selected page's real editor.");
			Assert.That(dialog.DispatchKey(Keys.Escape), Is.True);
			Assert.That(dialog.DialogResult, Is.EqualTo(DialogResult.Cancel));
		});
	}

	[Test]
	public void KeyboardOkAppliesOnlyTheSelectedPageAndKeepsOtherDraftsVisibleUntilResolved() {
		RunWithMessageLoop(async owner => {
			var store = new ProbeStore();
			var catalog = new ApplicationSettingsCatalog("Keyboard Options", store, Array.Empty<IApplicationBlock>(), new[] { Definition("one"), Definition("two") });
			await catalog.InitializeAsync();
			using var dialog = new KeyboardOptionsDialog(catalog);
			dialog.Show(owner);
			SetName(dialog, "Other draft");
			dialog.SelectProfile(catalog.GetProfile("two"));
			SetName(dialog, "Selected saved");
			Assert.That(dialog.DispatchKey(Keys.Enter), Is.True);
			Assert.That(dialog.Visible, Is.True, "An unresolved other-page draft must remain available after selected-page OK.");
			Assert.That(store.Saves, Is.EqualTo(1));
			Assert.That(catalog.GetProfile("one").GetValues<Preferences>().Name, Is.EqualTo("Default"));
			Assert.That(catalog.GetProfile("two").GetValues<Preferences>().Name, Is.EqualTo("Selected saved"));
			Assert.That(dialog.DispatchKey(Keys.Escape), Is.True);
			Assert.That(catalog.GetProfile("one").GetValues<Preferences>().Name, Is.EqualTo("Default"));
		});
	}

	[Test]
	public void AcceptedNativeCloseWaitsAfterAllGuardsAndRetainsTheScreenOnSaveFailure() {
		RunWithMessageLoop(async owner => {
			var services = new ServiceCollection();
			services.AddSingleton<IApplicationSettingsStore>(new ProbeStore());
			services.AddMainForm<QuietMainForm>();
			using var block = new WinFormsApplicationBlock { Name = "Close settings" };
			var definition = new WinFormsApplicationScreenDefinition(typeof(ProbeScreen), "Probe", settings: new Preferences());
			block.AddScreenDefinition(definition);
			block.DefaultScreenDefinition = definition;
			services.AddApplicationBlock(block);
			services.AddApplicationSettings("NativeClose");
			await using var provider = services.BuildServiceProvider();
			var application = provider.GetRequiredService<IWinFormsApplication>();
			await application.InitializeAsync();
			var main = (QuietMainForm)provider.GetRequiredService<IMainForm>();
			main.Show(owner);
			var screen = (ProbeScreen)main.ActiveScreen;
			Assert.That(screen.InitialName, Is.EqualTo("Default"));
			screen.Veto = true;
			Assert.That(await main.ScreenHost.CloseScreenAsync(screen), Is.False);
			Assert.That(screen.SaveCount, Is.Zero);
			screen.Veto = false;
			screen.FailSave = true;
			await Assert.ThatAsync(async () => await main.ScreenHost.CloseScreenAsync(screen), Throws.TypeOf<InvalidOperationException>());
			Assert.That(main.ScreenHost.Screens, Does.Contain(screen));
			Assert.That(screen.IsDisposed, Is.False);
			screen.FailSave = false;
			screen.Pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var closing = main.ScreenHost.CloseScreenAsync(screen);
			Assert.That(closing.IsCompleted, Is.False);
			Assert.That(screen.IsDisposed, Is.False);
			screen.Pending.SetResult();
			Assert.That(await closing, Is.True);
			Assert.That(screen.IsDisposed, Is.True);
		});
	}

	[Test]
	public void SharedRegistrationAppliesForeignOptionsAndAnUnchangedOldInstanceCannotOverwriteThemOnClose() {
		RunWithMessageLoop(async owner => {
			var store = new ProbeStore();
			using var block = new WinFormsApplicationBlock { Name = "Shared profile" };
			var definition = new WinFormsApplicationScreenDefinition(typeof(ProbeScreen), "Shared", settings: new Preferences());
			block.AddScreenDefinition(definition);
			block.DefaultScreenDefinition = definition;
			var services = new ServiceCollection();
			services.AddSingleton<IApplicationSettingsStore>(store);
			services.AddMainForm<QuietMainForm>();
			services.AddApplicationBlock(block);
			services.AddApplicationSettings("Native shared capture");
			await using var provider = services.BuildServiceProvider();
			var app = provider.GetRequiredService<IWinFormsApplication>();
			await app.InitializeAsync();
			var main = (QuietMainForm)provider.GetRequiredService<IMainForm>();
			main.ScreenMode = ScreenMode.MultiView;
			main.Show(owner);
			await Task.Yield();
			Assert.That(main.ReportedError, Is.Null, main.ReportedError?.ToString());
			var first = (ProbeScreen)main.ActiveScreen;
			var second = (ProbeScreen)await main.ScreenHost.OpenScreenAsync(block, definition);
			Assert.That(second, Is.Not.SameAs(first));
			first.EditName("From live instance");
			await first.SaveSettingsAsync();
			System.Windows.Forms.Application.DoEvents();
			Assert.That(main.ReportedError, Is.Null, main.ReportedError?.ToString());
			Assert.That(second.ReadName(), Is.EqualTo("From live instance"));
			first.EditName("Stale local draft");
			var profile = app.Settings.GetProfile(block, definition.Registration);
			var options = profile.BeginEdit();
			((Preferences)options.Draft).Name = "Explicit Options wins";
			Assert.That((await options.ApplyAsync()).IsSuccess, Is.True);
			await main.ScreenHost.CloseScreenAsync(first);
			await main.ScreenHost.CloseScreenAsync(second);
			Assert.That(profile.GetValues<Preferences>().Name, Is.EqualTo("Explicit Options wins"));
			Assert.That(store.Saves, Is.EqualTo(2), "Neither unchanged nor superseded close may write an older snapshot over Options.");
		});
	}

	[Test]
	public void OrderedOwnCapturesRetainANewerPendingEditWhenTheOlderWritePublishes() {
		RunWithMessageLoop(async owner => {
			var store = new ProbeStore { WriteGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
			using var block = new WinFormsApplicationBlock { Name = "Ordered capture" };
			var definition = new WinFormsApplicationScreenDefinition(typeof(ProbeScreen), "Ordered", settings: new Preferences());
			block.AddScreenDefinition(definition);
			block.DefaultScreenDefinition = definition;
			var services = new ServiceCollection();
			services.AddSingleton<IApplicationSettingsStore>(store);
			services.AddMainForm<QuietMainForm>();
			services.AddApplicationBlock(block);
			services.AddApplicationSettings("Native ordered captures");
			await using var provider = services.BuildServiceProvider();
			var app = provider.GetRequiredService<IWinFormsApplication>();
			await app.InitializeAsync();
			var main = (QuietMainForm)provider.GetRequiredService<IMainForm>();
			main.Show(owner);
			var screen = (ProbeScreen)main.ActiveScreen;
			screen.EditName("Older capture");
			var older = screen.SaveSettingsAsync();
			screen.EditName("Newer capture");
			var newer = screen.SaveSettingsAsync();
			Assert.That(store.Saves, Is.Zero);
			store.WriteGate.SetResult();
			await Task.WhenAll(older, newer);
			System.Windows.Forms.Application.DoEvents();
			Assert.That(screen.ReadName(), Is.EqualTo("Newer capture"));
			Assert.That(app.Settings.GetProfile(block, definition.Registration).GetValues<Preferences>().Name, Is.EqualTo("Newer capture"));
			Assert.That(store.Saves, Is.EqualTo(2));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void CorruptLegacySectionsKeepDefaultsAndFirstQueryUsableWithoutRestoreWritesOrRepeatedImports(bool logging) {
		RunWithMessageLoop(async owner => {
			var directory = Tools.FileSystem.GetTempEmptyDirectory();
			using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
			ISettingsProvider seed = new DirectoryFileSettingsProvider(directory);
			if (logging) {
				var old = seed.Get<LogOutputSelectorSettings>(BoundScreen.LegacyKey);
				old.HasConfiguration = true;
				old.Configuration = new LogOutputConfiguration { Destination = LogOutputDestination.Application, RememberAfterClose = true };
				old.Save();
			} else {
				var old = seed.Get<VisualGridSettings>(BoundScreen.LegacyKey);
				old.PageMode = VisualGridPageMode.Paged;
				old.PageSize = 23;
				old.Save();
			}
			var legacyFile = Directory.GetFiles(directory).Single();
			File.WriteAllText(legacyFile, "{ corrupt optional preferences");
			var reads = new LegacyProvider(directory);
			ISettingsProvider native = new CachedSettingsProvider(reads);
			await using (var first = CreateBoundProvider(logging, new WinFormsApplicationSettingsStore(native))) {
				var app = first.GetRequiredService<IWinFormsApplication>();
				await app.InitializeAsync();
				var main = (QuietMainForm)first.GetRequiredService<IMainForm>();
				main.Show(owner);
				var screen = (BoundScreen)main.ActiveScreen;
				await screen.Ready.Task;
				Assert.That(screen.LastSettingsLoadError, Is.Not.Null);
				Assert.That(screen.LoggingStarts, Is.Zero, "Loading optional values cannot open logging output.");
				if (!logging)
					Assert.That(screen.Source.FirstPageSize, Is.EqualTo(7), "A corrupt old record must not prevent the first query using declared defaults.");
				Assert.That(reads.LegacyReads, Is.EqualTo(1));
				Assert.That(reads.Writes, Is.Zero, "Restoration and migration presence remain in memory until an accepted edit.");
				Assert.That(File.ReadAllText(legacyFile), Is.EqualTo("{ corrupt optional preferences"));
				Assert.That(await main.ScreenHost.CloseScreenAsync(screen), Is.True);
				Assert.That(reads.Writes, Is.Zero, "Closing an unchanged restored screen does not save preferences.");
				var block = app.Blocks.Single();
				screen = (BoundScreen)await main.ScreenHost.OpenScreenAsync(block, block.ScreenDefinitions.Single());
				await screen.Ready.Task;
				screen.EditName("Accepted preference edit");
				Assert.That(await main.ScreenHost.CloseScreenAsync(screen), Is.True);
				Assert.That(reads.Writes, Is.EqualTo(1));
			}
			var freshFiles = new LegacyProvider(directory);
			ISettingsProvider fresh = new CachedSettingsProvider(freshFiles);
			await using var second = CreateBoundProvider(logging, new WinFormsApplicationSettingsStore(fresh));
			var reopened = second.GetRequiredService<IWinFormsApplication>();
			await reopened.InitializeAsync();
			var reopenedMain = (QuietMainForm)second.GetRequiredService<IMainForm>();
			reopenedMain.Show(owner);
			var reopenedScreen = (BoundScreen)reopenedMain.ActiveScreen;
			await reopenedScreen.Ready.Task;
			Assert.That(reopenedScreen.LastSettingsLoadError, Is.Null);
			Assert.That(freshFiles.LegacyReads, Is.Zero, "A persisted section marker prevents repeatedly importing the corrupt legacy file.");
			Assert.That(freshFiles.Writes, Is.Zero);
		});
	}

	[Test]
	public void NativeLegacyOwnerIsCapturedBeforeALaterStaticProviderReplacement() {
		RunWithMessageLoop(async owner => {
			var directory = Tools.FileSystem.GetTempEmptyDirectory();
			using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
			ISettingsProvider seed = new DirectoryFileSettingsProvider(directory);
			var old = seed.Get<VisualGridSettings>(BoundScreen.LegacyKey);
			old.PageMode = VisualGridPageMode.Paged;
			old.PageSize = 23;
			old.Save();
			var originalFiles = new LegacyProvider(directory);
			ISettingsProvider original = new CachedSettingsProvider(originalFiles);
			var replacement = new ProhibitedSettingsProvider();
			UserSettings.TryGetProvider(out var previous);
			using var restore = Tools.Scope.ExecuteOnDispose(() => UserSettings.Provider = previous);
			await using var provider = CreateBoundProvider(false, new DelegatingStore(new WinFormsApplicationSettingsStore(original)), original);
			var app = provider.GetRequiredService<IWinFormsApplication>();
			await app.InitializeAsync();
			UserSettings.Provider = replacement;
			var main = (QuietMainForm)provider.GetRequiredService<IMainForm>();
			main.Show(owner);
			var screen = (BoundScreen)main.ActiveScreen;
			await screen.Ready.Task;
			Assert.That(screen.Source.FirstPageSize, Is.EqualTo(23));
			Assert.That(originalFiles.LegacyReads, Is.EqualTo(1));
			Assert.That(originalFiles.Writes, Is.Zero);
		});
	}

	[Test]
	public void CustomStoreWithoutANativeProviderUsesDeclaredDefaultsAndRuntimeNoOptionsBlocksStillOpen() {
		RunWithMessageLoop(async owner => {
			UserSettings.TryGetProvider(out var previous);
			using var restore = Tools.Scope.ExecuteOnDispose(() => UserSettings.Provider = previous);
			UserSettings.Provider = null;
			await using var provider = CreateBoundProvider(false, new ProbeStore());
			var app = provider.GetRequiredService<IWinFormsApplication>();
			await app.InitializeAsync();
			var main = (QuietMainForm)provider.GetRequiredService<IMainForm>();
			main.Show(owner);
			await ((BoundScreen)main.ActiveScreen).Ready.Task;
			Assert.That(((BoundScreen)main.ActiveScreen).Source.FirstPageSize, Is.EqualTo(7));
			using var late = new WinFormsApplicationBlock { Name = "Late no-options block" };
			var definition = new WinFormsApplicationScreenDefinition(typeof(PlainScreen), "Late screen");
			late.AddScreenDefinition(definition);
			main.RegisterBlock(late);
			var opened = await main.ScreenHost.OpenScreenAsync(late, definition);
			Assert.That(opened, Is.InstanceOf<PlainScreen>());
		});
	}

	// Use normal DI construction; custom profile stores borrow a separately captured native owner only for optional migration.
	private static ServiceProvider CreateBoundProvider(bool logging, IApplicationSettingsStore store, ISettingsProvider legacy = null) {
		var services = new ServiceCollection();
		services.AddSingleton(store);
		if (legacy != null)
			services.AddSingleton(new Local<ISettingsProvider>(legacy));
		services.AddMainForm<QuietMainForm>();
		services.AddSingleton<IWinFormsApplicationBlock>(_ => {
			var block = new WinFormsApplicationBlock { Name = "Bound sections" };
			var defaults = new Preferences {
				Grids = new Dictionary<string, VisualGridSettings> { ["primary"] = new VisualGridSettings {
					PageMode = VisualGridPageMode.Paged, PageSize = 7,
					Columns = new[] { new ApplicationGridColumnPreferences { Key = "name", Title = "Name" } }
				} }
			};
			var definition = new WinFormsApplicationScreenDefinition(typeof(BoundScreen), "Bound screen", stableKey: "bound",
				activator: _ => new BoundScreen(logging), settings: defaults);
			block.AddScreenDefinition(definition);
			block.DefaultScreenDefinition = definition;
			return block;
		});
		services.AddApplicationSettings("Native bound preferences");
		return services.BuildServiceProvider();
	}

	private static ApplicationSettingsDefinition Definition(string key) {
		return new ApplicationSettingsDefinition(key, key, new Preferences());
	}

	private static PropertyDescriptor NameProperty(WinFormsApplicationSettingsDialog dialog, out object descriptor) {
		descriptor = Find<PropertyGrid>(dialog).SelectedObject;
		return TypeDescriptor.GetProperties(descriptor)[nameof(Preferences.Name)];
	}

	private static void SetName(WinFormsApplicationSettingsDialog dialog, string value) {
		var property = NameProperty(dialog, out var descriptor);
		property.SetValue(descriptor, value);
	}

	private static string GetName(WinFormsApplicationSettingsDialog dialog) {
		var property = NameProperty(dialog, out var descriptor);
		return (string)property.GetValue(descriptor);
	}

	private static IEnumerable<T> FindAll<T>(Control owner) where T : Control {
		foreach (Control child in owner.Controls) {
			if (child is T match)
				yield return match;
			foreach (var nested in FindAll<T>(child))
				yield return nested;
		}
	}

	private static T Find<T>(Control owner) where T : Control => FindAll<T>(owner).Single();

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 30000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("Native settings test timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
			} catch (Exception error) {
				failure = error;
			} finally {
				owner.Close();
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		if (failure != null)
			ExceptionDispatchInfo.Capture(failure).Throw();
	}

	private sealed class KeyboardOptionsDialog(ApplicationSettingsCatalog catalog) : WinFormsApplicationSettingsDialog(catalog) {
		public bool DispatchKey(Keys key) => base.ProcessDialogKey(key);
	}

	public class PlainPreferences {
		public string Name { get; set; }
	}

	[UserEditable]
	public class PlainGridPreferences {
		[ReadOnly(true)]
		public string ID { get; set; } = "Product identifier";

		[Browsable(false)]
		public string InternalValue { get; set; } = "Persisted internal state";

		public VisualGridSettings Grid { get; set; } = new();
	}

	[UserEditable]
	public class Preferences : SettingsObject {
		public string Name { get; set; } = "Default";

		public Dictionary<string, VisualGridSettings> Grids { get; set; } = new() { ["primary"] = new VisualGridSettings {
			Columns = new[] { new ApplicationGridColumnPreferences { Key = "name", Title = "Process name" }, new ApplicationGridColumnPreferences { Key = "cpu", Title = "CPU usage" } }
		} };

		public LogOutputSelectorPreferences LogOutput { get; set; } = new();

		public override Result Validate() {
			var result = string.IsNullOrWhiteSpace(Name) ? Result.Error("A name is required.") : new Result();
			foreach (var preferences in Grids.Values)
				result.Merge(preferences.Validate());
			result.Merge(LogOutput.Validate());
			return result;
		}
	}

	public class QuietMainForm : BlockMainForm {
		public QuietMainForm() {
			StartPosition = FormStartPosition.Manual;
			Location = new Point(-20000, -20000);
		}

		public Exception ReportedError { get; private set; }

		protected override void OnLoad(EventArgs e) { }

		protected override Task<bool> ConfirmExitAsync() => Task.FromResult(true);

		public override void ReportError(Exception error) {
			ReportedError = error;
			TestContext.Progress.WriteLine("native owned error: " + error);
		}
	}

	public class ProbeScreen : WinFormsApplicationScreen {
		public string InitialName { get; private set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool Veto { get; set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool FailSave { get; set; }

		public int SaveCount { get; private set; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TaskCompletionSource Pending { get; set; }

		protected override void OnLoad(EventArgs e) {
			InitialName = GetSettings<Preferences>().Name;
			base.OnLoad(e);
		}

		public string ReadName() => GetSettings<Preferences>().Name;

		public void EditName(string name) {
			GetSettings<Preferences>().Name = name;
			MarkSettingsChanged();
		}

		protected override void OnHide(ref bool cancelHide) => cancelHide = Veto;

		public override async Task SaveSettingsAsync(CancellationToken cancellationToken = default) {
			SaveCount++;
			if (FailSave)
				throw new InvalidOperationException("Failed preference store.");
			if (Pending != null)
				await Pending.Task;
			await base.SaveSettingsAsync(cancellationToken);
		}
	}

	public class PlainScreen : WinFormsApplicationScreen { }

	public class LazyRolesScreen : WinFormsApplicationScreen {
		public async Task BindRoleAsync(string role) {
			var grid = new VisualGrid { SettingsKey = $"LazyRoles.{role}", Dock = DockStyle.Top, Height = 100 };
			grid.GridBindings = new[] { new VisualGridColumn<PreferenceRow> {
				PreferenceKey = "name", ColumnName = "Name", PropertyName = nameof(PreferenceRow.Name),
				DataType = typeof(string), PropertyValue = row => row.Name
			} };
			var binding = BindGridPreferencesAsync<Preferences>(grid, $"Grids[{role}]", values => values.Grids[role], (values, value) => values.Grids[role] = value);
			Controls.Add(grid);
			await binding;
		}

		public string ReadName() {
			return GetSettings<Preferences>().Name;
		}

		public void EditName(string name) {
			GetSettings<Preferences>().Name = name;
			MarkSettingsChanged();
		}
	}

	public class BoundScreen : WinFormsApplicationScreen {
		public const string LegacyKey = "BoundScreen.Legacy";

		private readonly bool _logging;

		public BoundScreen(bool logging) {
			_logging = logging;
			if (logging) {
				LogOutput = new LogOutputSelectorControl { SettingsKey = LegacyKey, Dock = DockStyle.Fill };
				LogOutput.LoggingStarting += _ => LoggingStarts++;
				Controls.Add(LogOutput);
			} else {
				Grid = new VisualGrid { SettingsKey = LegacyKey, Dock = DockStyle.Fill };
				Grid.GridBindings = new[] { new VisualGridColumn<PreferenceRow> {
					PreferenceKey = "name", ColumnName = "Name", PropertyName = nameof(PreferenceRow.Name), SortName = nameof(PreferenceRow.Name),
					DataType = typeof(string), PropertyValue = row => row.Name, ExpandsToFit = true
				} };
				Controls.Add(Grid);
			}
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public VisualGrid Grid { get; }

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public LogOutputSelectorControl LogOutput { get; }

		public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public PreferenceSource Source { get; } = new();

		public int LoggingStarts { get; private set; }

		public void EditName(string name) {
			GetSettings<Preferences>().Name = name;
			MarkSettingsChanged();
		}

		protected override async void OnLoad(EventArgs e) {
			base.OnLoad(e);
			try {
				if (_logging)
					await BindLogOutputPreferencesAsync<Preferences>(LogOutput, nameof(Preferences.LogOutput), values => values.LogOutput, (values, preferences) => values.LogOutput = preferences);
				else {
					await BindGridPreferencesAsync<Preferences>(Grid, "Grids[primary]", values => values.Grids["primary"], (values, preferences) => values.Grids["primary"] = preferences);
					await Grid.SetDataSource(Source);
					await Grid.RefreshGrid();
				}
				Ready.TrySetResult();
			} catch (Exception error) {
				Ready.TrySetException(error);
			}
		}
	}

	public class PreferenceRow {
		public string Name { get; set; }
	}

	public class PreferenceSource() : ListDataSource<PreferenceRow>(new ExtendedList<PreferenceRow>()) {
		public int FirstPageSize { get; private set; }

		public override Task<DataSourceItems<PreferenceRow>> ReadRangeAsync(string search, int length, int page, string sort, SortDirection direction) {
			if (FirstPageSize == 0)
				FirstPageSize = length;
			return Task.FromResult(new DataSourceItems<PreferenceRow> {
				Items = new[] { new PreferenceRow { Name = "First row" } }, Page = 0, TotalCount = 1
			});
		}
	}

	private sealed class LegacyProvider(string directory) : DirectoryFileSettingsProvider(directory) {
		public int LegacyReads { get; private set; }

		public int Writes { get; private set; }

		protected override SettingsObject LoadInternal(Type type, object id) {
			if (type == typeof(VisualGridSettings) || type == typeof(LogOutputSelectorSettings))
				LegacyReads++;
			return base.LoadInternal(type, id);
		}

		protected override void SaveInternal(SettingsObject values) {
			Writes++;
			base.SaveInternal(values);
		}
	}

	private sealed class DelegatingStore(IApplicationSettingsStore inner) : IApplicationSettingsStore {
		public Task<ApplicationSettingsStoredProfile> LoadAsync(string key, object defaults, bool hasExplicitStorageId = false, object storageId = null, CancellationToken token = default) {
			return inner.LoadAsync(key, defaults, hasExplicitStorageId, storageId, token);
		}

		public Task SaveAsync(string key, ApplicationSettingsStoredProfile values, bool hasExplicitStorageId = false, object storageId = null, CancellationToken token = default) {
			return inner.SaveAsync(key, values, hasExplicitStorageId, storageId, token);
		}
	}

	private sealed class ProbeStore : IApplicationSettingsStore {
		public bool Fail { get; set; }

		public int Saves { get; private set; }

		public TaskCompletionSource WriteGate { get; set; }

		public Task<ApplicationSettingsStoredProfile> LoadAsync(string key, object defaults, bool hasExplicitStorageId = false, object storageId = null, CancellationToken token = default) {
			return Task.FromResult<ApplicationSettingsStoredProfile>(null);
		}

		public async Task SaveAsync(string key, ApplicationSettingsStoredProfile profile, bool hasExplicitStorageId = false, object storageId = null, CancellationToken token = default) {
			if (WriteGate != null)
				await WriteGate.Task.WaitAsync(token);
			if (Fail)
				throw new InvalidOperationException("Failed preference store.");
			Saves++;
		}
	}
}
