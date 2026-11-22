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

namespace Sphere10.Framework.Application.Tests.Updates;

/// <summary>
/// The release server's portable updater: it answers what the application's update service answers, refuses an offer that does not
/// name its installer, passes the token to the service and cannot install by itself.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ReleaseServerProductUpdaterTests {

	[Test]
	public async Task TheServicesAnswerIsTheCheckResult() {
		var update = new ReleaseServerProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0"), DownloadUrl = "https://updates.example.com/setup.exe", ReleaseNotes = "Faster." };
		var service = new FixedService(ProductUpdateCheckResult.Available(update));
		var result = await new ReleaseServerProductUpdater(service).CheckForUpdateAsync();
		Assert.That(result.Update, Is.SameAs(update));
		Assert.That(await new ReleaseServerProductUpdater(new FixedService(ProductUpdateCheckResult.NoUpdate)).CheckForUpdateAsync(), Is.SameAs(ProductUpdateCheckResult.NoUpdate));
		Assert.That(await new ReleaseServerProductUpdater(new FixedService(null)).CheckForUpdateAsync(), Is.SameAs(ProductUpdateCheckResult.NoUpdate));
		Assert.That(await new ReleaseServerProductUpdater(new NoOpProductUpdateService()).CheckForUpdateAsync(), Is.SameAs(ProductUpdateCheckResult.NoUpdate));
	}

	[Test]
	public async Task AnOfferThatDoesNotNameItsInstallerIsAFailedCheck() {
		var withoutAddress = new ReleaseServerProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0"), DownloadUrl = " " };
		var withoutInstaller = new ProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0") };
		foreach (var offer in new ProductUpdate[] { withoutAddress, withoutInstaller }) {
			var updater = new ReleaseServerProductUpdater(new FixedService(ProductUpdateCheckResult.Available(offer)));
			await Assert.ThatAsync(() => updater.CheckForUpdateAsync(), Throws.InvalidOperationException.With.Message.Contains("download address"));
		}
	}

	[Test]
	public async Task TheTokenReachesTheService() {
		var service = new FixedService(ProductUpdateCheckResult.NoUpdate);
		using var cancellation = new CancellationTokenSource();
		await new ReleaseServerProductUpdater(service).CheckForUpdateAsync(cancellation.Token);
		Assert.That(service.LastToken, Is.EqualTo(cancellation.Token));
	}

	[Test]
	public async Task ThePortableUpdaterCannotInstall() {
		var updater = new ReleaseServerProductUpdater(new FixedService(ProductUpdateCheckResult.NoUpdate));
		var update = new ReleaseServerProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0"), DownloadUrl = "https://updates.example.com/setup.exe" };
		await Assert.ThatAsync(() => updater.InstallAsync(update), Throws.InstanceOf<NotSupportedException>());
		Assert.That(() => new ReleaseServerProductUpdater(null), Throws.ArgumentNullException);
	}

	#region Inner Types

	private sealed class FixedService : IProductUpdateService {
		private readonly ProductUpdateCheckResult _answer;

		public FixedService(ProductUpdateCheckResult answer) => _answer = answer;

		public CancellationToken LastToken { get; private set; }

		public Task<ProductUpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default) {
			LastToken = cancellationToken;
			return Task.FromResult(_answer);
		}
	}

	#endregion
}
