// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

/// <summary>Verifies that shared owners expose commands and preserve specialized provider projections.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationCommandProviderContractTests {

	[Test]
	public void SharedApplicationsExposeEmptyCommandDefaults() {
		using IApplication application = new EmptyApplication();

		Assert.That(application.Menus, Is.Empty);
		Assert.That(application.ToolBarItems, Is.Empty);
	}

	[Test]
	public void TypedApplicationCommandsProjectThroughTheSharedOwnerWithoutCopyingOrSubstitution() {
		var menus = new[] { new TestApplicationMenu<TestApplicationMenuItem> { Text = "File" } };
		var toolbar = new[] { new TestApplicationMenuItem { Title = "Save" } };
		using var concrete = new TypedApplication { Menus = menus, ToolBarItems = toolbar };
		IApplication application = concrete;
		IApplicationCommandProvider<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem> typed = concrete;

		Assert.That(application.Menus, Is.SameAs(menus));
		Assert.That(application.ToolBarItems, Is.SameAs(toolbar));
		Assert.That(typed.Menus, Is.SameAs(application.Menus));
		Assert.That(typed.ToolBarItems, Is.SameAs(application.ToolBarItems));
	}

	[Test]
	public void DecoratorsForwardCommandReplacementsWithoutRetainingOldArrays() {
		using var concrete = new TypedApplication {
			Menus = new[] { new TestApplicationMenu<TestApplicationMenuItem> { Text = "File" } },
			ToolBarItems = new[] { new TestApplicationMenuItem { Title = "Save" } }
		};
		using IApplication application = new CommandDecorator(concrete);
		Assert.That(application.Menus, Is.SameAs(concrete.Menus));
		Assert.That(application.ToolBarItems, Is.SameAs(concrete.ToolBarItems));

		// A change in the underlying application's presentation must be visible through the same decorator.
		concrete.Menus = new[] { new TestApplicationMenu<TestApplicationMenuItem> { Text = "Help" } };
		concrete.ToolBarItems = new[] { new TestApplicationMenuItem { Title = "About" } };
		Assert.That(application.Menus, Is.SameAs(concrete.Menus));
		Assert.That(application.ToolBarItems, Is.SameAs(concrete.ToolBarItems));
		Assert.That(application.Menus[0].Text, Is.EqualTo("Help"));
		Assert.That(application.ToolBarItems[0].Title, Is.EqualTo("About"));
	}

	[Test]
	public void SharedBlocksExposeEmptyCommandDefaults() {
		IApplicationBlock block = new EmptyBlock();

		Assert.That(block.Menus, Is.Empty);
		Assert.That(block.ToolBarItems, Is.Empty);
	}

	[Test]
	public void SharedScreensExposeEmptyCommandDefaults() {
		IApplicationScreen screen = new EmptyScreen();

		Assert.That(screen.Menus, Is.Empty);
		Assert.That(screen.ToolBarItems, Is.Empty);
	}

	[Test]
	public void SharedPluginsExposeEmptyCommandDefaults() {
		IApplicationPlugin plugin = new ApplicationPlugin("Commands", Array.Empty<IApplicationBlock>());

		Assert.That(plugin.Menus, Is.Empty);
		Assert.That(plugin.ToolBarItems, Is.Empty);
	}

	[Test]
	public void TypedBlockCommandsProjectThroughTheSharedOwnerWithoutCopyingOrSubstitution() {
		var menus = new[] { new TestApplicationMenu<TestApplicationMenuItem> { Text = "File" } };
		var toolbar = new[] { new TestApplicationMenuItem { Title = "Save" } };
		var concrete = new TypedBlock { Menus = menus, ToolBarItems = toolbar };
		IApplicationBlock block = concrete;
		IApplicationCommandProvider<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem> typed = concrete;

		Assert.That(block.Menus, Is.SameAs(menus));
		Assert.That(block.ToolBarItems, Is.SameAs(toolbar));
		Assert.That(typed.Menus, Is.SameAs(block.Menus));
		Assert.That(typed.ToolBarItems, Is.SameAs(block.ToolBarItems));
	}

	[Test]
	public void TypedScreenCommandsProjectThroughTheSharedOwnerWithoutCopyingOrSubstitution() {
		var menus = new[] { new TestApplicationMenu<TestApplicationMenuItem> { Text = "File" } };
		var toolbar = new[] { new TestApplicationMenuItem { Title = "Save" } };
		var concrete = new TypedScreen { Menus = menus, ToolBarItems = toolbar };
		IApplicationScreen screen = concrete;
		IApplicationCommandProvider<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem> typed = concrete;

		Assert.That(screen.Menus, Is.SameAs(menus));
		Assert.That(screen.ToolBarItems, Is.SameAs(toolbar));
		Assert.That(typed.Menus, Is.SameAs(screen.Menus));
		Assert.That(typed.ToolBarItems, Is.SameAs(screen.ToolBarItems));
	}

	[Test]
	public void TypedPluginCommandsProjectThroughTheSharedOwnerWithoutCopyingOrSubstitution() {
		var menus = new[] { new TestApplicationMenu<TestApplicationMenuItem> { Text = "File" } };
		var toolbar = new[] { new TestApplicationMenuItem { Title = "Save" } };
		var concrete = new TypedPlugin { Menus = menus, ToolBarItems = toolbar };
		IApplicationPlugin plugin = concrete;
		IApplicationCommandProvider<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem> typed = concrete;

		Assert.That(plugin.Menus, Is.SameAs(menus));
		Assert.That(plugin.ToolBarItems, Is.SameAs(toolbar));
		Assert.That(typed.Menus, Is.SameAs(plugin.Menus));
		Assert.That(typed.ToolBarItems, Is.SameAs(plugin.ToolBarItems));
	}

	// The running application has the same portable provider defaults as its other command scopes.
	private sealed class EmptyApplication : SyncApplicationBase {
		public override IApplicationPlugin[] LoadedPlugins {
			get {
				return Array.Empty<IApplicationPlugin>();
			}
		}

		public override IApplicationScreenHost ScreenHost {
			get {
				return null;
			}
		}
	}

	// A platform specialization bridges its real descriptor arrays over the inherited empty defaults.
	private sealed class TypedApplication : SyncApplicationBase,
		IApplicationCommandProvider<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem> {
		public TestApplicationMenu<TestApplicationMenuItem>[] Menus { get; set; }

		public TestApplicationMenuItem[] ToolBarItems { get; set; }

		public override IApplicationPlugin[] LoadedPlugins {
			get {
				return Array.Empty<IApplicationPlugin>();
			}
		}

		public override IApplicationScreenHost ScreenHost {
			get {
				return null;
			}
		}
	}

	// A real decorator consumer must observe current application commands rather than interface defaults.
	private sealed class CommandDecorator : ApplicationDecorator {
		public CommandDecorator(IApplication application)
			: base(application) {
		}
	}

	// An ordinary block needs no redundant provider declaration or command storage for empty defaults.
	private sealed class EmptyBlock : ApplicationBlockBase {
	}

	// A shared screen uses the inherited command defaults independently of native or web rendering.
	private sealed class EmptyScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}

	// The specialized interface's projection must take precedence over the shared empty defaults.
	private sealed class TypedBlock : ApplicationBlockBase,
		IApplicationCommandProvider<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem> {
		public TestApplicationMenu<TestApplicationMenuItem>[] Menus { get; init; }

		public TestApplicationMenuItem[] ToolBarItems { get; init; }
	}

	// Retain the same specialized projection through the portable screen contract.
	private sealed class TypedScreen : IApplicationScreen,
		IApplicationCommandProvider<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem> {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }

		public TestApplicationMenu<TestApplicationMenuItem>[] Menus { get; init; }

		public TestApplicationMenuItem[] ToolBarItems { get; init; }
	}

	// A plugin can specialize its commands without altering its existing block or startup ownership.
	private sealed class TypedPlugin : ApplicationPlugin,
		IApplicationCommandProvider<TestApplicationMenu<TestApplicationMenuItem>, TestApplicationMenuItem> {
		public TypedPlugin()
			: base("Typed commands", Array.Empty<IApplicationBlock>()) {
		}

		public TestApplicationMenu<TestApplicationMenuItem>[] Menus { get; init; }

		public TestApplicationMenuItem[] ToolBarItems { get; init; }
	}
}
