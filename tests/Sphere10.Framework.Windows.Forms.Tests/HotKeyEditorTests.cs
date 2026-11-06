// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class HotKeyEditorTests {
	[Test]
	public void ShortcutRoundTripsThroughTheEditorAfterLoad() {
		using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-30000, -30000) };
		var editor = new HotKeyEditor { Shortcut = Keys.Control | Keys.Alt | Keys.L };
		form.Controls.Add(editor);
		form.Show();
		System.Windows.Forms.Application.DoEvents();

		Assert.That(editor.Shortcut, Is.EqualTo(Keys.Control | Keys.Alt | Keys.L));
		editor.Shortcut = Keys.Shift | Keys.F5;
		Assert.That(editor.Shortcut, Is.EqualTo(Keys.Shift | Keys.F5));
		editor.Shortcut = Keys.None;
		Assert.That(editor.Shortcut, Is.EqualTo(Keys.None));
	}

	[TestCase(Keys.A, true)]
	[TestCase(Keys.F11, true)]
	[TestCase(Keys.F12, false)]
	[TestCase(Keys.ControlKey, false)]
	[TestCase(Keys.LWin, false)]
	[TestCase(Keys.Control | Keys.A, false)]
	[TestCase(Keys.None, false)]
	public void AcceptsOnlyRegularKeys(Keys key, bool expected) {
		Assert.That(HotKeyEditor.IsEditableKey(key), Is.EqualTo(expected));
	}

	[TestCase(Keys.Control | Keys.Alt | Keys.L, "Ctrl+Alt+L")]
	[TestCase(Keys.Shift | Keys.F5, "Shift+F5")]
	[TestCase(Keys.None, "None")]
	public void DescribesShortcuts(Keys shortcut, string expected) {
		Assert.That(HotKeyEditor.Describe(shortcut), Is.EqualTo(expected));
	}
}
