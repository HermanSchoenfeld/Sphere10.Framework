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

[TestFixture, NonParallelizable, Category("WindowsIntegration")]
public class BitsInteropTests {
	[Test, Apartment(ApartmentState.MTA), Explicit("Uses the Windows BITS service and an explicitly configured loopback fixture server.")]
	public async Task GeneratedComWrappersTransferFilesAndDeliverCompletionCallbacks() {
		var fixture = Environment.GetEnvironmentVariable("SPHERE10_BITS_TEST_URL");
		Assert.That(Uri.TryCreate(fixture, UriKind.Absolute, out var uri) && uri.IsLoopback, Is.True, "Supply a loopback fixture URL in SPHERE10_BITS_TEST_URL.");
		var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "BitsInterop-" + Guid.NewGuid().ToString("N"))).FullName;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		using var manager = new BitsManager();
		var failures = new ConcurrentQueue<string>();
		manager.OnInterfaceError += (_, error) => failures.Enqueue(error.Description);
		manager.EnumJobs(JobOwner.CurrentUser);
		using var job = manager.CreateJob("Sphere10 BITS test " + Guid.NewGuid().ToString("N"), JobType.Download);
		using var cancel = Tools.Scope.ExecuteOnDispose(() => {
			if (job.State != JobState.Acknowledged)
				job.Cancel();
		});
		var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		job.OnJobTransferredEvent += (_, _) => completed.TrySetResult();
		job.OnJobErrorEvent += (_, error) => completed.TrySetException(new IOException(error.Error.Description));
		job.Description = "Generated COM fixture 測試";
		job.Priority = JobPriority.ForeGround;
		job.NotificationFlags = NotificationFlags.JobTransferred | NotificationFlags.JobErrorOccured;
		var credentials = new BitsCredentials {
			AuthenticationTarget = AuthenticationTarget.Server,
			AuthenticationScheme = AuthenticationScheme.Ntlm,
			UserName = "FixtureUser",
			Password = "FixturePassword"
		};
		job.AddCredentials(credentials);
		job.RemoveCredentials(credentials);
		var destination = Path.Combine(directory, "download.bin");
		job.AddFile(fixture, destination);
		Assert.That(job.Description, Is.EqualTo("Generated COM fixture 測試"));
		using (var files = job.EnumFiles()) {
			Assert.That(files.Count, Is.EqualTo(1));
			Assert.That(files[0].LocalName, Is.EqualTo(destination));
		}
		job.Resume();
		await completed.Task.WaitAsync(TimeSpan.FromSeconds(30));
		Assert.That(job.State, Is.EqualTo(JobState.Transferred));
		job.Complete();
		Assert.That(job.State, Is.EqualTo(JobState.Acknowledged));
		Assert.That(File.ReadAllText(destination), Is.EqualTo("Sphere10 Native COM transfer fixture\n"));
		Assert.That(failures, Is.Empty);
	}
}
