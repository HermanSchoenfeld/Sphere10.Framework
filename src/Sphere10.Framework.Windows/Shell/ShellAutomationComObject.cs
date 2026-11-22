// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Owns one Shell automation interface on its originating STA. It calls members by name through <see cref="DispatchComObject"/>,
/// which supports Native AOT. Derived wrappers expose the named members their callers use.
/// </summary>
internal abstract class ShellAutomationComObject : DispatchComObject {
	protected ShellAutomationComObject(IntPtr dispatch)
		: base(dispatch) {
	}

	protected override string Subject => "The Shell";
}

/// <summary>The Shell.Application automation root, created through <c>Tools.Windows.COM</c>.</summary>
internal sealed class ShellApplicationComObject(IntPtr dispatch) : ShellAutomationComObject(dispatch) {
	/// <summary>Opens a Shell namespace such as shell:RecycleBinFolder, or returns null when Windows cannot open it.</summary>
	public ShellFolderComObject OpenNamespace(string name) => GetObject("NameSpace", pointer => new ShellFolderComObject(pointer), name);
}

/// <summary>One Shell namespace folder.</summary>
internal sealed class ShellFolderComObject(IntPtr dispatch) : ShellAutomationComObject(dispatch) {
	/// <summary>Returns the folder's item collection, which the caller disposes.</summary>
	public ShellFolderItemsComObject GetItems() {
		var items = GetObject("Items", pointer => new ShellFolderItemsComObject(pointer));
		Guard.Ensure(items != null, "Windows returned no item collection for the Shell namespace.");
		return items;
	}
}

/// <summary>The items of one Shell namespace folder at the time of the query.</summary>
internal sealed class ShellFolderItemsComObject(IntPtr dispatch) : ShellAutomationComObject(dispatch) {
	public int Count => (int)GetValue("Count");

	/// <summary>Returns one item, which the caller disposes, or null when the Shell supplies none at that index.</summary>
	public ShellFolderItemComObject GetItem(int index) => GetObject("Item", pointer => new ShellFolderItemComObject(pointer), index);
}

/// <summary>One Shell item with its parsing path, display values, extended properties and commands.</summary>
internal sealed class ShellFolderItemComObject(IntPtr dispatch) : ShellAutomationComObject(dispatch) {
	public string Path => GetValue("Path") as string;

	public string Name => GetValue("Name") as string;

	public string Type => GetValue("Type") as string;

	public bool IsFolder => (bool)GetValue("IsFolder");

	/// <summary>Reads a property-system value by canonical name, such as System.Size.</summary>
	public object GetExtendedProperty(string name) => GetValue("ExtendedProperty", name);

	/// <summary>Returns the commands the Shell currently offers for this item, which the caller disposes.</summary>
	public ShellItemVerbsComObject GetVerbs() {
		var verbs = GetObject("Verbs", pointer => new ShellItemVerbsComObject(pointer));
		Guard.Ensure(verbs != null, "Windows returned no commands for the Shell item.");
		return verbs;
	}
}

/// <summary>The commands offered for one Shell item at the time of the query.</summary>
internal sealed class ShellItemVerbsComObject(IntPtr dispatch) : ShellAutomationComObject(dispatch) {
	public int Count => (int)GetValue("Count");

	/// <summary>Returns one command, which the caller disposes.</summary>
	public ShellItemVerbComObject GetItem(int index) {
		var verb = GetObject("Item", pointer => new ShellItemVerbComObject(pointer), index);
		Guard.Ensure(verb != null, "Windows returned no command at the requested position.");
		return verb;
	}
}

/// <summary>One localized Shell command of an item.</summary>
internal sealed class ShellItemVerbComObject(IntPtr dispatch) : ShellAutomationComObject(dispatch) {
	public string Name => GetValue("Name") as string;

	/// <summary>Requests the command. Windows owns any dialogs and the lifetime of the operation it starts.</summary>
	public void Invoke() => Call("DoIt");
}
