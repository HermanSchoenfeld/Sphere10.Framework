// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Files;

public sealed record PortableExecutableSection(string Name, uint RelativeVirtualAddress, uint VirtualSize, uint FileOffset, uint RawSize, uint Characteristics);

/// <summary>A parsed structural entry. FileOffset is absent when an address is virtual/unbacked or not applicable.</summary>
public sealed record PortableExecutableEntry(string Kind, string Name, uint? RelativeVirtualAddress, long? FileOffset, long Size, string Value);

public sealed record ManagedAssemblyEntry(int Token, int ParentToken, string Kind, string Owner, string Name, string Detail);

public sealed record ManagedAssemblyReference(int SourceToken, int TargetToken, string Kind, string Source, int? IlOffset);

/// <summary>An inert named entry inside an embedded .resources container; values are not instantiated.</summary>
public sealed record ManagedResourceEntry(int ResourceToken, string Name, string DataType, int Size, bool IsBaml);
