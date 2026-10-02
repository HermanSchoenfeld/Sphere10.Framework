// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;

namespace PackageConsumer.Desktop;

public sealed class PackageUsage : WinFormsApplicationScreen {
	public static IWinFormsApplicationPlugin CreatePlugin() => new WinFormsApplicationPluginBuilder()
		.WithName("Desktop plugin")
		.AddBlock(block => block.WithName("Desktop block").WithDefaultScreen<PackageUsage>(activationMode: ScreenActivationMode.PermanentSingleton))
		.Build();
}
