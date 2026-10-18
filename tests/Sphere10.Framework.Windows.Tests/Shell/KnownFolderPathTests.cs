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
		var shell = new ShellUtility();
		var documents = new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7");
		var before = shell.GetKnownFolderPath(documents);
		var path = await shell.GetDefaultKnownFolderPathAsync(documents);
		Assert.That(Path.IsPathFullyQualified(path), Is.True);
		Assert.That(shell.GetKnownFolderPath(documents), Is.EqualTo(before));
	}

	[TestCase("")]
	[TestCase("relative-directory")]
	[TestCase("C:\\bad\0path")]
	public void InvalidTargetsAreRejectedBeforeNativeMutation(string path) {
		Assert.That(() => new ShellUtility().SetKnownFolderPath(new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7"), path), Throws.ArgumentException);
		Assert.That(() => new ShellUtility().SetKnownFolderPath(Guid.Empty, "C:\\"), Throws.ArgumentException);
	}

	[Test]
	public void StaleExpectedPathRejectsRedirectionWithoutChangingTheWindowsMapping() {
		var shell = new ShellUtility();
		var documents = new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7");
		var before = shell.GetKnownFolderPath(documents);
		Assert.That(() => shell.SetKnownFolderPath(documents, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), before + Guid.NewGuid().ToString("N")),
			Throws.InvalidOperationException);
		Assert.That(shell.GetKnownFolderPath(documents), Is.EqualTo(before));
	}

	[Test]
	public async Task AsyncMutationRunsOnStaWorkerAndRetainsCompletionAfterDispatchCancellation() {
		using var cancellation = new CancellationTokenSource();
		var caller = Environment.CurrentManagedThreadId;
		var shell = new ObservedShell(() => cancellation.Cancel());
		await shell.SetKnownFolderPathAsync(Guid.NewGuid(), "C:\\Target", "C:\\Original", cancellation.Token);
		Assert.That(shell.ThreadId, Is.Not.EqualTo(caller));
		Assert.That(shell.Apartment, Is.EqualTo(ApartmentState.STA));
		Assert.That(shell.Completed, Is.True);
	}

	private sealed class ObservedShell : ShellUtility {
		private readonly Action _dispatched;
		public ObservedShell(Action dispatched) => _dispatched = dispatched;
		public int ThreadId { get; private set; }
		public ApartmentState Apartment { get; private set; }
		public bool Completed { get; private set; }
		public override void SetKnownFolderPath(Guid folderId, string path, string expectedPath = null) {
			ThreadId = Environment.CurrentManagedThreadId;
			Apartment = Thread.CurrentThread.GetApartmentState();
			_dispatched();
			Completed = true;
		}
	}
}
