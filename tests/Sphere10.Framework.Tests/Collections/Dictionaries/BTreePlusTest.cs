// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

/// <summary>
/// Abstract test harness for B+ tree implementations.
/// Inherits all common tests from <see cref="BTreeBaseTests"/> and adds B+ tree specific tests.
/// </summary>
public abstract class BTreePlusTests : BTreeBaseTests {

[Test]
public void Add_DuplicateKey_Throws() {
var Tree = CreateInstance<int, string>(3);
Tree.Add(1, "one");
Assert.That(() => Tree.Add(1, "duplicate"), Throws.InstanceOf<ArgumentException>());
}

[Test]
public void Set_NoOverwrite_Throws() {
var Tree = CreateInstance<int, string>(3);
Tree.Add(1, "one");
Assert.That(() => Tree.Set(1, "ONE", false), Throws.InstanceOf<ArgumentException>());
}
}
