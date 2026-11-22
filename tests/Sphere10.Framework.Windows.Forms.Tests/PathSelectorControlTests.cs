// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>The path selector's existence check for a typed path, which runs on a worker so that a slow share cannot freeze the window.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class PathSelectorControlTests {

	[Test]
	public async Task ATypedPathIsCheckedOnAWorkerAndOnlyAnExistingOneIsReported() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(500, 100) };
		using var selector = new PathSelectorControl { Mode = PathSelectionMode.Folder, ForcePathExists = true, Dock = DockStyle.Top };
		form.Controls.Add(selector);
		form.Show();
		var changes = 0;
		selector.PathChanged += () => changes++;

		// A missing path: leaving the box does not wait for the check, and nothing is reported once it has answered.
		var missing = Path.Combine(folder, "Missing");
		var leave = Type(selector, missing);
		Assert.That(leave.Cancel, Is.False, "The focus moves on while the path is checked.");
		await selector.ValidationCompletion;
		Assert.That((selector.IsPathMissing, changes), Is.EqualTo((true, 0)));

		// Leaving again checks again, and a path that exists is reported once it is found.
		Type(selector, missing);
		await selector.ValidationCompletion;
		Assert.That(selector.IsPathMissing, Is.True, "Leaving the box again checks the missing path again.");
		Type(selector, folder);
		await selector.ValidationCompletion;
		Assert.That((selector.IsPathMissing, changes, selector.Path), Is.EqualTo((false, 1, folder)));

		// A path set from code wins over a check still running.
		Type(selector, missing);
		var pending = selector.ValidationCompletion;
		selector.Path = folder;
		await pending;
		Assert.That((selector.IsPathMissing, changes, selector.Path), Is.EqualTo((false, 2, folder)));
		// The missing path returned the focus to the box, so closing the form leaves it and checks it once more; the test waits for that check.
		form.Close();
		await selector.ValidationCompletion;
	}

	// Types a path into the box and leaves it, as focus moving to another control does.
	private static CancelEventArgs Type(PathSelectorControl selector, string path) {
		var textBox = selector.Controls.Find("_filenameTextBox", true).OfType<TextBoxEx>().Single();
		Invoke(selector, "_filenameTextBox_Enter", EventArgs.Empty);
		textBox.Text = path;
		var leave = new CancelEventArgs();
		Invoke(selector, "_filenameTextBox_Validating", leave);
		return leave;
	}

	private static void Invoke(PathSelectorControl selector, string handler, EventArgs args)
		=> typeof(PathSelectorControl).GetMethod(handler, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(selector, new object[] { selector, args });
}
