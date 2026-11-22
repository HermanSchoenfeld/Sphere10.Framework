// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Files;

/// <summary>A parsed structural entry. FileOffset is absent when an address is virtual/unbacked or not applicable.</summary>
public sealed record PortableExecutableEntry(string Kind, string Name, uint? RelativeVirtualAddress, long? FileOffset, long Size, string Value);
