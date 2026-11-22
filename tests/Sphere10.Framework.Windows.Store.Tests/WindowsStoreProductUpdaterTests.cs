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
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Windows.Services.Store;

namespace Sphere10.Framework.Windows.Store.Tests;

/// <summary>
/// The Windows Store updater's decisions over the Store's answers: which version it reports and when there is none, how the Store's
/// installation states and per-package statuses become the Framework's results and progress, and the module's registration. A subclass
/// answers for the Store, so no Store request is made; a real Store update needs a published package and is not exercised here.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WindowsStoreProductUpdaterTests {
	private const string Family = "Example.Product_8wekyb3d8bbwe";
	private const string OptionalFamily = "Example.Product.Content_8wekyb3d8bbwe";

	[Test]
	public async Task AnApplicationWithoutPackageIdentityHasNoUpdateAndTheStoreIsNotAsked() {
		var updater = new ScriptedUpdater { FamilyName = null };
		Assert.That(await updater.CheckForUpdateAsync(), Is.SameAs(ProductUpdateCheckResult.NoUpdate));
		Assert.That(updater.Calls, Is.Empty);
		Assert.That(updater.Infos.Single(), Does.Contain("no package identity"));
	}

	[Test]
	public async Task AStoreThatRefusesTheRequestMeansNoUpdateAndIsLogged() {
		// A sideloaded package, or a Store that cannot answer, is reported as not available and never as a failed check.
		var updater = new ScriptedUpdater { Query = Task.FromException<StorePackageUpdateInfo[]>(new InvalidOperationException("This package was not installed from the Store.")) };
		Assert.That(await updater.CheckForUpdateAsync(), Is.SameAs(ProductUpdateCheckResult.NoUpdate));
		Assert.That(updater.Calls, Is.EqualTo(new[] { "query" }));
		Assert.That(updater.Warnings.Single(), Does.Contain("not installed from the Store"));
	}

	[Test]
	public async Task ACancelledCheckIsNotReadAsNoUpdate() {
		var updater = new ScriptedUpdater { Query = Task.FromCanceled<StorePackageUpdateInfo[]>(new CancellationToken(true)) };
		await Assert.ThatAsync(() => updater.CheckForUpdateAsync(), Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public async Task NoPackageUpdateIsNoUpdate() {
		var updater = new ScriptedUpdater { Query = Task.FromResult(Array.Empty<StorePackageUpdateInfo>()) };
		Assert.That(await updater.CheckForUpdateAsync(), Is.SameAs(ProductUpdateCheckResult.NoUpdate));
		updater.Query = Task.FromResult<StorePackageUpdateInfo[]>(null);
		Assert.That(await updater.CheckForUpdateAsync(), Is.SameAs(ProductUpdateCheckResult.NoUpdate));
	}

	[Test]
	public async Task TheApplicationsOwnPackageNamesTheVersionAndAnyMandatoryPackageMakesTheUpdateMandatory() {
		var packages = new[] {
			new StorePackageUpdateInfo(OptionalFamily, ApplicationVersion.Parse("9.0.0.0"), true, null),
			new StorePackageUpdateInfo(Family.ToUpperInvariant(), ApplicationVersion.Parse("2.1.0.0"), false, null)
		};
		var updater = new ScriptedUpdater { Query = Task.FromResult(packages) };
		var update = (WindowsStoreProductUpdate)(await updater.CheckForUpdateAsync()).Update;
		Assert.That(update.Version, Is.EqualTo(ApplicationVersion.Parse("2.1.0.0")), "The family is compared without case.");
		Assert.That(update.IsMandatory, Is.True);
		Assert.That(update.ReleaseNotes, Is.Null, "The Store publishes no release notes to the application.");
		Assert.That(update.Packages, Is.EqualTo(packages), "Every package update is installed together.");
	}

	[Test]
	public async Task AnOfferOfOptionalPackagesAloneTakesTheirHighestVersion() {
		var updater = new ScriptedUpdater {
			Query = Task.FromResult(new[] {
				new StorePackageUpdateInfo(OptionalFamily, ApplicationVersion.Parse("1.2.0.0"), false, null),
				new StorePackageUpdateInfo(OptionalFamily + ".Extra", ApplicationVersion.Parse("1.10.0.0"), false, null)
			})
		};
		var update = (await updater.CheckForUpdateAsync()).Update;
		Assert.That((update.Version, update.IsMandatory), Is.EqualTo((ApplicationVersion.Parse("1.10.0.0"), false)));
	}

	[TestCase(StorePackageUpdateState.Completed, ProductUpdateInstallResult.Completed)]
	[TestCase(StorePackageUpdateState.Canceled, ProductUpdateInstallResult.Declined)]
	public async Task TheStoresOverallStateBecomesTheInstallResult(StorePackageUpdateState state, ProductUpdateInstallResult expected) {
		var updater = new ScriptedUpdater { Installed = state };
		using var cancellation = new CancellationTokenSource();
		Assert.That(await updater.InstallAsync(StoreUpdate(), 5678, cancellation.Token), Is.EqualTo(expected));
		Assert.That(updater.Calls, Is.EqualTo(new[] { $"install {Family} 5678" }), "The Store's dialogs are owned by the notice's window.");
		Assert.That(updater.LastToken, Is.EqualTo(cancellation.Token));
	}

	[TestCase(StorePackageUpdateState.ErrorLowBattery, "battery")]
	[TestCase(StorePackageUpdateState.ErrorWiFiRecommended, "Wi-Fi")]
	[TestCase(StorePackageUpdateState.ErrorWiFiRequired, "Wi-Fi")]
	[TestCase(StorePackageUpdateState.OtherError, "OtherError")]
	[TestCase(StorePackageUpdateState.Pending, "Pending")]
	public async Task AStoreThatDidNotInstallFailsWithItsReasonAndTheFailureIsLogged(StorePackageUpdateState state, string reason) {
		var updater = new ScriptedUpdater { Installed = state };
		await Assert.ThatAsync(() => updater.InstallAsync(StoreUpdate()), Throws.InvalidOperationException.With.Message.Contains(reason));
		Assert.That(updater.Errors.Single(), Does.Contain(reason));
	}

	[Test]
	public async Task OnlyAStoreUpdateWithPackagesIsInstalled() {
		var updater = new ScriptedUpdater();
		await Assert.ThatAsync(() => updater.InstallAsync(new ProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0") }), Throws.ArgumentException);
		await Assert.ThatAsync(() => updater.InstallAsync(new WindowsStoreProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0") }), Throws.ArgumentException);
		Assert.That(updater.Calls, Is.Empty);
	}

	[Test]
	public async Task ThePackagesStatusesAreSummedIntoOneProgress() {
		var updater = new ScriptedUpdater {
			Statuses = new[] {
				Status(Family, StorePackageUpdateState.Pending, 0, 0),
				Status(Family, StorePackageUpdateState.Downloading, 100, 1_000),
				Status(OptionalFamily, StorePackageUpdateState.Pending, 0, 500),
				Status(OptionalFamily, StorePackageUpdateState.Downloading, 200, 500),
				Status(Family, StorePackageUpdateState.Deploying, 1_000, 1_000),
				Status(OptionalFamily, StorePackageUpdateState.Completed, 500, 500),
				Status(Family, StorePackageUpdateState.Completed, 1_000, 1_000)
			}
		};
		var progress = new List<ProductUpdateProgress>();
		updater.InstallProgressChanged += progress.Add;
		await updater.InstallAsync(StoreUpdate());
		Assert.That(progress, Is.EqualTo(new[] {
			new ProductUpdateProgress(ProductUpdateStage.Waiting, 0, 0),
			new ProductUpdateProgress(ProductUpdateStage.Downloading, 100, 1_000),
			new ProductUpdateProgress(ProductUpdateStage.Downloading, 100, 1_500),
			new ProductUpdateProgress(ProductUpdateStage.Downloading, 300, 1_500),
			new ProductUpdateProgress(ProductUpdateStage.Installing, 1_200, 1_500),
			new ProductUpdateProgress(ProductUpdateStage.Installing, 1_500, 1_500),
			new ProductUpdateProgress(ProductUpdateStage.Installing, 1_500, 1_500)
		}));
	}

	[Test]
	public void TheModuleReplacesTheRegisteredUpdater() {
		var services = new ServiceCollection();
		// The release server's updater, as UseProductUpdates or the Application module registers it.
		services.AddSingleton<ProductUpdaterBase>(_ => new ReleaseServerProductUpdater(new NoOpProductUpdateService()));
		new ModuleConfiguration().RegisterComponents(services);
		new Sphere10.Framework.Application.ModuleConfiguration().RegisterComponents(services);
		using var provider = services.BuildServiceProvider();
		Assert.That(provider.GetServices<ProductUpdaterBase>().Single(), Is.TypeOf<WindowsStoreProductUpdater>(), "The Store's updater is the only one, and the Application module adds no default beside it.");
		var empty = new ServiceCollection();
		new ModuleConfiguration().RegisterComponents(empty);
		using var alone = empty.BuildServiceProvider();
		Assert.That(alone.GetRequiredService<ProductUpdaterBase>(), Is.TypeOf<WindowsStoreProductUpdater>());
	}

	[Test]
	public async Task ThisProcessHasNoPackageIdentitySoTheRealStoreIsNeverAsked() {
		// The test host is unpackaged, as a development run of a Store build is: the real updater reports no update instead of failing.
		Assert.That(await new WindowsStoreProductUpdater().CheckForUpdateAsync(), Is.SameAs(ProductUpdateCheckResult.NoUpdate));
	}

	private static WindowsStoreProductUpdate StoreUpdate() => new() {
		Version = ApplicationVersion.Parse("2.0.0.0"),
		Packages = new[] { new StorePackageUpdateInfo(Family, ApplicationVersion.Parse("2.0.0.0"), false, null) }
	};

	private static StorePackageUpdateStatus Status(string family, StorePackageUpdateState state, ulong downloaded, ulong size)
		=> new() { PackageFamilyName = family, PackageUpdateState = state, PackageBytesDownloaded = downloaded, PackageDownloadSizeInBytes = size };

	#region Inner Types

	/// <summary>An updater that answers for the Store from what the case gives it and records what it was asked.</summary>
	private sealed class ScriptedUpdater : WindowsStoreProductUpdater {
		private readonly ConcurrentQueue<string> _infos;
		private readonly ConcurrentQueue<string> _warnings;
		private readonly ConcurrentQueue<string> _errors;

		public ScriptedUpdater()
			: this(new ConcurrentQueue<string>(), new ConcurrentQueue<string>(), new ConcurrentQueue<string>()) {
		}

		private ScriptedUpdater(ConcurrentQueue<string> infos, ConcurrentQueue<string> warnings, ConcurrentQueue<string> errors)
			: base(new ActionLogger(_ => { }, infos.Enqueue, warnings.Enqueue, errors.Enqueue) { Options = LogOptions.VerboseProfile }) {
			_infos = infos;
			_warnings = warnings;
			_errors = errors;
		}

		public string FamilyName { get; set; } = Family;

		public Task<StorePackageUpdateInfo[]> Query { get; set; } = Task.FromResult(Array.Empty<StorePackageUpdateInfo>());

		public StorePackageUpdateState Installed { get; set; } = StorePackageUpdateState.Completed;

		public StorePackageUpdateStatus[] Statuses { get; set; } = Array.Empty<StorePackageUpdateStatus>();

		public List<string> Calls { get; } = new();

		public CancellationToken LastToken { get; private set; }

		public string[] Infos => _infos.ToArray();

		public string[] Warnings => _warnings.ToArray();

		public string[] Errors => _errors.ToArray();

		protected override string GetPackageFamilyName() => FamilyName;

		protected override Task<StorePackageUpdateInfo[]> GetStorePackageUpdatesAsync(CancellationToken cancellationToken) {
			Calls.Add("query");
			return Query;
		}

		protected override Task<StorePackageUpdateState> RequestDownloadAndInstallAsync(StorePackageUpdateInfo[] packages, nint ownerWindow, IProgress<StorePackageUpdateStatus> progress, CancellationToken cancellationToken) {
			Calls.Add($"install {string.Join(",", packages.Select(package => package.PackageFamilyName))} {ownerWindow}");
			LastToken = cancellationToken;
			foreach (var status in Statuses)
				progress.Report(status);
			return Task.FromResult(Installed);
		}
	}

	#endregion
}
