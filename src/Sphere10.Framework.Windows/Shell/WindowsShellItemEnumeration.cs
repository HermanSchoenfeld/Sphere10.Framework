// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>Bounded detached Shell enumeration; TotalCount is the native collection's captured count.</summary>
public sealed record WindowsShellItemEnumeration(WindowsShellItemInfo[] Items, int TotalCount, bool IsLimited);
