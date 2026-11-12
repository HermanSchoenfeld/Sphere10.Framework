// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class ShellRecycleItemTests {
	[Test]
	public void SelectedRecycleDeletionRejectsAnOrdinaryNamespaceBeforeNativeDispatch() {
		var item = new WindowsShellItemInfo { Namespace = "shell:Desktop", ParsingName = "not-a-recycle-item", Name = "test" };
		Assert.That(() => Tools.Windows.Shell.DeleteRecycleBinItem(item), Throws.ArgumentException);
		Assert.That(() => Tools.Windows.Shell.DeleteRecycleBinItem(null), Throws.ArgumentNullException);
	}

	[Test]
	public async Task CanceledDeletionDoesNotInspectOrInvokeAnyRecycleItem() {
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		var item = new WindowsShellItemInfo { Namespace = "shell:RecycleBinFolder", ParsingName = "not-an-existing-item", Name = "test" };
		await Assert.ThatAsync(() => Tools.Windows.Shell.DeleteRecycleBinItemAsync(item, cancellationToken: canceled.Token), Throws.InstanceOf<OperationCanceledException>());
	}
}

