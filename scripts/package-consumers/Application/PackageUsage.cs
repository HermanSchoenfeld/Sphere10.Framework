// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;

namespace PackageConsumer.Application;

public static class PackageUsage {
	public static IScreenActivationPolicyRegistry CreatePolicies() {
		var policies = new ScreenActivationPolicyRegistry();
		policies.RegisterInstance(typeof(Screen), ScreenActivationMode.SingleInstance);
		return policies;
	}

	public static IApplicationScreen CreateScreen() => new Screen();

	public static IApplicationBlock CreateBlock() {
		var block = new ApplicationBlock { Name = "Portable application", DefaultScreen = typeof(Screen) };
		var menu = new ApplicationMenu<ApplicationMenuItem> { Text = "Actions" };
		menu.AddItem(new ApplicationMenuItem { Title = "Refresh" });
		block.AddMenu(menu);
		return block;
	}

	public static IApplicationMenu[] CreateMenus() => CreateBlock().Menus;

	public static IApplicationMenuItem[] CreateItems() => CreateMenus()[0].Items;

	public static Type BlockContract => typeof(IApplicationBlock);

	private class Screen : IApplicationScreen {
	}
}
