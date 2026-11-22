// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests.Updates;

/// <summary>The update description's check answers and the release-server update's refusal of a downloaded file that is not the declared installer.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ProductUpdateTests {
	private static readonly byte[] Installer = Enumerable.Range(0, 100_000).Select(index => (byte)(index * 31)).ToArray();

	[Test]
	public void AFileThatMatchesTheDeclaredHashAndLengthIsAccepted() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory();
		using (Tools.Scope.DeleteDirOnDispose(folder)) {
			var path = WriteInstaller(folder, Installer);
			var update = CreateUpdate(Hashers.Hash(CHF.SHA2_256, Installer).ToHexString(true), Installer.Length);
			Assert.That(update.VerifyFile(path).IsSuccess, Is.True);
			update.Hash = update.Hash.ToUpperInvariant();
			update.Length = 0;
			Assert.That(update.VerifyFile(path).IsSuccess, Is.True, "The hash is case-insensitive and an undeclared length is not checked.");
		}
	}

	[Test]
	public void AFileWithAnotherHashIsRefused() {
		var tampered = (byte[])Installer.Clone();
		tampered[^1] ^= 1;
		var folder = Tools.FileSystem.GetTempEmptyDirectory();
		using (Tools.Scope.DeleteDirOnDispose(folder)) {
			var path = WriteInstaller(folder, tampered);
			var result = CreateUpdate(Hashers.Hash(CHF.SHA2_256, Installer).ToHexString(true), Installer.Length).VerifyFile(path);
			Assert.That(result.IsFailure, Is.True);
			Assert.That(result.ErrorMessages.Single(), Does.Contain("SHA-256"));
		}
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("not a hash")]
	[TestCase("a1b2c3")]
	public void AnUpdateWithoutAValidHashIsRefused(string hash) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory();
		using (Tools.Scope.DeleteDirOnDispose(folder)) {
			var path = WriteInstaller(folder, Installer);
			var result = CreateUpdate(hash, Installer.Length).VerifyFile(path);
			Assert.That(result.IsFailure, Is.True, "A file that cannot be verified is never accepted.");
		}
	}

	[Test]
	public void AFileOfAnotherLengthIsRefused() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory();
		using (Tools.Scope.DeleteDirOnDispose(folder)) {
			var path = WriteInstaller(folder, Installer);
			var result = CreateUpdate(Hashers.Hash(CHF.SHA2_256, Installer).ToHexString(true), Installer.Length + 1).VerifyFile(path);
			Assert.That(result.IsFailure, Is.True);
			Assert.That(result.ErrorMessages.Single(), Does.Contain("bytes"));
		}
	}

	[Test]
	public void AMissingFileIsRefused() {
		var update = CreateUpdate(Hashers.Hash(CHF.SHA2_256, Installer).ToHexString(true), Installer.Length);
		var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "setup.exe");
		Assert.That(update.VerifyFile(missing).IsFailure, Is.True);
		Assert.That(() => update.VerifyFile(" "), Throws.ArgumentException);
	}

	[Test]
	public async Task TheAwaitedVerificationGivesTheSameAnswerAndHonoursACancelledToken() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory();
		using (Tools.Scope.DeleteDirOnDispose(folder)) {
			var path = WriteInstaller(folder, Installer);
			var update = CreateUpdate(Hashers.Hash(CHF.SHA2_256, Installer).ToHexString(true), Installer.Length);
			Assert.That((await update.VerifyFileAsync(path)).IsSuccess, Is.True);
			update.Hash = new string('0', 64);
			Assert.That((await update.VerifyFileAsync(path)).IsFailure, Is.True);
			using var cancelled = new CancellationTokenSource();
			cancelled.Cancel();
			await Assert.ThatAsync(async () => await update.VerifyFileAsync(path, cancelled.Token), Throws.InstanceOf<OperationCanceledException>());
		}
	}

	[Test]
	public async Task AnOpenFileIsVerifiedFromItsStartThroughTheCallersOwnStream() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory();
		using (Tools.Scope.DeleteDirOnDispose(folder)) {
			var path = WriteInstaller(folder, Installer);
			var update = CreateUpdate(Hashers.Hash(CHF.SHA2_256, Installer).ToHexString(true), Installer.Length);
			using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				file.Position = 1234;
				Assert.That(update.VerifyFile(file).IsSuccess, Is.True, "The whole file is hashed wherever the stream stood.");
				Assert.That((await update.VerifyFileAsync(file)).IsSuccess, Is.True);
				Assert.That(file.CanRead, Is.True, "The caller keeps its stream, which holds the verified file until it is used.");
				update.Hash = new string('0', 64);
				Assert.That(update.VerifyFile(file).IsFailure, Is.True);
				update.Hash = null;
				Assert.That(update.VerifyFile(file).IsFailure, Is.True);
			}
			Assert.That(() => update.VerifyFile((Stream)null), Throws.ArgumentNullException);
			using var writeOnly = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
			Assert.That(() => update.VerifyFile(writeOnly), Throws.ArgumentException);
		}
	}

	[Test]
	public void ACheckAnswerIsEitherNoUpdateOrAnUpdateThatNamesItsVersion() {
		var update = CreateUpdate(new string('0', 64), 1);
		var available = ProductUpdateCheckResult.Available(update);
		Assert.That(available.IsUpdateAvailable, Is.True);
		Assert.That(available.Update, Is.SameAs(update));
		Assert.That(ProductUpdateCheckResult.NoUpdate.IsUpdateAvailable, Is.False);
		Assert.That(ProductUpdateCheckResult.NoUpdate.Update, Is.Null);
		Assert.That(() => ProductUpdateCheckResult.Available(null), Throws.ArgumentNullException);
		Assert.That(() => ProductUpdateCheckResult.Available(new ReleaseServerProductUpdate { DownloadUrl = update.DownloadUrl }), Throws.ArgumentException);
		// A channel without an installer of its own, such as the Windows Store, offers the version alone. The release server's
		// updater refuses such an offer from its server (ReleaseServerProductUpdaterTests).
		var storeLike = new ProductUpdate { Version = update.Version, IsMandatory = true };
		Assert.That(ProductUpdateCheckResult.Available(storeLike).Update, Is.SameAs(storeLike));
		Assert.That(storeLike.ReleaseNotes, Is.Null);
	}

	[Test]
	public async Task TheDefaultServiceNeverFindsAnUpdate() {
		var service = new NoOpProductUpdateService();
		Assert.That(await service.CheckForUpdateAsync(), Is.SameAs(ProductUpdateCheckResult.NoUpdate));
		using var cancelled = new CancellationTokenSource();
		cancelled.Cancel();
		await Assert.ThatAsync(async () => await service.CheckForUpdateAsync(cancelled.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	private static ReleaseServerProductUpdate CreateUpdate(string hash, long length) => new() {
		Version = ApplicationVersion.Parse("2.0.0.0"),
		DownloadUrl = "https://updates.example.com/setup.exe",
		FileName = "setup.exe",
		Hash = hash,
		Length = length
	};

	private static string WriteInstaller(string folder, byte[] content) {
		var path = Path.Combine(folder, "setup.exe");
		Tools.FileSystem.WriteAllBytes(path, content);
		return path;
	}
}
