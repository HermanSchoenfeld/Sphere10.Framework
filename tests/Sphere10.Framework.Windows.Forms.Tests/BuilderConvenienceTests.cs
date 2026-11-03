// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using FormsUI = global::Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Checks inline builder overloads against native ownership and deferred activation.</summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
public class BuilderConvenienceTests {
	[Test]
	public void InlineRegistrationsAndMenusKeepExactMonikersAndTheRetainedNativeGraph() {
		var activations = 0;
		var defaults = new Preferences { Value = 7 };
		var builder = new ApplicationBlockBuilder()
			.WithName("Input")
			.AddScreen<ProbeScreen>("First", out var first)
			.AddScreen<ProbeScreen>("Second", out var second, activationMode: ScreenActivationMode.SingleInstance,
				stableKey: "second", settings: defaults, activator: _ => {
					activations++;
					return new ProbeScreen();
				}
			)
			.WithDefaultScreen(second)
			.AddMenuItem("Choices", out var menu, image32x32: new Bitmap(32, 32), showInMenuStrip: true, configure: items => items
				.AddItem<ProbeScreen>()
				.AddItem(second, "Exact second")
			);
		using var block = builder.Build();

		Assert.That(builder.Build(), Is.SameAs(block));
		Assert.That(block.DefaultScreenDefinition.Settings, Is.SameAs(defaults));
		Assert.That(activations, Is.Zero);
		Assert.That(block.Menus.Single(), Is.SameAs(menu));
		Assert.That(menu.Parent, Is.SameAs(block));
		Assert.That(menu.ShowInMenuStrip, Is.True);
		Assert.That(menu.Image32x32.Size, Is.EqualTo(new Size(32, 32)));
		Assert.That(((IWinFormsScreenMenuItem)menu.Items[0]).ScreenDefinition.Registration, Is.SameAs(first));
		Assert.That(((IWinFormsScreenMenuItem)menu.Items[1]).ScreenDefinition, Is.SameAs(block.DefaultScreenDefinition));
		Assert.That(block.DefaultScreenDefinition.Registration, Is.SameAs(second));
		Assert.That(block.DefaultScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(((Preferences)block.DefaultScreenDefinition.Settings).Value, Is.EqualTo(7));
		Assert.That(block.DefaultScreenDefinition.Activators.Single().Title, Is.EqualTo("Exact second"));
	}

	[Test]
	public void RuntimeTypesAndInlineActionsUseExistingCommandExecutionAndReferences() {
		var calls = 0;
		FormsUI.IApplicationMenuItem action = null;
		FormsUI.IApplicationMenuItem screen = null;
		var builder = new ApplicationBlockBuilder()
			.WithName("Commands")
			.AddScreen(typeof(ProbeScreen), "Runtime", out var registration)
			.AddMenu("Actions", configure: menu => menu
				.AddItem("Run", () => calls++, out action, showOnExplorerBar: false, showOnToolBar: false, executeOnLoad: true)
				.AddItem(typeof(ProbeScreen), "Runtime command", out screen, isStartScreen: true)
				.AddItem("UI action", _ => calls++)
			);
		using var block = builder.Build();

		Assert.That(calls, Is.Zero);
		Assert.That(block.Menus.Single().Items[0], Is.SameAs(action));
		Assert.That(action.ShowOnExplorerBar, Is.False);
		Assert.That(action.ShowOnToolStrip, Is.False);
		Assert.That(action.ExecuteOnLoad, Is.True);
		Assert.That(((IWinFormsScreenMenuItem)screen).ScreenDefinition.Registration, Is.SameAs(registration));
		Assert.That(screen.ExecuteOnLoad, Is.True);
		((WinFormsActionMenuItem)action).OnSelect();
		Assert.That(calls, Is.EqualTo(1));
	}

	[Test]
	public void InvalidInlineChildrenDoNotPublishPartialDefinitions() {
		var builder = new ApplicationBlockBuilder().WithName("Valid");

		Assert.That(() => builder.AddScreen<ProbeScreen>(""), Throws.ArgumentException);
		Assert.That(() => builder.AddScreen<ProbeScreen>("Bad", activationMode: ScreenActivationMode.PermanentSingleton, screenKind: ScreenKind.Empty), Throws.ArgumentException);
		Assert.That(() => builder.AddMenuItem("Broken", configure: menu => menu.AddItem("Run", (Action)null)), Throws.ArgumentNullException);
		using var block = builder.AddScreen<ProbeScreen>("Valid").Build();
		Assert.That(block.ScreenDefinitions, Has.Length.EqualTo(1));
		Assert.That(block.Menus, Is.Empty);
	}

	private sealed class ProbeScreen : ApplicationScreen {
	}

	private sealed class Preferences : SettingsObject {
		public int Value { get; set; }
	}
}

