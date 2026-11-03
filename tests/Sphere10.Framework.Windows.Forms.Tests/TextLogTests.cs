// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.TextBoxes;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class TextLogTests {
	[Test]
	public void ConstructionDoesNotRequireApplicationServicesOrBackgroundStorage() {
		using var log = new TextLog();
		Assert.That(log.Text, Is.Empty);
		Assert.That(log.Controls.OfType<TextBoxEx>().Single().Multiline, Is.True);
		Assert.That(log.SelectionStart, Is.Zero);
		Assert.That(log.SelectionLength, Is.Zero);
		Assert.That(log.SelectedText, Is.Empty);
	}

	[Test]
	public void TextAndChangesFollowTheNativeEditorInBothDirections() {
		using var form = CreateForm();
		using var log = new TextLog { Dock = DockStyle.Fill };
		form.Controls.Add(log);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		var editor = log.Controls.OfType<TextBoxEx>().Single();
		var changes = new List<string>();
		log.TextChanged += (_, _) => changes.Add(log.Text);
		log.Text = "initial";
		Assert.That(editor.Text, Is.EqualTo("initial"));
		editor.Text = "edited";
		Assert.That(log.Text, Is.EqualTo("edited"));
		log.Text = "edited";
		Assert.That(changes, Is.EqualTo(new[] { "initial", "edited" }));
	}

	[Test]
	public void SelectionReadsTheNativeSubstringAndRetainsNativeRangeValidation() {
		using var form = CreateForm();
		using var log = new TextLog { Text = "first second", Dock = DockStyle.Fill };
		form.Controls.Add(log);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		log.SelectionStart = 6;
		log.SelectionLength = 6;
		Assert.That(log.SelectedText, Is.EqualTo("second"));
		var editor = log.Controls.OfType<TextBoxEx>().Single();
		editor.Select(0, 5);
		Assert.That(log.SelectionStart, Is.Zero);
		Assert.That(log.SelectionLength, Is.EqualTo(5));
		Assert.That(log.SelectedText, Is.EqualTo("first"));
		Assert.That(() => log.SelectionStart = -1, Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(() => log.SelectionLength = -1, Throws.TypeOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public void AppendAddsAtTheEndWithoutReplacingTheSelectedText() {
		using var form = CreateForm();
		using var log = new TextLog { Text = "first", Dock = DockStyle.Fill };
		form.Controls.Add(log);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		log.SelectionStart = 0;
		log.SelectionLength = 5;
		log.AppendText("\r\nsecond");
		Assert.That(log.Text, Is.EqualTo("first\r\nsecond"));
		Assert.That(log.SelectionStart, Is.EqualTo(log.Text.Length));
		Assert.That(log.SelectionLength, Is.Zero);
		log.AppendText(null);
		log.AppendText(string.Empty);
		log.AppendText();
		Assert.That(log.Text, Is.EqualTo("first\r\nsecond"));
	}

	[Test]
	public void PropertyInspectionCanReadTheDisplayedTextAndSelection() {
		using var form = CreateForm();
		using var log = new TextLog { Text = "inspect this", Dock = DockStyle.Fill };
		using var properties = new PropertyGrid { Dock = DockStyle.Right, Width = 250 };
		form.Controls.Add(log);
		form.Controls.Add(properties);
		form.Show();
		System.Windows.Forms.Application.DoEvents();
		log.SelectionStart = 8;
		log.SelectionLength = 4;
		properties.SelectedObject = log;
		properties.Refresh();
		System.Windows.Forms.Application.DoEvents();
		Assert.That(properties.SelectedObject, Is.SameAs(log));
		var descriptors = TypeDescriptor.GetProperties(log);
		Assert.That(descriptors[nameof(TextLog.Text)].GetValue(log), Is.EqualTo("inspect this"));
		Assert.That(descriptors[nameof(TextLog.SelectionStart)].GetValue(log), Is.EqualTo(8));
		Assert.That(descriptors[nameof(TextLog.SelectionLength)].GetValue(log), Is.EqualTo(4));
		Assert.That(descriptors[nameof(TextLog.SelectedText)].GetValue(log), Is.EqualTo("this"));
	}

	private static Form CreateForm() {
		return new Form {
			StartPosition = FormStartPosition.Manual,
			Location = new Point(-19000, -19000),
			Size = new Size(700, 350),
			ShowInTaskbar = false
		};
	}
}
