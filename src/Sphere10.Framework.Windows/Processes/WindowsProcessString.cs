// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>Address is a virtual address for memory scans and a file offset for image scans.</summary>
public sealed record WindowsProcessString(ulong Address, string Value, bool IsUnicode, bool IsTruncated = false);

