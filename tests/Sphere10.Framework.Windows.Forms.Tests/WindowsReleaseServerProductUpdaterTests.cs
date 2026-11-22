// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.BITS;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>
/// The release server's Windows updater: an installation downloads and verifies the installer, starts it with the update's arguments and
/// owner window and answers that the application must exit; a failed download starts nothing; BITS states become installation stages;
/// and <c>UseProductUpdates</c> registers it unless the host chose another updater. The transfer and the process start are substituted,
/// so no BITS job, window or installer process is created.
/// </summary>
[TestFixture]
[NonParallelizable] // The registration cases share the module's process-wide named registrations.
public class WindowsReleaseServerProductUpdaterTests {
	private static readonly byte[] Content = Enumerable.Range(0, 30_000).Select(index => (byte)(index * 7)).ToArray();

	[Test]
	public async Task AnInstallationDownloadsVerifiesAndStartsTheInstallerAndThenRequiresTheApplicationToExit() {
		var updater = new SubstitutedUpdater();
		var result = await updater.InstallAsync(Update(), 4321);
		Assert.That(result, Is.EqualTo(ProductUpdateInstallResult.ExitRequired));
		Assert.That(updater.Events, Is.EqualTo(new[] { "download https://updates.example.com/files/setup.exe", "start setup.exe /quiet 4321", "disposed" }));
		Assert.That(Directory.Exists(updater.Folder), Is.True, "A started installer keeps its folder, since it outlives the application.");
		Tools.FileSystem.DeleteDirectory(updater.Folder, true);
	}

	[Test]
	public async Task AFailedDownloadStartsNothingAndRemovesItsFolder() {
		var updater = new SubstitutedUpdater { Tampered = true };
		await Assert.ThatAsync(() => updater.InstallAsync(Update()), Throws.InvalidOperationException.With.Message.Contains("SHA-256"));
		Assert.That(updater.Events, Is.EqualTo(new[] { "download https://updates.example.com/files/setup.exe", "disposed" }));
		Assert.That(Directory.Exists(updater.Folder), Is.False);
	}

	[Test]
	public async Task OnlyAReleaseServerUpdateIsInstalled() {
		var updater = new SubstitutedUpdater();
		await Assert.ThatAsync(() => updater.InstallAsync(new ProductUpdate { Version = ApplicationVersion.Parse("2.0.0.0") }), Throws.ArgumentException);
		Assert.That(updater.Events, Is.Empty);
	}

	[TestCase(JobState.Queued, ProductUpdateStage.Waiting)]
	[TestCase(JobState.Connecting, ProductUpdateStage.Connecting)]
	[TestCase(JobState.TransientError, ProductUpdateStage.Retrying)]
	[TestCase(JobState.Transferring, ProductUpdateStage.Downloading)]
	public void ABitsStateBecomesAnInstallationStage(JobState state, ProductUpdateStage stage)
		=> Assert.That(SubstitutedUpdater.Describe(100, 400, state), Is.EqualTo(new ProductUpdateProgress(stage, 100, 400)));

	[Test]
	public void AnUnknownTotalIsZero()
		=> Assert.That(SubstitutedUpdater.Describe(100, ulong.MaxValue, JobState.Transferring).BytesTotal, Is.Zero);

	[Test]
	public void UseProductUpdatesRegistersTheWindowsUpdaterUnlessTheHostChoseOne() {
		using (var provider = RegisterHost(builder => builder.UseProductUpdates())) {
			var updater = provider.GetRequiredService<ProductUpdaterBase>();
			Assert.That(updater, Is.TypeOf<WindowsReleaseServerProductUpdater>());
			Assert.That(provider.GetRequiredService<ProductUpdatePresenter>(), Is.Not.Null);
			Assert.That(provider.GetServices<ProductUpdaterBase>().Single(), Is.SameAs(updater), "The checker and the notice share one updater.");
		}
		// Checks alone, as a portable host enables them, get the Application module's updater, which finds but cannot install.
		using (var provider = RegisterHost(builder => builder.UseProductUpdateChecks()))
			Assert.That(provider.GetRequiredService<ProductUpdaterBase>(), Is.TypeOf<ReleaseServerProductUpdater>());
		// A host's own updater, such as a Store distribution's, is kept.
		var own = new SubstitutedUpdater();
		using (var provider = RegisterHost(builder => builder.AddServices(services => services.AddSingleton<ProductUpdaterBase>(own)).UseProductUpdates()))
			Assert.That(provider.GetServices<ProductUpdaterBase>().Single(), Is.SameAs(own));
	}

	private static ReleaseServerProductUpdate Update() => new() {
		Version = ApplicationVersion.Parse("2.0.0.0"),
		DownloadUrl = "https://updates.example.com/files/setup.exe",
		FileName = "setup.exe",
		Length = Content.Length,
		Hash = Hashers.Hash(CHF.SHA2_256, Content).ToHexString(true),
		InstallerArguments = "/quiet"
	};

	// Registers what a WinForms host with update checks registers, through the builder path Start() uses, without starting anything.
	private static ServiceProvider RegisterHost(Func<Sphere10FrameworkBuilder, Sphere10FrameworkBuilder> configure) {
		var builder = new Sphere10Framework()
			.BuildWinFormsApplication()
			.AddModule<Sphere10.Framework.Application.ModuleConfiguration>()
			.AddModule<Sphere10.Framework.Windows.Forms.ModuleConfiguration>();
		var services = new ServiceCollection();
		configure(builder).RegisterModules(services);
		return services.BuildServiceProvider();
	}

	#region Inner Types

	/// <summary>An updater whose installer writes the update's file instead of downloading it and records the start instead of running it.</summary>
	private sealed class SubstitutedUpdater : WindowsReleaseServerProductUpdater {

		public SubstitutedUpdater()
			: base(new NoOpProductUpdateService(), new FixedProduct()) {
		}

		public List<string> Events { get; } = new();

		public bool Tampered { get; set; }

		public string Folder { get; set; }

		public static ProductUpdateProgress Describe(ulong bytesTransferred, ulong bytesTotal, JobState state) => DescribeDownloadProgress(bytesTransferred, bytesTotal, state);

		protected override ProductUpdateInstaller CreateInstaller(ReleaseServerProductUpdate update) => new SubstitutedInstaller(update, this);

		private sealed class SubstitutedInstaller : ProductUpdateInstaller {
			private readonly SubstitutedUpdater _updater;

			public SubstitutedInstaller(ReleaseServerProductUpdate update, SubstitutedUpdater updater)
				: base(update, new ProductInformation { ProductName = "Example Product" }) {
				_updater = updater;
			}

			protected override async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken) {
				_updater.Events.Add($"download {url}");
				_updater.Folder = Path.GetDirectoryName(destinationPath);
				var content = (byte[])Content.Clone();
				if (_updater.Tampered)
					content[0] ^= 1;
				await File.WriteAllBytesAsync(destinationPath, content, cancellationToken);
			}

			protected override Task StartInstallerAsync(string fileName, string arguments, string workingDirectory, IntPtr ownerWindow, CancellationToken cancellationToken) {
				_updater.Events.Add($"start {Path.GetFileName(fileName)} {arguments} {ownerWindow}");
				return Task.CompletedTask;
			}

			protected override void DisposeInternal() {
				base.DisposeInternal();
				_updater.Events.Add("disposed");
			}
		}
	}

	private sealed class FixedProduct : IProductInformationProvider {
		public ProductInformation ProductInformation { get; } = new() { ProductName = "Example Product", ProductVersion = ApplicationVersion.Parse("1.0.0.0") };
	}

	#endregion
}
