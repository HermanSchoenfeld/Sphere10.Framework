// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class KnownFolderPathTests {
	[Test]
	public async Task DefaultPathReadReturnsAnAbsoluteUserFolderWithoutChangingItsMapping() {
		var documents = new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7");
		var before = Tools.Windows.Shell.GetKnownFolderPath(documents);
		var path = await Tools.Windows.Shell.GetDefaultKnownFolderPathAsync(documents);
		Assert.That(Path.IsPathFullyQualified(path), Is.True);
		Assert.That(Tools.Windows.Shell.GetKnownFolderPath(documents), Is.EqualTo(before));
	}

	[TestCase("")]
	[TestCase("relative-directory")]
	[TestCase("C:\\bad\0path")]
	public void InvalidTargetsAreRejectedBeforeNativeMutation(string path) {
		Assert.That(() => Tools.Windows.Shell.SetKnownFolderPath(new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7"), path), Throws.ArgumentException);
		Assert.That(() => Tools.Windows.Shell.SetKnownFolderPath(Guid.Empty, "C:\\"), Throws.ArgumentException);
	}

	[Test]
	public void StaleExpectedPathRejectsRedirectionWithoutChangingTheWindowsMapping() {
		var documents = new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7");
		var before = Tools.Windows.Shell.GetKnownFolderPath(documents);
		Assert.That(() => Tools.Windows.Shell.SetKnownFolderPath(documents, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), before + Guid.NewGuid().ToString("N")),
			Throws.InvalidOperationException);
		Assert.That(Tools.Windows.Shell.GetKnownFolderPath(documents), Is.EqualTo(before));
	}
}
