// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>
/// Marks a dimension type as a GC root. Root dimension objects are user-managed and
/// will never be automatically collected by the ObjectSpace garbage collector.
/// Non-root dimension objects are only kept alive by incoming references from other objects;
/// when nothing references them, they become eligible for garbage collection.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class RootAttribute : Attribute {
}
