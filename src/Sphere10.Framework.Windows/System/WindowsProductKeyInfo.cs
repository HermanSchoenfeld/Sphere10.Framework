// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>One product key Windows keeps in the registry, with the product ID, edition and channel recorded beside it.</summary>
/// <param name="Key">The decoded key, or null when Windows keeps none, as after a volume (MAK or KMS) activation, which clears it.</param>
/// <param name="Edition">The edition the key is for, such as Professional.</param>
/// <param name="Channel">How the key was distributed, such as Retail, OEM:DM or Volume:MAK.</param>
public sealed record WindowsProductKeyInfo(string Key, string ProductId, string Edition, string Channel);
