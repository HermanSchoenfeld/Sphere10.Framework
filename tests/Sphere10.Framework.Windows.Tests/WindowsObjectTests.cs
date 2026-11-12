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
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WindowsObjectTests {
	[Test]
	public async Task ActualNtRootAndSymbolicLinkAreReadWithoutFollowingOrModifyingObjects() {
		var root = await Tools.Windows.Objects.ReadDirectoryAsync(@"\");
		Assert.That(root.IsPartial, Is.False);
		Assert.That(root.Entries, Is.Not.Empty);
		Assert.That(root.Entries.All(entry => entry.Path == "\\" + entry.Name), Is.True);
		Assert.That(root.Entries.Any(entry => entry.Type == "Directory"), Is.True);
		var link = root.Entries.FirstOrDefault(entry => entry.Type == "SymbolicLink");
		Assert.That(link, Is.Not.Null, "The Windows NT root supplies symbolic links such as GLOBAL??.");
		Assert.That(await Tools.Windows.Objects.ReadSymbolicLinkAsync(link.Path), Is.Not.Empty);
		var bounded = await Tools.Windows.Objects.ReadDirectoryAsync(@"\", maximumEntries: 1);
		Assert.That(bounded.Entries, Has.Length.EqualTo(1));
		Assert.That(bounded.IsPartial, Is.True);
	}

	[Test]
	public async Task RetainedTextBudgetTruncatesActualNativeReadIndependentlyOfRowCount() {
		var result = await Tools.Windows.Objects.ReadDirectoryAsync(@"\", maximumEntries: 4096, maximumCharacters: 64);
		Assert.That(result.IsPartial, Is.True);
		Assert.That(result.LimitReason, Is.EqualTo("Retained Text Limit Reached"));
		Assert.That(Tools.Windows.Objects.CountRetainedCharacters(result), Is.LessThanOrEqualTo(64));
		Assert.That(result.Entries.Length, Is.LessThan(4096));
		var noRowFits = await Tools.Windows.Objects.ReadDirectoryAsync(@"\", maximumEntries: 4096, maximumCharacters: 1);
		Assert.That(noRowFits.Entries, Is.Empty);
		Assert.That(noRowFits.IsPartial, Is.True, "The text limit must not be presented as an empty complete namespace.");
		Assert.That(() => Tools.Windows.Objects.ReadDirectoryAsync(@"\", 4096, 0), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public async Task CanceledAndInvalidQueriesDoNotReturnInventedEmptyDirectories() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await Tools.Windows.Objects.ReadDirectoryAsync(@"\", cancellationToken: cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(() => Tools.Windows.Objects.ReadDirectoryAsync("Relative"), Throws.InstanceOf<ArgumentException>());
		await Assert.ThatAsync(async () => await Tools.Windows.Objects.ReadDirectoryAsync("\\Sphere10-Owned-Missing-" + Guid.NewGuid().ToString("N")), Throws.InstanceOf<System.ComponentModel.Win32Exception>());
	}

	[Test]
	public void DecodedRightsPreserveTypeSemanticsAndUnknownBits() {
		Assert.That(Tools.Windows.Objects.DecodeAccessMask("File", 0x00120001), Does.Contain("Read Data").And.Contain("Read Control").And.Contain("Synchronize"));
		Assert.That(Tools.Windows.Objects.DecodeAccessMask("Key", 1), Does.Contain("Query Value").And.Not.Contain("Read Data"));
		Assert.That(Tools.Windows.Objects.DecodeAccessMask("Event", 0x4002), Does.Contain("Modify State").And.Contain("Unknown 0x00004000"));
		Assert.That(Tools.Windows.Objects.DecodeAccessMask("Unknown Future Type", 1), Is.EqualTo("Unknown 0x00000001"));
	}
}
