// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class NamedPipeUtilTests {
	[TestCase("")]
	[TestCase("LOCAL\\")]
	public async Task PassiveEnumerationFindsAnOwnedPipeWithoutConnectingToItsServer(string pipeNamespace) {
		var name = pipeNamespace + "Sphere10-PassivePipe-" + Guid.NewGuid().ToString("N");
		using var server = new NamedPipeServerStream(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var waiting = server.WaitForConnectionAsync(cancellation.Token);
		var names = await Tools.Windows.Pipes.GetNamesAsync(cancellation.Token);
		Assert.That(Tools.Windows.Pipes, Is.TypeOf<NamedPipeUtil>());
		Assert.That(names, Does.Contain(name));
		Assert.That(names, Is.Ordered.Using<string>(StringComparer.OrdinalIgnoreCase));
		Assert.That(names.Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(names.Length));
		Assert.That(Tools.Windows.Pipes.GetPath(name), Is.EqualTo(@"\\.\pipe\" + name));
		Assert.That(server.IsConnected, Is.False, "Discovering names must not open or occupy the server's pipe instance.");
		Assert.That(waiting.IsCompleted, Is.False);
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await waiting, Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public async Task CancelledEnumerationDoesNotBeginNativeDiscovery() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await Tools.Windows.Pipes.GetNamesAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	[TestCase("")]
	[TestCase(" ")]
	[TestCase(@"\\.\pipe\supplied-path")]
	[TestCase("LOCAL\\..\\pipe")]
	[TestCase("LOCAL\\.\\pipe")]
	[TestCase("LOCAL\\\\pipe")]
	[TestCase("LOCAL\\")]
	[TestCase("C:\\pipe")]
	[TestCase("/rooted/pipe")]
	[TestCase("with\0nul")]
	public void InvalidLocalNamesCannotBeUsedAsPipePaths(string name) {
		Assert.That(() => Tools.Windows.Pipes.GetPath(name), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void TheWin32EntireNameLengthLimitIsEnforced() {
		Assert.That(Tools.Windows.Pipes.GetPath(new string('x', 256 - NamedPipeUtil.LocalNamespacePath.Length)).Length, Is.EqualTo(256));
		Assert.That(() => Tools.Windows.Pipes.GetPath(new string('x', 257 - NamedPipeUtil.LocalNamespacePath.Length)), Throws.InstanceOf<ArgumentException>());
	}
}

