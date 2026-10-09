// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
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
						.WithId("stock")
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
		Assert.That(screen.ScreenTitle, Is.EqualTo("Stock levels"));
		Assert.That(screen.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
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
		Assert.That(block.DefaultScreenTitle, Is.EqualTo("Default"));
		Assert.That(block.Menus.Length, Is.EqualTo(2));
		Assert.That(block.Menus[1].Parent, Is.SameAs(block));
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
	public void NativeScreenShortcutsPreserveTextBasedIdentifiers() {
		using var menu = new WinFormsApplicationMenuBuilder()
			.WithText("Screens")
			.AddScreenItem<Screen>("First")
			.AddScreenItem<Screen>("Second")
			.Build();
		Assert.That(menu.Items[0].Id, Is.EqualTo("First"));
		Assert.That(menu.Items[1].Id, Is.EqualTo("Second"));
	}

	private sealed class Screen : WinFormsApplicationScreen {
	}
}
