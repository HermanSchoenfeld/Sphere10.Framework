// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>One explicitly authored native example, created only when selected.</summary>
public sealed class ControlCatalogEntry {

	public ControlCatalogEntry(Type controlType, string category, string instructions, Func<Control> createExample, bool opensWindow = false, Func<Control, Task> showWindow = null) {
		ControlType = controlType;
		Category = category;
		Instructions = instructions;
		CreateExample = createExample;
		OpensWindow = opensWindow;
		ShowWindow = showWindow;
	}

	public Type ControlType { get; }

	public string Category { get; }

	public string Instructions { get; }

	public Func<Control> CreateExample { get; }

	public bool OpensWindow { get; }

	public Func<Control, Task> ShowWindow { get; }

	public string ClassName => ControlType.FullName;

	public override string ToString() {
		return ControlType.Name + " — " + Category;
	}
}
