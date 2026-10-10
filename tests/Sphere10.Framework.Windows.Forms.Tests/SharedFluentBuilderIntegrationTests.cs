// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class SharedFluentBuilderIntegrationTests {
	[Test]
	public void NativePluginUsesSharedNestedConfigurationWithPlatformExtensions() {
		var calls = 0;
		var plugin = new WinFormsApplicationPluginBuilder()
			.WithName("Operations")
			.ConfigureBlock(block => block
				.WithName("Inventory")
				.ShowInMenuStrip()
				.WithDefaultScreen<Screen>(activationMode: ScreenActivationMode.SingleInstance)
				.ConfigureMenu(menu => menu
					.WithText("Items")
					.ShowInMenuStrip()
					.ConfigureItem(item => item
						.AsScreenItem()
						.WithText("Stock")
						.WithScreen<Screen>()
						.AsSingleInstance()
						.WithTitle("Stock levels")
					)
					.AddActionItem("Adjust", () => calls++)
				)
			)
			.Build();
		using var block = plugin.Blocks[0];
		var menu = block.Menus[0];
		var screen = (IWinFormsScreenMenuItem)menu.Items[0];
		((WinFormsActionMenuItem)menu.Items[1]).OnSelect();
		Assert.That(screen.ScreenDefinition.Title, Is.EqualTo("Stock levels"));
		Assert.That(screen.ScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(menu.Parent, Is.SameAs(block));
		Assert.That(menu.Items[0].Parent, Is.SameAs(menu));
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public void SharedMetadataChangesPreserveUnrelatedLiveNativeEdits() {
		var builder = new WinFormsApplicationBlockBuilder()
			.WithName("Original")
			.ConfigureMenu(menu => menu
				.WithText("First")
			);
		using var block = builder.Build();
		block.Name = "Edited directly";
		builder
			.WithPosition(7)
			.WithDefaultScreen<Screen>("Default")
			.ConfigureMenu(menu => menu
				.WithText("Second")
			);

		Assert.That(builder.Build(), Is.SameAs(block));
		Assert.That(block.Name, Is.EqualTo("Edited directly"));
		Assert.That(block.Position, Is.EqualTo(7));
		Assert.That(block.DefaultScreenDefinition.Title, Is.EqualTo("Default"));
		Assert.That(block.Menus.Length, Is.EqualTo(2));
		Assert.That(block.Menus[1].Parent, Is.SameAs(block));
	}

	[Test]
	public void NativeCachedBlockBuildRetainsNewCanonicalDefinitions() {
		var builder = new WinFormsApplicationBlockBuilder().WithName("Workspace");
		using var block = builder.Build();
		var definition = new ApplicationScreenDefinition(typeof(Screen), "Direct editor");
		builder.AddScreenDefinition(definition);
		Assert.That(builder.Build(), Is.SameAs(block));
		Assert.That(block.ScreenDefinitions, Has.Length.EqualTo(1));
		Assert.That(block.ScreenDefinitions[0], Is.SameAs(definition));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void NativeCachedTypeDefaultsResolveToTheirMatchingMenuDefinition(bool defineDefaultBeforeFirstBuild) {
		var builder = new WinFormsApplicationBlockBuilder().WithName("Workspace");
		if (defineDefaultBeforeFirstBuild)
			builder.WithDefaultScreen<Screen>();
		using var block = builder.Build();
		block.Name = "Edited workspace";
		if (!defineDefaultBeforeFirstBuild)
			builder.WithDefaultScreen<Screen>();
		builder.ConfigureMenu(menu => menu.WithText("Editors").ConfigureItem(item => item
			.AsScreenItem()
			.WithText("Open editor")
			.WithScreen<Screen>()
			.WithTitle("Site editor")
			.AsSingleInstance()
		));
		Assert.That(builder.Build(), Is.SameAs(block));
		var definition = ((IScreenMenuItem)block.Menus[0].Items[0]).ScreenDefinition;
		Assert.That(block.DefaultScreenDefinition, Is.SameAs(definition));
		Assert.That(block.DefaultScreenDefinition.Title, Is.EqualTo("Site editor"));
		Assert.That(block.DefaultScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(block.Name, Is.EqualTo("Edited workspace"));
	}

	[Test]
	public void NativeCachedTypeDefaultRetiresItsProvisionalNormalDefinitionWhenMenuDeclaresEmpty() {
		var builder = new WinFormsApplicationBlockBuilder().WithName("Workspace").WithDefaultScreen<Screen>();
		using var block = builder.Build();
		var provisional = block.DefaultScreenDefinition;
		block.Name = "Edited workspace";
		builder.ConfigureMenu(menu => menu.WithText("Editors").ConfigureItem(item => item
			.AsScreenItem()
			.WithText("Empty workspace")
			.WithScreen<Screen>()
			.WithScreenKind(ScreenKind.Empty)
			.AsSingleInstance()
		));
		Assert.That(builder.Build(), Is.SameAs(block));
		var definition = ((IScreenMenuItem)block.Menus[0].Items[0]).ScreenDefinition;
		Assert.That(block.DefaultScreenDefinition, Is.SameAs(definition));
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(definition));
		Assert.That(block.ContainsScreenDefinition(provisional), Is.False);
		Assert.That(block.DefaultScreenDefinition.ScreenKind, Is.EqualTo(ScreenKind.Empty));
		Assert.That(block.Name, Is.EqualTo("Edited workspace"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void NativeCachedEmptyDefaultRemainsBlockOwnedAfterRemovingItsNavigation(bool removeEntireMenu) {
		var builder = new WinFormsApplicationBlockBuilder().WithName("Workspace").WithDefaultScreen<Screen>()
			.ConfigureMenu(menu => menu.WithText("Editors").ConfigureItem(item => item
				.AsScreenItem()
				.WithText("Empty workspace")
				.WithScreen<Screen>()
				.WithScreenKind(ScreenKind.Empty)
				.AsSingleInstance()
			));
		using var block = builder.Build();
		var menu = (WinFormsApplicationMenu)block.Menus[0];
		var item = (WinFormsScreenMenuItem)menu.Items[0];
		var definition = item.ScreenDefinition;
		block.Name = "Edited workspace";
		if (removeEntireMenu)
			block.RemoveMenu(menu);
		else
			menu.RemoveItem(item);
		Assert.That(builder.Build(), Is.SameAs(block));
		Assert.That(block.DefaultScreenDefinition, Is.SameAs(definition));
		Assert.That(block.ScreenDefinitions.Single(), Is.SameAs(definition));
		Assert.That(block.DefaultScreenDefinition.ScreenKind, Is.EqualTo(ScreenKind.Empty));
		Assert.That(block.Name, Is.EqualTo("Edited workspace"));
		if (removeEntireMenu)
			menu.Dispose();
		else
			item.Dispose();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void NativeCachedDefaultsKeepExplicitlyOwnedDefinitions(bool addGeneratedDefinitionExplicitly) {
		var explicitDefinition = new ApplicationScreenDefinition(typeof(Screen), "Explicit editor");
		var builder = new WinFormsApplicationBlockBuilder().WithName("Workspace");
		if (addGeneratedDefinitionExplicitly)
			builder.WithDefaultScreen<Screen>();
		else
			builder.WithDefaultScreen(explicitDefinition);
		using var block = builder.Build();
		if (addGeneratedDefinitionExplicitly) {
			explicitDefinition = block.DefaultScreenDefinition;
			builder.AddScreenDefinition(explicitDefinition);
		}
		builder.WithDefaultScreen<Screen>("New editor");
		Assert.That(builder.Build(), Is.SameAs(block));
		Assert.That(block.ContainsScreenDefinition(explicitDefinition), Is.True);
		Assert.That(block.DefaultScreenDefinition, Is.Not.SameAs(explicitDefinition));
		Assert.That(block.DefaultScreenDefinition.Title, Is.EqualTo("New editor"));
	}

	[Test]
	public void NativeScreenBuilderRejectsActionOnlyConfiguration() {
		var builder = new WinFormsApplicationMenuItemBuilder()
			.AsScreenItem()
			.WithText("Invalid")
			.WithAction(() => { });
		Assert.That(() => builder.Build(), Throws.InvalidOperationException);
	}

	[Test]
	public void NativeActionBuilderRejectsScreenOnlyConfiguration() {
		var builder = new WinFormsApplicationMenuItemBuilder()
			.AsActionItem()
			.WithText("Invalid")
			.WithScreen<Screen>();
		Assert.That(() => builder.Build(), Throws.InvalidOperationException);
	}

	[Test]
	public void NativeActionBuilderRejectsAsyncConfigurationItCannotExecute() {
		var builder = new WinFormsApplicationMenuItemBuilder()
			.AsActionItem()
			.WithText("Invalid")
			.WithAction((_, _) => Task.CompletedTask);
		Assert.That(() => builder.Build(), Throws.InvalidOperationException);
	}

	[TestCase(true)]
	[TestCase(false)]
	public async Task NativeWizardPreservesAsyncTerminalVetoes(bool finishing) {
		using var step = new WinFormsWizardScreen<object>();
		using var wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Setup")
			.AddScreen(step)
			.OnFinished(async _ => {
				await Task.Yield();
				return new Result<bool>(false);
			})
			.OnCancelled(async _ => {
				await Task.Yield();
				return new Result<bool>(false);
			})
			.Build();

		var result = finishing ? await wizard.FinishAsync() : await wizard.CancelAsync();
		Assert.That(result.IsSuccess, Is.True);
		Assert.That(result.Value, Is.False);
		Assert.That(wizard.State, Is.EqualTo(WizardState.Active));
	}

	[Test]
	public void NativeWizardRetainsLegacySynchronousCancellationCallback() {
		using var wizard = new WinFormsWizardBuilder<object>()
			.WithTitle("Setup")
			.AddScreen(new WinFormsWizardScreen<object>())
			.OnFinished(_ => Task.FromResult(Result.Success))
			.OnCancelled(_ => Result.Error("Keep editing"))
			.Build();

		Assert.That(wizard.CancelRequested().IsFailure, Is.True);
	}

	[Test]
	public void NativeScreenShortcutsCreateDistinctCanonicalDefinitions() {
		var menu = new WinFormsApplicationMenuBuilder()
			.WithText("Screens")
			.AddScreenItem<Screen>("First")
			.AddScreenItem<Screen>("Second")
			.Build();
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		block.AddMenu(menu);
		var first = ((IScreenMenuItem)menu.Items[0]).ScreenDefinition;
		var second = ((IScreenMenuItem)menu.Items[1]).ScreenDefinition;
		Assert.That(first, Is.Not.SameAs(second));
		Assert.That(first.ScreenType, Is.EqualTo(second.ScreenType));
		Assert.That(first.Title, Is.EqualTo("First"));
		Assert.That(second.Title, Is.EqualTo("Second"));
		Assert.That(block.ScreenDefinitions.Length, Is.EqualTo(2));
		Assert.That(block.ScreenDefinitions[0], Is.SameAs(first));
		Assert.That(block.ScreenDefinitions[1], Is.SameAs(second));
	}

	private sealed class Screen : WinFormsApplicationScreen {
	}
}
