// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.BITS;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, NonParallelizable]
public class BitsFileDownloadTests {
	private const string Address = "https://updates.example.com/files/setup.exe";

	[Test]
	public void ADownloadDescribesItsJobWithoutCreatingOne() {
		var destination = Path.Combine(Path.GetTempPath(), "BitsFileDownload-" + Guid.NewGuid().ToString("N"), "setup.exe");
		var download = new BitsFileDownload("Example Update", Address, destination);
		Assert.That(download.Name, Is.EqualTo("Example Update"));
		Assert.That(download.Url, Is.EqualTo(Address));
		Assert.That(download.DestinationPath, Is.EqualTo(destination));
		Assert.That(download.JobType, Is.EqualTo(JobType.Download));
		Assert.That(download.Owner, Is.EqualTo(JobOwner.CurrentUser));
		Assert.That(download.DeleteOnFailure, Is.True);
		Assert.That(download.Priority, Is.EqualTo(JobPriority.ForeGround));
		Assert.That(download.NoProgressTimeout, Is.EqualTo(TimeSpan.FromMinutes(2)));
		Assert.That(download.UnderlyingJob, Is.Null);
		Assert.That(Directory.Exists(Path.GetDirectoryName(destination)), Is.False, "Construction touches neither BITS nor the file system.");
	}

	[TestCase(null, Address, @"C:\Temp\setup.exe")]
	[TestCase(" ", Address, @"C:\Temp\setup.exe")]
	[TestCase("Example Update", null, @"C:\Temp\setup.exe")]
	[TestCase("Example Update", "files/setup.exe", @"C:\Temp\setup.exe")]
	[TestCase("Example Update", "ftp://updates.example.com/setup.exe", @"C:\Temp\setup.exe")]
	[TestCase("Example Update", @"C:\Temp\source.exe", @"C:\Temp\setup.exe")]
	[TestCase("Example Update", Address, null)]
	[TestCase("Example Update", Address, " ")]
	public void InvalidArgumentsAreRejected(string name, string url, string destinationPath)
		=> Assert.That(() => new BitsFileDownload(name, url, destinationPath), Throws.InstanceOf<ArgumentException>());

	[Test]
	public void ACancelledTokenEndsTheDownloadBeforeAnyJobIsCreated() {
		var destination = Path.Combine(Path.GetTempPath(), "BitsFileDownload-" + Guid.NewGuid().ToString("N"), "setup.exe");
		var download = new BitsFileDownload("Example Update", Address, destination);
		using var cancelled = new CancellationTokenSource();
		cancelled.Cancel();
		var transfer = download.DownloadAsync(cancelled.Token);
		Assert.That(transfer.IsCanceled, Is.True);
		Assert.That(download.UnderlyingJob, Is.Null);
		Assert.That(Directory.Exists(Path.GetDirectoryName(destination)), Is.False);
	}

	[TestCase(0, 500)]
	[TestCase(-5, 500)]
	[TestCase(120_000, 0)]
	public void AnInvalidTimeoutOrPollIntervalIsRejectedBeforeAnyJobIsCreated(int noProgressMilliseconds, int pollMilliseconds) {
		var destination = Path.Combine(Path.GetTempPath(), "BitsFileDownload-" + Guid.NewGuid().ToString("N"), "setup.exe");
		var download = new BitsFileDownload("Example Update", Address, destination) {
			NoProgressTimeout = TimeSpan.FromMilliseconds(noProgressMilliseconds),
			PollInterval = TimeSpan.FromMilliseconds(pollMilliseconds)
		};
		Assert.That(() => { _ = download.DownloadAsync(); }, Throws.InvalidOperationException);
		Assert.That(download.UnderlyingJob, Is.Null);
		Assert.That(Directory.Exists(Path.GetDirectoryName(destination)), Is.False);
	}

	[Test, Apartment(ApartmentState.MTA), Category("WindowsIntegration"), Explicit("Uses the Windows BITS service against a loopback port that nothing listens on.")]
	public async Task ADownloadThatMakesNoProgressFailsWithAnExplanationAndLeavesNoJobOrFile() {
		var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "BitsFileDownload-" + Guid.NewGuid().ToString("N"))).FullName;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var destination = Path.Combine(directory, "stalled.bin");
		// Port 9 on the loopback address refuses connections, which BITS treats as a transient error and retries.
		var download = new BitsFileDownload("Sphere10 BITS download test " + Guid.NewGuid().ToString("N"), "http://127.0.0.1:9/stalled.bin", destination) {
			PollInterval = TimeSpan.FromMilliseconds(100),
			NoProgressTimeout = TimeSpan.FromSeconds(3)
		};
		var states = new ConcurrentQueue<JobState>();
		download.ProgressChanged += (_, state) => states.Enqueue(state);
		await Assert.ThatAsync(async () => await download.DownloadAsync().WaitAsync(TimeSpan.FromSeconds(30)), Throws.InstanceOf<BitsDownloadException>().With.Message.Contains("no progress"));
		Assert.That(states, Is.Not.Empty, "The waiting states are reported while nothing is transferred.");
		Assert.That(File.Exists(destination), Is.False);
		Assert.That(download.UnderlyingJob.State, Is.EqualTo(JobState.Cancelled));
	}

	[Test, Apartment(ApartmentState.MTA), Category("WindowsIntegration"), Explicit("Uses the Windows BITS service and an explicitly configured loopback fixture server.")]
	public async Task TheFixtureIsDownloadedToItsDestinationAndTheJobIsAcknowledged() {
		var fixture = Environment.GetEnvironmentVariable("SPHERE10_BITS_TEST_URL");
		Assert.That(Uri.TryCreate(fixture, UriKind.Absolute, out var uri) && uri.IsLoopback, Is.True, "Supply a loopback fixture URL in SPHERE10_BITS_TEST_URL.");
		var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "BitsFileDownload-" + Guid.NewGuid().ToString("N"))).FullName;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		// The download creates the missing folder and replaces a file already at its destination.
		var destination = Path.Combine(directory, "nested", "download.bin");
		var download = new BitsFileDownload("Sphere10 BITS download test " + Guid.NewGuid().ToString("N"), fixture, destination) { PollInterval = TimeSpan.FromMilliseconds(100) };
		var reported = new ConcurrentQueue<ulong>();
		download.ProgressChanged += (progress, _) => reported.Enqueue(progress.BytesTransferred);
		await download.DownloadAsync().WaitAsync(TimeSpan.FromSeconds(30));
		Assert.That(File.ReadAllText(destination), Is.EqualTo("Sphere10 Native COM transfer fixture\n"));
		Assert.That(download.UnderlyingJob.State, Is.EqualTo(JobState.Acknowledged));
		Assert.That(() => { _ = download.DownloadAsync(); }, Throws.InvalidOperationException, "A download runs once.");
	}

	[Test, Apartment(ApartmentState.MTA), Category("WindowsIntegration"), Explicit("Uses the Windows BITS service and an explicitly configured loopback fixture server.")]
	public async Task AMissingRemoteFileFailsTheDownloadAndLeavesNoFile() {
		var fixture = Environment.GetEnvironmentVariable("SPHERE10_BITS_TEST_URL");
		Assert.That(Uri.TryCreate(fixture, UriKind.Absolute, out var uri) && uri.IsLoopback, Is.True, "Supply a loopback fixture URL in SPHERE10_BITS_TEST_URL.");
		var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "BitsFileDownload-" + Guid.NewGuid().ToString("N"))).FullName;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var destination = Path.Combine(directory, "missing.bin");
		var missing = new Uri(uri, "/missing-" + Guid.NewGuid().ToString("N")).AbsoluteUri;
		var download = new BitsFileDownload("Sphere10 BITS download test " + Guid.NewGuid().ToString("N"), missing, destination) { PollInterval = TimeSpan.FromMilliseconds(100) };
		await Assert.ThatAsync(async () => await download.DownloadAsync().WaitAsync(TimeSpan.FromSeconds(30)), Throws.InstanceOf<BitsDownloadException>());
		Assert.That(File.Exists(destination), Is.False);
	}
}
