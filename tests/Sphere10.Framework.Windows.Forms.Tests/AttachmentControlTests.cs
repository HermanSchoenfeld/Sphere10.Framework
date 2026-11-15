// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class AttachmentControlTests {
	[Test]
	public void AttachmentOwnsItsBytesAndShowsOnlyTheFileName() {
		using var control = new AttachmentControl();
		byte[] bytes = [1, 2, 3];
		control.SetAttachment(@"D:\reports\report.txt", bytes);
		bytes[0] = 9;
		Assert.That(control.TryGetAttachment(out var name, out var attachment), Is.True);
		Assert.That(name, Is.EqualTo("report.txt"));
		Assert.That(attachment, Is.EqualTo(new byte[] { 1, 2, 3 }));
		attachment[1] = 9;
		Assert.That(control.TryGetAttachment(out _, out var fresh), Is.True);
		Assert.That(fresh, Is.EqualTo(new byte[] { 1, 2, 3 }));
		Assert.That(control.Controls.OfType<LinkLabel>().Single().Text, Is.EqualTo(name));
	}

	[Test]
	public void ClearingRemovesTheDisplayedFileAndRaisesTheChange() {
		using var control = new AttachmentControl();
		var changes = 0;
		control.AttachmentChanged += (_, _) => changes++;
		control.SetAttachment("empty.txt", []);
		Assert.That(control.HasAttachment, Is.True, "A zero-byte file is still an attachment.");
		control.ClearAttachment();
		Assert.That(control.HasAttachment, Is.False);
		Assert.That(control.TryGetAttachment(out _, out _), Is.False);
		Assert.That(control.FileName, Is.Empty);
		Assert.That(changes, Is.EqualTo(2));
	}
}
