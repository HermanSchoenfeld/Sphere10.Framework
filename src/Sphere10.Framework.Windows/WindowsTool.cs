// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Files;
using Sphere10.Framework.Windows.EventLogs;
using Sphere10.Framework.Windows.Networking;
using Sphere10.Framework.Windows.Security;
using Sphere10.Framework.Windows.Startup;
using Sphere10.Framework.Windows.Storage;

namespace Tools;

/// <summary>
/// Static facade for the Windows utilities. Each utility is a nested static class declared in its own domain folder, such as
/// <c>Tools.Windows.Files</c> in Files/FileUtil.cs. The four properties below are the utilities that own state and therefore stay instances,
/// one per accessing thread.
/// </summary>
public static partial class Windows {

	[ThreadStatic] private static ManagedAssemblyUtil _assemblies;
	public static ManagedAssemblyUtil Assemblies => _assemblies ??= new();

	[ThreadStatic] private static WindowsUiAutomationUtil _uiAutomation;
	public static WindowsUiAutomationUtil UiAutomation => _uiAutomation ??= new();

	[ThreadStatic] private static WindowsDumpUtil _dumps;
	public static WindowsDumpUtil Dumps => _dumps ??= new();

	[ThreadStatic] private static RegistryUtil _registry;
	public static RegistryUtil Registry => _registry ??= new();
}
