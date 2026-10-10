// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ActionMenuOwnerTests {
	private bool _startedFramework;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance
				.Build()
				.Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ActionReceivesTheExecutingMainFormThroughBothBuilderPaths(bool shortcut) {
		using var form = new BlockMainForm();
		IUserInterfaceServices receivedUI = null;
		var calls = new List<string>();
		Action<IUserInterfaceServices> action = ui => {
			receivedUI = ui;
			calls.Add("action");
		};
		var builder = new WinFormsApplicationMenuBuilder()
			.WithText("Actions");
		if (shortcut)
			builder.AddActionItem("Run", action);
		else
			builder
				.ConfigureItem(item => item
					.AsActionItem()
					.WithText("Run")
					.WithAction(action)
				);
		using var menu = builder.Build();
		var item = (WinFormsActionMenuItem)menu.Items[0];
		item.Select += () => calls.Add("notification");

		form.ExecuteMenuItem(item);

		Assert.That(receivedUI, Is.SameAs(form));
		Assert.That(receivedUI.PrimaryUIController, Is.SameAs(form));
		Assert.That(receivedUI.PrimaryUIController, Is.InstanceOf<IWin32Window>());
		Assert.That(calls, Is.EqualTo(new[] { "action", "notification" }));
	}

	[Test]
	public void ExecuteOnLoadReceivesTheRegisteringMainForm() {
		using var form = new BlockMainForm();
		IUserInterfaceServices receivedUI = null;
		var actions = 0;
		var notifications = 0;
		using var block = new WinFormsApplicationBlockBuilder()
			.WithName("Operations")
			.ConfigureMenu(menu => menu
				.WithText("Actions")
				.AddActionItem("Startup", ui => {
					receivedUI = ui;
					actions++;
				}, executeOnLoad: true)
			)
			.Build();
		var item = (WinFormsActionMenuItem)block.Menus[0].Items[0];
		item.Select += () => notifications++;
		Assert.That(actions, Is.Zero);

		form.RegisterBlock(block);

		Assert.That(form.IsBlockRegistered(block), Is.True);
		Assert.That(receivedUI, Is.SameAs(form));
		Assert.That(receivedUI.PrimaryUIController, Is.SameAs(form));
		Assert.That(receivedUI.PrimaryUIController, Is.InstanceOf<IWin32Window>());
		Assert.That(actions, Is.EqualTo(1));
		Assert.That(notifications, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void WithActionReplacesThePreviousCallbackKind(bool ownerAwareLast) {
		using var form = new BlockMainForm();
		IUserInterfaceServices receivedUI = null;
		var ownerAwareCalls = 0;
		var parameterlessCalls = 0;
		Action<IUserInterfaceServices> ownerAwareAction = ui => {
			receivedUI = ui;
			ownerAwareCalls++;
		};
		Action parameterlessAction = () => parameterlessCalls++;
		var builder = new WinFormsApplicationMenuItemBuilder()
			.AsActionItem()
			.WithText("Run");
		if (ownerAwareLast)
			builder
				.WithAction(parameterlessAction)
				.WithAction(ownerAwareAction);
		else
			builder
				.WithAction(ownerAwareAction)
				.WithAction(parameterlessAction);
		using var item = builder.Build();

		form.ExecuteMenuItem(item);

		Assert.That(ownerAwareCalls, Is.EqualTo(ownerAwareLast ? 1 : 0));
		Assert.That(parameterlessCalls, Is.EqualTo(ownerAwareLast ? 0 : 1));
		if (ownerAwareLast)
			Assert.That(receivedUI, Is.SameAs(form));
		else
			Assert.That(receivedUI, Is.Null);
	}

	[Test]
	public void ContextualSelectionInvokesTheActionAndNotifiesOnce() {
		using var form = new BlockMainForm();
		IUserInterfaceServices receivedUI = null;
		var actions = 0;
		var notifications = 0;
		using var item = new WinFormsActionMenuItem("Run", ui => {
			receivedUI = ui;
			actions++;
		});
		item.Select += () => notifications++;

		((IWinFormsLinkMenuItem)item).OnSelect(form);

		Assert.That(receivedUI, Is.SameAs(form));
		Assert.That(actions, Is.EqualTo(1));
		Assert.That(notifications, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ImageConstructorsRetainTheirCallbacks(bool ownerAware) {
		using var form = new BlockMainForm();
		IUserInterfaceServices receivedUI = null;
		var actions = 0;
		var notifications = 0;
		var image = new Bitmap(16, 16);
		using var item = ownerAware
			? new WinFormsActionMenuItem("Run", image, ui => {
				receivedUI = ui;
				actions++;
			})
			: new WinFormsActionMenuItem("Run", image, () => actions++);
		item.Select += () => notifications++;

		form.ExecuteMenuItem(item);

		Assert.That(item.Image16x16, Is.SameAs(image));
		Assert.That(actions, Is.EqualTo(1));
		Assert.That(notifications, Is.EqualTo(1));
		if (ownerAware)
			Assert.That(receivedUI, Is.SameAs(form));
		else
			Assert.That(receivedUI, Is.Null);
	}

	[Test]
	public void ExistingCustomLinkUsesTheDefaultContextualSelectionMethod() {
		using var form = new BlockMainForm();
		using var item = new LegacyLinkMenuItem();
		var notifications = 0;
		item.Select += () => notifications++;

		form.ExecuteMenuItem(item);

		Assert.That(item.SelectionCount, Is.EqualTo(1));
		Assert.That(notifications, Is.EqualTo(1));
	}

	private sealed class LegacyLinkMenuItem : WinFormsApplicationMenuItemBase, IWinFormsLinkMenuItem {
		public string Text => "Legacy link";

		public int SelectionCount { get; private set; }

		public void OnSelect() {
			SelectionCount++;
			NotifySelect();
		}
	}
}
