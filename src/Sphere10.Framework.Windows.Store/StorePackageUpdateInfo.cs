// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application;
using Windows.Services.Store;

namespace Sphere10.Framework.Windows.Store;

/// <summary>
/// One package update the Store offered: the package family, its new version, whether the publisher marked it mandatory, and the
/// Store's own update object that an installation hands back to the Store. <see cref="WindowsStoreProductUpdater"/> reads it from the
/// Store; a test supplies it without one.
/// </summary>
public sealed record StorePackageUpdateInfo(string PackageFamilyName, ApplicationVersion Version, bool IsMandatory, StorePackageUpdate StoreUpdate);
