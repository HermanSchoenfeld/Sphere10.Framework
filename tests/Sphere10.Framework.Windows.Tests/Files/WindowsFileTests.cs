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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Files;

namespace Sphere10.Framework.Windows.Tests.Files;

[TestFixture]
[NonParallelizable]
public class WindowsFileTests {
	private string _root;
	private string _source;
	private string _destination;

	[SetUp]
	public void SetUp() {
		_root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Sphere10NativeCopy-" + Guid.NewGuid().ToString("N"))).FullName;
		_source = Path.Combine(_root, "source.bin");
		_destination = Path.Combine(_root, "destination.bin");
		File.WriteAllBytes(_source, Enumerable.Range(0, 1024 * 1024).Select(index => (byte)index).ToArray());
	}

	[TearDown]
	public void TearDown() => Directory.Delete(_root, true);

	[TestCase(false)]
	[TestCase(true)]
	public async Task SingleFileCopyPreservesBytesAndReportsNativeAggregateProgress(bool asynchronous) {
		var samples = new List<WindowsFileCopyProgress>();
		var checkpoints = 0;
		if (asynchronous)
			await WindowsFile.CopyAsync(_source, _destination, progress: samples.Add, checkpoint: () => checkpoints++);
		else
			WindowsFile.Copy(_source, _destination, progress: samples.Add, checkpoint: () => checkpoints++);
		Assert.That(File.ReadAllBytes(_destination), Is.EqualTo(File.ReadAllBytes(_source)));
		Assert.That(samples, Is.Not.Empty);
		Assert.That(samples.Last().TotalBytes, Is.EqualTo(new FileInfo(_source).Length));
		Assert.That(samples.Last().TransferredBytes, Is.EqualTo(samples.Last().TotalBytes));
		Assert.That(checkpoints, Is.GreaterThan(samples.Count), "The checkpoint also runs before native work begins.");
	}

	[Test]
	public void FailIfExistsPreservesTheExistingDestination() {
		File.WriteAllText(_destination, "Keep existing destination");
		Assert.That(() => WindowsFile.Copy(_source, _destination, overwrite: false), Throws.TypeOf<Win32Exception>());
		Assert.That(File.ReadAllText(_destination), Is.EqualTo("Keep existing destination"));
	}

	[Test]
	public async Task CancellationBeforeCopyNeverOverwritesTheDestination() {
		File.WriteAllText(_destination, "Keep existing destination");
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await WindowsFile.CopyAsync(_source, _destination, cancellationToken: cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(File.ReadAllText(_destination), Is.EqualTo("Keep existing destination"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CallbackFailureOrCancellationIsRethrownAfterNativeCopyReturns(bool cancel) {
		using var cancellation = new CancellationTokenSource();
		var failure = new InvalidOperationException("Owned native callback failure");
		var reports = 0;
		void Report(WindowsFileCopyProgress _) {
			reports++;
			if (cancel)
				cancellation.Cancel();
			else
				throw failure;
		}
		if (cancel)
			await Assert.ThatAsync(async () => await WindowsFile.CopyAsync(_source, _destination, cancellationToken: cancellation.Token, progress: Report), Throws.InstanceOf<OperationCanceledException>());
		else
			Assert.That(Assert.Throws<InvalidOperationException>(() => WindowsFile.Copy(_source, _destination, progress: Report)), Is.SameAs(failure));
		Assert.That(reports, Is.GreaterThan(0));
		Assert.That(File.Exists(_destination), Is.False, "PROGRESS_CANCEL discards the partial destination.");
		Assert.That(new FileInfo(_source).Length, Is.EqualTo(1024 * 1024));
	}
}
