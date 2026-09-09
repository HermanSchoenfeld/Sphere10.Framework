// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.Threading;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ScreenActivationBuilderTests {
	private bool _startedFramework;

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

	[TestCase(ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenActivationMode.MultiInstance)]
	public void FluentMethodsPreserveTheBuilderAndDeclareMetadata(ScreenActivationMode Mode) {
		var Builder = new MenuItemBuilder();
		var ScreenBuilder = Builder.AsScreenItem().WithScreen<DefaultSingleScreen>().WithText("Open");
		var Result = Mode == ScreenActivationMode.SingleInstance ? ScreenBuilder.AsSingleInstance() : ScreenBuilder.AsMultiInstance();
		using var Item = (ScreenMenuItem)Builder.Build();
		Assert.That(Result, Is.SameAs(ScreenBuilder));
		Assert.That(Item.ActivationMode, Is.EqualTo(Mode));
		Assert.That(((IScreenMenuItem)Item).ActivationMode, Is.EqualTo(Mode));
		Assert.That(Item.Screen, Is.EqualTo(typeof(DefaultSingleScreen)));
	}

	[Test]
	public void UnspecifiedMetadataRetainsTheScreenConstructorDefault() {
		using var Block = new ApplicationBlockBuilder().WithName("Defaults").AddMenu(Menu => Menu.WithText("Screens")
			.AddScreenItem<DefaultSingleScreen>("Settings")
			.ConfigureItem(Item => Item.AsScreenItem().WithScreen<DefaultMultiScreen>().WithText("Design"))).Build();
		using var DirectItem = new ScreenMenuItem("Direct", typeof(DefaultSingleScreen));
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		Assert.That(Block.Menus[0].Items.Cast<IScreenMenuItem>().Select(Item => Item.ActivationMode), Is.All.Null);
		Assert.That(DirectItem.ActivationMode, Is.Null);
		var Settings = Host.ActivateScreen(Block, typeof(DefaultSingleScreen));
		var Design = Host.ActivateScreen(Block, typeof(DefaultMultiScreen));
		Assert.That(Host.ActivateScreen(Block, typeof(DefaultSingleScreen)), Is.SameAs(Settings));
		Assert.That(Host.ActivateScreen(Block, typeof(DefaultMultiScreen)), Is.Not.SameAs(Design));
	}

	[TestCase(ScreenMode.SingleView)]
	[TestCase(ScreenMode.MultiView)]
	public void MultiInstanceDeclarationOverridesTheDefaultSingleConstructor(ScreenMode Mode) {
		using var Block = CreateBlock<DefaultSingleScreen>(ScreenActivationMode.MultiInstance);
		using var Host = new ApplicationScreenHost { ScreenMode = Mode };
		var First = Host.ActivateScreen(Block, typeof(DefaultSingleScreen))!;
		var Second = Host.ActivateScreen(Block, typeof(DefaultSingleScreen))!;
		Assert.That(First.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(Second.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(Second, Is.Not.SameAs(First));
		Assert.That(First.IsDisposed, Is.EqualTo(Mode == ScreenMode.SingleView));
		Assert.That(Host.OpenScreens, Has.Count.EqualTo(Mode == ScreenMode.MultiView ? 2 : 1));
	}

	[TestCase(ScreenMode.SingleView, false)]
	[TestCase(ScreenMode.MultiView, false)]
	[TestCase(ScreenMode.MultiView, true)]
	public void ExplicitSingletonIsReusedAcrossBlocksWhenCachedOrDetached(ScreenMode Mode, bool Detached) {
		using var Block = CreateBlock<DefaultMultiScreen>(ScreenActivationMode.SingleInstance);
		using var OtherBlock = CreateBlock<DefaultMultiScreen>(ScreenActivationMode.SingleInstance);
		using var Host = new ApplicationScreenHost { ScreenMode = Mode };
		var First = Host.ActivateScreen(Block, typeof(DefaultMultiScreen))!;
		Host.ActivateScreen(Block, typeof(DefaultSingleScreen));
		if (Detached)
			Assert.That(Host.UndockScreen(First), Is.True);
		Assert.That(Host.ActivateScreen(OtherBlock, typeof(DefaultMultiScreen)), Is.SameAs(First));
		Assert.That(First.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(First.ApplicationBlock, Is.SameAs(Block));
		Assert.That(Host.Screens.OfType<DefaultMultiScreen>().Count(), Is.EqualTo(1));
		Assert.That(Host.IsScreenUndocked(First), Is.EqualTo(Detached));
	}

	[Test]
	public void UnspecifiedAliasUsesALaterDeclarationFromAnotherMenu() {
		using var Block = new ApplicationBlockBuilder().WithName("Aliases")
			.AddMenu(Menu => Menu.WithText("Shortcuts").AddScreenItem<DefaultSingleScreen>("Design shortcut"))
			.AddMenu(Menu => Menu.WithText("Designs").ConfigureItem(Item => Item.AsScreenItem()
				.WithScreen<DefaultSingleScreen>().WithText("New design").AsMultiInstance())).Build();
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var Alias = (IScreenMenuItem)Block.Menus[0].Items[0];
		var First = Host.ActivateScreen(Alias.Parent.Parent, Alias.Screen)!;
		var Second = Host.ActivateScreen(Alias.Parent.Parent, Alias.Screen)!;
		Assert.That(First.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(Second, Is.Not.SameAs(First));
		Assert.That(Host.OpenScreens, Has.Count.EqualTo(2));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SuppliedScreensUseTheirBlockOrPreviouslyRegisteredMultiInstanceDeclaration(bool RegisterFirst) {
		using var Block = CreateBlock<DefaultSingleScreen>(ScreenActivationMode.MultiInstance);
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		if (RegisterFirst)
			((IApplicationScreenHost)Host).RegisterScreenTypes(Block);
		using var First = new DefaultSingleScreen();
		using var Second = new DefaultSingleScreen();
		if (!RegisterFirst) {
			First.ApplicationBlock = Block;
			Second.ApplicationBlock = Block;
		}
		Assert.That(Host.ShowScreen(First), Is.True);
		Assert.That(Host.ShowScreen(Second), Is.True);
		Assert.That(First.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(Second.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(Host.OpenScreens, Is.EquivalentTo(new[] { First, Second }));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SuppliedScreensCannotBypassAnExplicitSingletonWithAMultiInstanceConstructor(bool RegisterFirst) {
		using var Block = CreateBlock<DefaultMultiScreen>(ScreenActivationMode.SingleInstance);
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		if (RegisterFirst)
			Host.RegisterScreenTypes(Block);
		using var First = new DefaultMultiScreen();
		using var Duplicate = new DefaultMultiScreen();
		if (!RegisterFirst) {
			First.ApplicationBlock = Block;
			Duplicate.ApplicationBlock = Block;
		}
		Assert.That(Host.ShowScreen(First), Is.True);
		Assert.That(First.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(() => Host.ShowScreen(Duplicate), Throws.ArgumentException);
		Assert.That(Duplicate.IsDisposed, Is.False);
		Assert.That(Host.ActiveScreen, Is.SameAs(First));
		Assert.That(Host.Screens, Is.EqualTo(new[] { First }));
	}

	[Test]
	public void ConflictingDeclarationsWithinABlockDoNotRegisterAnyTypes() {
		using var Block = CreateConflictingBlock();
		using var EmptyBlock = new ApplicationBlock();
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		Assert.That(() => Host.RegisterScreenTypes(Block), Throws.ArgumentException);
		Assert.That(Host.Screens, Is.Empty);
		var First = Host.ActivateScreen(EmptyBlock, typeof(DefaultSingleScreen))!;
		Assert.That(First.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance), "A rejected block must not leak its earlier multi-instance declaration");
		Assert.That(Host.ActivateScreen(EmptyBlock, typeof(DefaultSingleScreen)), Is.SameAs(First));
	}

	[Test]
	public void ConflictingDeclarationsAcrossRegisteredBlocksAreAtomic() {
		using var RegisteredBlock = CreateBlock<DefaultMultiScreen>(ScreenActivationMode.SingleInstance);
		using var ConflictingBlock = new ApplicationBlockBuilder().WithName("Conflict").AddMenu(Menu => Menu.WithText("Screens")
			.ConfigureItem(Item => Item.AsScreenItem().WithScreen<DefaultSingleScreen>().WithText("New design").AsMultiInstance())
			.ConfigureItem(Item => Item.AsScreenItem().WithScreen<DefaultMultiScreen>().WithText("Conflict").AsMultiInstance())).Build();
		using var EmptyBlock = new ApplicationBlock();
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		Host.RegisterScreenTypes(RegisteredBlock);
		Assert.That(() => Host.RegisterScreenTypes(ConflictingBlock), Throws.ArgumentException);
		var Untouched = Host.ActivateScreen(EmptyBlock, typeof(DefaultSingleScreen))!;
		var Registered = Host.ActivateScreen(RegisteredBlock, typeof(DefaultMultiScreen))!;
		Assert.That(Untouched.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(Registered.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(Host.ActivateScreen(EmptyBlock, typeof(DefaultSingleScreen)), Is.SameAs(Untouched));
		Assert.That(Host.ActivateScreen(RegisteredBlock, typeof(DefaultMultiScreen)), Is.SameAs(Registered));
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void ResolvedTypePolicyCannotChangeAfterInstantiationOrClosing(bool ExplicitInitially, bool CloseFirst) {
		using var InitialBlock = ExplicitInitially ? CreateBlock<DefaultSingleScreen>(ScreenActivationMode.SingleInstance) : new ApplicationBlock();
		using var ConflictingBlock = CreateBlock<DefaultSingleScreen>(ScreenActivationMode.MultiInstance);
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var First = Host.ActivateScreen(InitialBlock, typeof(DefaultSingleScreen))!;
		if (CloseFirst)
			Assert.That(Host.CloseScreen(First), Is.True);
		Assert.That(() => Host.RegisterScreenTypes(ConflictingBlock), Throws.ArgumentException);
		Assert.That(() => Host.ActivateScreen(ConflictingBlock, typeof(DefaultSingleScreen)), Throws.ArgumentException);
		Assert.That(Host.Screens, Has.Count.EqualTo(CloseFirst ? 0 : 1));
		var Selected = Host.ActivateScreen(InitialBlock, typeof(DefaultSingleScreen))!;
		Assert.That(Selected.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
		Assert.That(Host.ActivateScreen(InitialBlock, typeof(DefaultSingleScreen)), Is.SameAs(Selected));
	}

	[Test]
	public void SelectingAnExistingSingletonStillValidatesTheWholeIncomingBlock() {
		using var InitialBlock = new ApplicationBlock();
		using var ConflictingBlock = CreateConflictingBlock();
		using var Host = new ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var Existing = Host.ActivateScreen(InitialBlock, typeof(OtherScreen));
		Assert.That(() => Host.ActivateScreen(ConflictingBlock, typeof(OtherScreen)), Throws.ArgumentException);
		Assert.That(Host.ActiveScreen, Is.SameAs(Existing));
		Assert.That(Host.Screens, Is.EqualTo(new[] { Existing }));
	}

	[Test]
	public void RegisterBlockAppliesALaterDeclarationToItsDefaultScreen() {
		using var Block = new ApplicationBlockBuilder().WithName("Default").WithDefaultScreen<DefaultSingleScreen>("Initial design")
			.AddMenu(Menu => Menu.WithText("Shortcuts").AddScreenItem<DefaultSingleScreen>("Design shortcut"))
			.AddMenu(Menu => Menu.WithText("Designs").ConfigureItem(Item => Item.AsScreenItem()
				.WithScreen<DefaultSingleScreen>().WithText("New design").AsMultiInstance())).Build();
		using var Form = new BlockMainForm { ScreenMode = ScreenMode.MultiView };
		Form.RegisterBlock(Block);
		var Initial = Form.ActiveScreen!;
		Assert.That(Initial.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(Initial.Title, Is.EqualTo("Initial design"));
		Assert.That(Form.ScreenHost.ActivateScreen(Block, typeof(DefaultSingleScreen)), Is.Not.SameAs(Initial));
		Assert.That(Form.ScreenHost.OpenScreens, Has.Count.EqualTo(2));
	}

	[Test]
	public void RegisterBlockAppliesAllDeclarationsBeforeAnyExecuteOnLoadAction() {
		using var Form = new BlockMainForm { ScreenMode = ScreenMode.MultiView };
		ApplicationScreen? StartupScreen = null;
		using var Block = new ApplicationBlockBuilder().WithName("Startup")
			.AddMenu(Menu => Menu.WithText("Startup").AddActionItem("Open initial design", () => {
				StartupScreen = new DefaultSingleScreen();
				Form.ShowScreen(StartupScreen);
			}, executeOnLoad: true))
			.AddMenu(Menu => Menu.WithText("Designs").ConfigureItem(Item => Item.AsScreenItem()
				.WithScreen<DefaultSingleScreen>().WithText("New design").AsMultiInstance())).Build();
		Form.RegisterBlock(Block);
		Assert.That(StartupScreen, Is.Not.Null);
		Assert.That(StartupScreen!.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(Form.ScreenHost.ActivateScreen(Block, typeof(DefaultSingleScreen)), Is.Not.SameAs(StartupScreen));
		Assert.That(Form.ScreenHost.OpenScreens, Has.Count.EqualTo(2));
	}

	[Test]
	public void RegisterBlockRejectsConflictsBeforeStartupActionsAndUserInterfaceChanges() {
		using var Form = new BlockMainForm { ScreenMode = ScreenMode.MultiView, Text = "Original" };
		using var Block = CreateConflictingBlock();
		var Executed = false;
		Block.AddMenu(new MenuBuilder().WithText("Startup").AddActionItem("Run", () => Executed = true, executeOnLoad: true).Build());
		Assert.That(() => Form.RegisterBlock(Block), Throws.ArgumentException);
		Assert.That(Executed, Is.False);
		Assert.That(Form.Text, Is.EqualTo("Original"));
		Assert.That(Form.RegisteredBlocks, Is.Empty);
		Assert.That(Form.PluginBindings, Is.Empty);
		Assert.That(Form.ActiveBlock, Is.Null);
		Assert.That(Form.ScreenHost.Screens, Is.Empty);
	}

	private static ApplicationBlock CreateBlock<TScreen>(ScreenActivationMode Mode) where TScreen : ApplicationScreen {
		return new ApplicationBlockBuilder().WithName("Declarations").AddMenu(Menu => Menu.WithText("Screens").ConfigureItem(Item => {
			var ScreenBuilder = Item.AsScreenItem().WithScreen<TScreen>().WithText("Open");
			if (Mode == ScreenActivationMode.SingleInstance)
				ScreenBuilder.AsSingleInstance();
			else
				ScreenBuilder.AsMultiInstance();
		})).Build();
	}

	private static ApplicationBlock CreateConflictingBlock() {
		return new ApplicationBlockBuilder().WithName("Conflict").AddMenu(Menu => Menu.WithText("Screens")
			.ConfigureItem(Item => Item.AsScreenItem().WithScreen<DefaultSingleScreen>().WithText("New design").AsMultiInstance())
			.ConfigureItem(Item => Item.AsScreenItem().WithScreen<DefaultMultiScreen>().WithText("Settings").AsSingleInstance())
			.ConfigureItem(Item => Item.AsScreenItem().WithScreen<DefaultMultiScreen>().WithText("Conflict").AsMultiInstance())).Build();
	}

	public class DefaultSingleScreen : ApplicationScreen {
	}

	public class DefaultMultiScreen : ApplicationScreen {
		public DefaultMultiScreen() => ActivationMode = ScreenActivationMode.MultiInstance;
	}

	public class OtherScreen : ApplicationScreen {
	}
}
