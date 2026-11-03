// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Threading;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[Apartment(ApartmentState.STA)]
public class NativeApplicationMenuBuilderPatternTests {
	[Test]
	public void ScreenShortcutPreservesConfiguredMetadata() {
		var directImage = new Bitmap(16, 16);
		var configuredImage = new Bitmap(16, 16);
		var definition = new SharedUI.ApplicationScreenDefinition(typeof(Screen), "Stock levels");
		using var directBlock = new ApplicationBlockBuilder()
			.WithName("Direct")
			.AddScreen(definition)
			.WithDefaultScreen(definition)
			.AddMenu(menu => menu
				.WithText("Screens")
				.AddShowScreenItem(definition, "Stock", directImage, showOnExplorerBar: false, showOnToolBar: false, isStartScreen: true)
			)
			.Build();
		using var configuredBlock = new ApplicationBlockBuilder()
			.WithName("Configured")
			.AddScreen(definition)
			.WithDefaultScreen(definition)
			.AddMenu(menu => menu
				.WithText("Screens")
				.AddItem(item => item
					.AsScreenItem()
					.WithText("Stock")
					.WithScreen(definition.Registration)
					.WithImage(configuredImage)
					.ShowOnExplorerBar(false)
					.ShowOnToolBar(false)
					.IsStartScreen()
				)
			)
			.Build();
		var directItem = (WinFormsScreenMenuItem)directBlock.Menus[0].Items[0];
		var configuredItem = (WinFormsScreenMenuItem)configuredBlock.Menus[0].Items[0];
		Assert.That(directItem.Text, Is.EqualTo(configuredItem.Text));
		Assert.That(directItem.ScreenDefinition.ScreenType, Is.EqualTo(configuredItem.ScreenDefinition.ScreenType));
		Assert.That(directItem.ScreenDefinition.Title, Is.EqualTo(configuredItem.ScreenDefinition.Title));
		Assert.That(directItem.ScreenDefinition.ActivationMode, Is.EqualTo(configuredItem.ScreenDefinition.ActivationMode));
		Assert.That(directItem.ScreenDefinition.ScreenKind, Is.EqualTo(configuredItem.ScreenDefinition.ScreenKind));
		Assert.That(directBlock.DefaultScreenDefinition, Is.SameAs(configuredBlock.DefaultScreenDefinition));
		Assert.That(directItem.ShowOnExplorerBar, Is.EqualTo(configuredItem.ShowOnExplorerBar));
		Assert.That(directItem.ShowOnToolStrip, Is.EqualTo(configuredItem.ShowOnToolStrip));
		Assert.That(directItem.ExecuteOnLoad, Is.EqualTo(configuredItem.ExecuteOnLoad));
		Assert.That(directItem.Image16x16, Is.SameAs(directImage));
	}

	[Test]
	public void OwnerAwareActionShortcutPreservesNativeOptions() {
		var image = new Bitmap(16, 16);
		IUserInterfaceServices receivedUI = null;
		var userInterface = new ConsoleApplicationUserInterfaceServices();
		using var menu = new ApplicationMenuBuilder()
			.WithText("Actions")
			.AddActionItem("Run", ui => receivedUI = ui, image, showOnExplorerBar: false, showOnToolBar: false, executeOnLoad: true)
			.Build();
		var item = (WinFormsActionMenuItem)menu.Items[0];

		item.OnSelect(userInterface);
		Assert.That(item.Text, Is.EqualTo("Run"));
		Assert.That(item.Image16x16, Is.SameAs(image));
		Assert.That(item.ShowOnExplorerBar, Is.False);
		Assert.That(item.ShowOnToolStrip, Is.False);
		Assert.That(item.ExecuteOnLoad, Is.True);
		Assert.That(receivedUI, Is.SameAs(userInterface));
	}

	[Test]
	public void AShowScreenShortcutRejectsAnInvalidRegistrationBeforeAppendingACommand() {
		var builder = new ApplicationMenuBuilder()
			.WithText("Screens");

		Assert.That(() => builder.AddShowScreenItem((ApplicationScreenRegistration)null), Throws.ArgumentNullException);
		using var menu = builder.Build();
		Assert.That(menu.Items, Is.Empty);
	}

	[Test]
	public void AnOmittedShortcutCaptionUsesTheRegistrationTitleAfterBlockBinding() {
		using var block = new ApplicationBlockBuilder()
			.WithName("Workspace")
			.AddScreen<Screen>(screen => screen.WithTitle("Stock levels"))
			.AddMenu(menu => menu
				.WithText("Screens")
				.AddShowScreenItem<Screen>()
			)
			.Build();
		Assert.That(block.Menus[0].Items[0].Title, Is.EqualTo("Stock levels"));
	}

	private sealed class Screen : ApplicationScreen {
	}
}
