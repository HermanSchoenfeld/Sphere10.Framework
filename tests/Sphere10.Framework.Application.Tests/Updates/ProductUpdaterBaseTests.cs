// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests.Updates;

/// <summary>
/// The updater template: each public operation guards its arguments and calls its hook once, a missing answer is no update, one
/// installation runs at a time, a failed installation is logged and rethrown while a cancelled one is not logged, and progress reaches
/// both the hook and the event.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ProductUpdaterBaseTests {
	private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

	[Test]
	public async Task ACheckCallsTheFetchHookWithTheCallersToken() {
		var update = new ProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0") };
		var updater = new ScriptedUpdater { Answer = ProductUpdateCheckResult.Available(update) };
		using var cancellation = new CancellationTokenSource();
		var result = await updater.CheckForUpdateAsync(cancellation.Token);
		Assert.That(result.Update, Is.SameAs(update));
		Assert.That(updater.Calls, Is.EqualTo(new[] { "fetch" }));
		Assert.That(updater.LastToken, Is.EqualTo(cancellation.Token));
	}

	[Test]
	public async Task AMissingAnswerIsNoUpdate() {
		var updater = new ScriptedUpdater { Answer = null };
		Assert.That(await updater.CheckForUpdateAsync(), Is.SameAs(ProductUpdateCheckResult.NoUpdate));
	}

	[Test]
	public async Task ACancelledCheckOrInstallationNeverReachesItsHook() {
		var updater = new ScriptedUpdater();
		using var cancelled = new CancellationTokenSource();
		cancelled.Cancel();
		await Assert.ThatAsync(() => updater.CheckForUpdateAsync(cancelled.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => updater.InstallAsync(new ProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0") }, cancellationToken: cancelled.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(updater.Calls, Is.Empty);
	}

	[Test]
	public async Task AnInstallationCallsTheInstallHookWithTheUpdateTheWindowAndTheToken() {
		var update = new ProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0") };
		var updater = new ScriptedUpdater { Installed = ProductUpdateInstallResult.Completed };
		using var cancellation = new CancellationTokenSource();
		Assert.That(await updater.InstallAsync(update, 1234, cancellation.Token), Is.EqualTo(ProductUpdateInstallResult.Completed));
		Assert.That(updater.Calls, Is.EqualTo(new[] { "install 2.0.0.0 1234" }));
		Assert.That(updater.LastToken, Is.EqualTo(cancellation.Token));
		Assert.That(() => updater.InstallAsync(null), Throws.ArgumentNullException);
	}

	[Test]
	public async Task OneInstallationRunsAtATime() {
		var release = new TaskCompletionSource<ProductUpdateInstallResult>(TaskCreationOptions.RunContinuationsAsynchronously);
		var updater = new ScriptedUpdater { Installation = release.Task };
		var update = new ProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0") };
		var first = updater.InstallAsync(update);
		await Assert.ThatAsync(() => updater.InstallAsync(update), Throws.InvalidOperationException);
		release.SetResult(ProductUpdateInstallResult.ExitRequired);
		Assert.That(await first.WaitAsync(Deadline), Is.EqualTo(ProductUpdateInstallResult.ExitRequired));
		updater.Installation = null;
		Assert.That(await updater.InstallAsync(update), Is.EqualTo(ProductUpdateInstallResult.Declined), "A finished installation admits the next one.");
		Assert.That(updater.Calls, Has.Count.EqualTo(2));
	}

	[Test]
	public async Task AFailedInstallationIsLoggedAndRethrownAndACancelledOneIsNotLogged() {
		var errors = new ConcurrentQueue<string>();
		var failure = new InvalidOperationException("The installer could not be started.");
		var updater = new ScriptedUpdater(new ActionLogger(_ => { }, _ => { }, _ => { }, errors.Enqueue)) { Installation = Task.FromException<ProductUpdateInstallResult>(failure) };
		var update = new ProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0") };
		await Assert.ThatAsync(() => updater.InstallAsync(update), Throws.Exception.SameAs(failure));
		Assert.That(errors, Has.Count.EqualTo(1));
		Assert.That(errors.Single(), Does.Contain("2.0.0.0").And.Contain(failure.Message));
		updater.Installation = Task.FromCanceled<ProductUpdateInstallResult>(new CancellationToken(true));
		await Assert.ThatAsync(() => updater.InstallAsync(update), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(errors, Has.Count.EqualTo(1), "The user cancelled; nothing failed.");
		updater.Installation = null;
		Assert.That(await updater.InstallAsync(update), Is.EqualTo(ProductUpdateInstallResult.Declined), "A failed installation does not block the next one.");
	}

	[Test]
	public void ProgressReachesTheHookAndThenTheEvent() {
		var updater = new ScriptedUpdater();
		var observed = new List<string>();
		updater.InstallProgressChanged += progress => observed.Add($"event {progress.Stage} {progress.BytesTransferred}/{progress.BytesTotal}");
		updater.Report(new ProductUpdateProgress(ProductUpdateStage.Downloading, 10, 40));
		Assert.That(updater.Calls, Is.EqualTo(new[] { "hook Downloading 10/40" }));
		Assert.That(observed, Is.EqualTo(new[] { "event Downloading 10/40" }));
	}

	#region Inner Types

	/// <summary>An updater whose hooks record their calls and answer what the case gives them.</summary>
	private sealed class ScriptedUpdater : ProductUpdaterBase {

		public ScriptedUpdater(ILogger logger = null)
			: base(logger) {
		}

		public List<string> Calls { get; } = new();

		public CancellationToken LastToken { get; private set; }

		public ProductUpdateCheckResult Answer { get; set; } = ProductUpdateCheckResult.NoUpdate;

		/// <summary>The task an installation returns; when null it answers <see cref="Installed"/>.</summary>
		public Task<ProductUpdateInstallResult> Installation { get; set; }

		public ProductUpdateInstallResult Installed { get; set; } = ProductUpdateInstallResult.Declined;

		public void Report(ProductUpdateProgress progress) => NotifyInstallProgressChanged(progress);

		protected override Task<ProductUpdateCheckResult> FetchUpdateAsync(CancellationToken cancellationToken) {
			Calls.Add("fetch");
			LastToken = cancellationToken;
			return Task.FromResult(Answer);
		}

		protected override Task<ProductUpdateInstallResult> InstallUpdateAsync(ProductUpdate update, nint ownerWindow, CancellationToken cancellationToken) {
			Calls.Add($"install {update.Version} {ownerWindow}");
			LastToken = cancellationToken;
			return Installation ?? Task.FromResult(Installed);
		}

		protected override void OnInstallProgressChanged(ProductUpdateProgress progress) => Calls.Add($"hook {progress.Stage} {progress.BytesTransferred}/{progress.BytesTotal}");
	}

	#endregion
}
