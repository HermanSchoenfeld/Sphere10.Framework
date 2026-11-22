// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Security;

/// <summary>
/// One user profile registered on this computer, with the registry hive file (NTUSER.DAT) that holds the user's settings.
/// A profile whose user is signed in has its hive loaded at HKEY_USERS\<see cref="Sid"/>; any other hive is only a file.
/// </summary>
/// <param name="Sid">The profile owner's security identifier, or null for the template that new profiles are copied from.</param>
/// <param name="AccountName">The owner's account as DOMAIN\Name, or null when Windows can no longer resolve the identifier.</param>
/// <param name="ProfilePath">The profile folder, with environment variables expanded.</param>
/// <param name="HivePath">The full path of the profile's NTUSER.DAT.</param>
/// <param name="IsLoaded">Whether Windows has the hive loaded, because the user is signed in or a service runs as them.</param>
/// <param name="IsSystemAccount">Whether the profile belongs to Local System, Local Service or Network Service.</param>
/// <param name="HiveLength">The hive file's length in bytes, or null when the file cannot be read.</param>
/// <param name="HiveLastWriteUtc">When the hive file was last written, or null when the file cannot be read.</param>
/// <param name="Error">Why the hive file's details are unavailable, or null.</param>
public sealed record WindowsUserProfile(string Sid, string AccountName, string ProfilePath, string HivePath, bool IsLoaded, bool IsSystemAccount, long? HiveLength,
	DateTime? HiveLastWriteUtc, string Error);
