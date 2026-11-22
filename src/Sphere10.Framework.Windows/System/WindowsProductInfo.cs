// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>The installed Windows product as its registry and firmware record it: name, edition, build and product keys.</summary>
/// <param name="ProductName">The registry's product name. Windows 11 keeps Windows 10's name there; compare <paramref name="BuildNumber"/> (22000 and later is Windows 11).</param>
/// <param name="EditionId">The installed edition, such as Professional.</param>
/// <param name="DisplayVersion">The feature update, such as 22H2, or null before Windows 10 version 20H2 recorded it.</param>
/// <param name="BuildNumber">The build and its update revision, such as 19045.6456.</param>
/// <param name="Installed">The key Windows was installed or last activated with, or null when the registry has none.</param>
/// <param name="Original">The key Windows kept from before an upgrade, or null when the registry has none.</param>
/// <param name="FirmwareKey">The key the manufacturer wrote into the firmware's ACPI MSDM table, or null when the firmware has none.</param>
public sealed record WindowsProductInfo(string ProductName, string EditionId, string DisplayVersion, string ProductId, string BuildNumber, bool Is64BitOperatingSystem,
	WindowsProductKeyInfo Installed, WindowsProductKeyInfo Original, string FirmwareKey);
