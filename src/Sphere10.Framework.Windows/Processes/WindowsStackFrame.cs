// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.
namespace Sphere10.Framework.Windows;

/// <summary>Symbols may be unavailable; an unresolved instruction address remains useful evidence.</summary>
public sealed record WindowsStackFrame(ulong InstructionPointer, ulong StackPointer, ulong FramePointer, string ModuleName, string SymbolName, ulong Displacement, string SourceFile, uint? SourceLine) {
	public string ModulePath { get; init; }

	public ulong? ModuleBaseAddress { get; init; }
}
