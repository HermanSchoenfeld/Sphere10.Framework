// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A version-independent registered-screen identity in the application's existing per-user settings store.</summary>
public sealed class ApplicationStartupScreenSettings : SettingsObject {
	public string PluginName { get; set; } = string.Empty;
	public string BlockName { get; set; } = string.Empty;
	public string ScreenTypeName { get; set; } = string.Empty;
	public string ScreenAssemblyName { get; set; } = string.Empty;
}
