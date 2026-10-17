// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class ShellUtilityTests {
	[Test]
	public void DiscoveryAndKnownFolderReadsUseExistingUtilityWithoutCreatingFolders() {
		Assert.That(Tools.Windows.Shell, Is.TypeOf<ShellUtility>());
		var profile = Tools.Windows.Shell.GetKnownFolderPath(new Guid("5e6c858f-0e22-4760-9afe-ea3317b67173"));
		Assert.That(Path.GetFullPath(profile), Is.EqualTo(Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))).IgnoreCase);
		Assert.That(() => Tools.Windows.Shell.GetKnownFolderPath(Guid.Empty), Throws.ArgumentException);
	}

	[Test]
	public void ReadOnlyRecycleBinQueryReturnsNonnegativeNativeTotals() {
		Assert.That(Marshal.SizeOf<WinAPI.SHELL32.SHQUERYRBINFO>(), Is.EqualTo(24));
		var root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
		var result = Tools.Windows.Shell.QueryRecycleBin(root);
		Assert.That(result.DriveRoot, Is.EqualTo(root).IgnoreCase);
		Assert.That(result.ItemCount, Is.GreaterThanOrEqualTo(0));
		Assert.That(result.SizeBytes, Is.GreaterThanOrEqualTo(0));
	}

	[TestCase("")]
	[TestCase("C:")]
	[TestCase("C:\\Windows")]
	[TestCase("\\\\server\\share\\")]
	public void InvalidRecycleBinTargetsAreRejectedBeforeQueryOrEmpty(string target) {
		Assert.That(() => Tools.Windows.Shell.QueryRecycleBin(target), Throws.ArgumentException);
		Assert.That(() => Tools.Windows.Shell.EmptyRecycleBin(target), Throws.ArgumentException);
	}

	[Test]
	public async Task CancelledRecycleBinQueryDoesNotEnterTheNativeOperation() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await new ShellUtility().QueryRecycleBinAsync(cancellationToken: cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public async Task ReadOnlyShellNamespaceEnumerationIsBoundedAndReturnsDetachedItems() {
		var result = await Tools.Windows.Shell.GetShellFolderItemsAsync("shell:RecycleBinFolder", maximumItems: 1);
		Assert.That(result.TotalCount, Is.GreaterThanOrEqualTo(0));
		Assert.That(result.Items.Length, Is.LessThanOrEqualTo(1));
		Assert.That(result.IsLimited, Is.EqualTo(result.TotalCount > 1));
		foreach (var item in result.Items) {
			Assert.That(item.Namespace, Is.EqualTo("shell:RecycleBinFolder"));
			Assert.That(item.Name, Is.Not.Null.And.Not.Empty);
			Assert.That(item.ParsingName, Is.Not.Null.And.Not.Empty);
			Assert.That(item.SizeBytes, Is.Null.Or.GreaterThanOrEqualTo(0));
		}
	}

	[Test]
	public async Task ShellNamespaceReadsRejectInvalidBoundsAndCancelledWorkBeforeNativeEnumeration() {
		foreach (var bound in new[] { 0, -1, 100001 })
			await Assert.ThatAsync(async () => await Tools.Windows.Shell.GetShellFolderItemsAsync("shell:RecycleBinFolder", bound), Throws.ArgumentException);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await Tools.Windows.Shell.GetShellFolderItemsAsync("shell:RecycleBinFolder", cancellationToken: cancellation.Token),
			Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public async Task StaleShellItemCannotResolveAnyCommands() {
		var stale = new WindowsShellItemInfo {
			Namespace = "shell:RecycleBinFolder", ParsingName = "C:\\$Recycle.Bin\\Sphere10-missing-fixture-" + Guid.NewGuid().ToString("N"), Name = "Missing fixture"
		};
		await Assert.ThatAsync(async () => await Tools.Windows.Shell.GetShellItemVerbsAsync(stale), Throws.InvalidOperationException);
	}

	[Test]
	public void ShellLaunchValidatesBeforeLaunchingAndExplorerArgumentsQuoteTheExistingPath() {
		Assert.That(() => new ShellUtility().Launch(" "), Throws.ArgumentException);
		Assert.That(() => new ShellUtility().Launch("unused.exe", windowStyle: (ProcessWindowStyle)123), Throws.ArgumentException);
		var directory = Path.Combine(Path.GetTempPath(), "Sphere10 shell fixture " + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var shell = new FixtureShell();
		shell.HighlightFileInExplorer(directory);
		Assert.That(Path.GetFileName(shell.Target), Is.EqualTo("explorer.exe").IgnoreCase);
		Assert.That(shell.Arguments, Is.EqualTo("/select,\"" + Path.GetFullPath(directory) + "\""));
	}

	private sealed class FixtureShell : ShellUtility {
		public string Target { get; private set; }
		public string Arguments { get; private set; }
		public override Process Launch(string target, string arguments = null, string workingDirectory = null, string verb = null,
			IntPtr ownerWindow = default, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal) {
			Target = target;
			Arguments = arguments;
			return null;
		}
	}
}
