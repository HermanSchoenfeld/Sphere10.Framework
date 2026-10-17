// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.Win32;

namespace Sphere10.Framework.Windows;

/// <summary>Identifies a local registry key without navigation or presentation projections.</summary>
public readonly record struct RegistryAddress(RegistryHive Hive, RegistryView View, string SubKeyPath);
