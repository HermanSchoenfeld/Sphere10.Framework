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

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[Apartment(ApartmentState.STA)]
public class NativeApplicationMenuBuilderPatternTests {
	[Test]
	public void ScreenShortcutPreservesConfiguredMetadata() {
		var directImage = new Bitmap(16, 16);
		var configuredImage = new Bitmap(16, 16);
		using var direct = new WinFormsApplicationMenuBuilder()
			.WithText("Screens")
			.AddScreenItem<Screen>("Stock", directImage, showOnExplorerBar: false, showOnToolBar: false, isStartScreen: true, title: "Stock levels")
			.Build();
		using var configured = new WinFormsApplicationMenuBuilder()
			.WithText("Screens")
			.ConfigureItem(item => item
				.AsScreenItem()
				.WithText("Stock")
				.WithScreen<Screen>()
				.WithImage(configuredImage)
				.ShowOnExplorerBar(false)
				.ShowOnToolBar(false)
				.IsStartScreen()
				.WithScreenTitle("Stock levels")
			)
			.Build();
		var directItem = (WinFormsScreenMenuItem)direct.Items[0];
		var configuredItem = (WinFormsScreenMenuItem)configured.Items[0];
		Assert.That(directItem.Text, Is.EqualTo(configuredItem.Text));
		Assert.That(directItem.ScreenDefinition.ScreenType, Is.EqualTo(configuredItem.ScreenDefinition.ScreenType));
		Assert.That(directItem.ScreenDefinition.Title, Is.EqualTo(configuredItem.ScreenDefinition.Title));
		Assert.That(directItem.ScreenDefinition.ActivationMode, Is.EqualTo(configuredItem.ScreenDefinition.ActivationMode));
		Assert.That(directItem.ScreenDefinition.ScreenKind, Is.EqualTo(configuredItem.ScreenDefinition.ScreenKind));
		Assert.That(directItem.IsDefault, Is.EqualTo(configuredItem.IsDefault));
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
		using var menu = new WinFormsApplicationMenuBuilder()
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

	[TestCase(null, typeof(ArgumentException))]
	[TestCase("", typeof(ArgumentException))]
	[TestCase(" ", typeof(InvalidOperationException))]
	public void InvalidScreenShortcutDoesNotAppendAnItem(string text, Type exceptionType) {
		var builder = new WinFormsApplicationMenuBuilder()
			.WithText("Screens");

		Assert.That(() => builder.AddScreenItem<Screen>(text), Throws.TypeOf(exceptionType));
		using var menu = builder.Build();
		Assert.That(menu.Items, Is.Empty);
	}

	private sealed class Screen : WinFormsApplicationScreen {
	}
}
