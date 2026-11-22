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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.BITS;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>
/// The update installer's file-name checks, download, verification, hold on the verified file, launch command and clean-up. The
/// transfer and the process start are substituted, so no BITS job, window or installer process is created.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ProductUpdateInstallerTests {
	private static readonly byte[] Content = Enumerable.Range(0, 50_000).Select(index => (byte)(index * 17)).ToArray();
	private static readonly string ContentHash = Hashers.Hash(CHF.SHA2_256, Content).ToHexString(true);

	[Test]
	public async Task AVerifiedInstallerIsKeptInItsOwnTemporaryFolderUntilTheInstallerIsDisposed() {
		string folder;
		using (var installer = new SubstitutedInstaller(CreateUpdate(ContentHash))) {
			var installerPath = await installer.DownloadAsync();
			folder = Path.GetDirectoryName(installerPath);
			Assert.That(installerPath, Is.EqualTo(installer.DestinationPath));
			Assert.That(installer.InstallerPath, Is.EqualTo(installerPath));
			Assert.That(installer.Url, Is.EqualTo(installer.Update.DownloadUrl));
			Assert.That(Path.GetFileName(installerPath), Is.EqualTo("setup.exe"));
			Assert.That(Path.GetDirectoryName(folder), Is.EqualTo(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)));
			Assert.That(File.Exists(installerPath), Is.True);
			await Assert.ThatAsync(async () => await installer.DownloadAsync(), Throws.InvalidOperationException, "A verified installer is not downloaded again.");
		}
		Assert.That(Directory.Exists(folder), Is.False, "An installer that was never started is removed with its folder.");
	}

	[Test]
	public async Task AnInstallerThatDoesNotMatchTheHashIsRefusedRemovedAndLogged() {
		var tampered = (byte[])Content.Clone();
		tampered[0] ^= 1;
		using var installer = new SubstitutedInstaller(CreateUpdate(ContentHash)) { Transfer = (path, _) => File.WriteAllBytesAsync(path, tampered) };
		await Assert.ThatAsync(async () => await installer.DownloadAsync(), Throws.InvalidOperationException.With.Message.Contains("SHA-256"));
		Assert.That(Directory.Exists(Path.GetDirectoryName(installer.DestinationPath)), Is.False, "A refused installer is not left where it could be run.");
		Assert.That(installer.InstallerPath, Is.Null);
		Assert.That(installer.Errors.Single(), Does.Contain("SHA-256"));
	}

	[Test]
	public async Task AFailedDownloadIsLoggedRemovedAndCanBeTriedAgain() {
		var failure = new BitsDownloadException("The server returned 404.");
		using var installer = new SubstitutedInstaller(CreateUpdate(ContentHash)) { Transfer = (_, _) => Task.FromException(failure) };
		await Assert.ThatAsync(async () => await installer.DownloadAsync(), Throws.Exception.SameAs(failure));
		Assert.That(Directory.Exists(Path.GetDirectoryName(installer.DestinationPath)), Is.False);
		Assert.That(installer.Errors.Single(), Does.Contain("404"));
		installer.Transfer = (path, _) => File.WriteAllBytesAsync(path, Content);
		Assert.That(File.Exists(await installer.DownloadAsync()), Is.True);
	}

	[Test]
	public async Task ACancelledDownloadRemovesItsFolderWithoutBeingLogged() {
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var installer = new SubstitutedInstaller(CreateUpdate(ContentHash)) {
			Transfer = async (_, token) => {
				started.TrySetResult();
				await Task.Delay(Timeout.Infinite, token);
			}
		};
		using var cancellation = new CancellationTokenSource();
		var download = installer.DownloadAsync(cancellation.Token);
		await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await download, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(Directory.Exists(Path.GetDirectoryName(installer.DestinationPath)), Is.False);
		Assert.That(installer.Errors, Is.Empty);
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase(" ")]
	[TestCase(".")]
	[TestCase("..")]
	[TestCase(@"..\..\evil.exe")]
	[TestCase("sub/evil.exe")]
	[TestCase(@"C:\Windows\evil.exe")]
	[TestCase("setup.exe:stream")]
	[TestCase("name:stream.exe")]
	[TestCase("NUL.exe")]
	[TestCase("con.msi")]
	[TestCase("COM1.exe")]
	[TestCase("lpt9.setup.exe")]
	[TestCase("setup.exe.")]
	[TestCase("setup.exe ")]
	[TestCase("setup?.exe")]
	[TestCase("setup")]
	[TestCase("setup.bat")]
	[TestCase("setup.cmd")]
	[TestCase("setup.ps1")]
	[TestCase("setup.exe.txt")]
	public async Task AnInstallerFileNameThatIsNotAPlainExeOrMsiNameIsRefusedBeforeAnythingIsDownloaded(string fileName) {
		var update = CreateUpdate(ContentHash);
		update.FileName = fileName;
		using var installer = new SubstitutedInstaller(update);
		await Assert.ThatAsync(async () => await installer.DownloadAsync(), Throws.InvalidOperationException.With.Message.Contains("installer"));
		Assert.That(installer.DestinationPath, Is.Null, "Nothing is downloaded for a refused name.");
		Assert.That(installer.Errors, Has.Length.EqualTo(1), "The refusal is logged.");
	}

	[TestCase("setup.exe")]
	[TestCase("Setup.MSI")]
	[TestCase("Example Product 2.0 Setup.exe")]
	public async Task APlainExeOrMsiNameIsAccepted(string fileName) {
		var update = CreateUpdate(ContentHash);
		update.FileName = fileName;
		using var installer = new SubstitutedInstaller(update);
		Assert.That(Path.GetFileName(await installer.DownloadAsync()), Is.EqualTo(fileName));
	}

	[Test]
	public async Task TheVerifiedFileCannotBeChangedOrReplacedUntilTheInstallerHasBeenStarted() {
		using var installer = new SubstitutedInstaller(CreateUpdate(ContentHash));
		var installerPath = await installer.DownloadAsync();
		using (Tools.Scope.DeleteDirOnDispose(Path.GetDirectoryName(installerPath))) {
			AssertHeld(installerPath);
			// The hold still stands while the process is being started.
			installer.Starting = () => AssertHeld(installerPath);
			await installer.LaunchAsync();
			Assert.That(installer.Starts, Is.EqualTo(1));
			// Once the start call has returned the handle is released, and the installer's own process owns the file.
			Assert.That(() => new FileStream(installerPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None).Dispose(), Throws.Nothing);
		}
	}

	[Test]
	public async Task AnExecutableIsStartedDirectlyWithTheUpdatesArgumentsAsOneString() {
		var update = CreateUpdate(ContentHash);
		update.InstallerArguments = "/SILENT /DIR=\"C:\\Program Files\\Example\" & calc";
		string folder;
		using (var installer = new SubstitutedInstaller(update)) {
			var installerPath = await installer.DownloadAsync();
			folder = Path.GetDirectoryName(installerPath);
			await installer.LaunchAsync();
			Assert.That(installer.StartedFileName, Is.EqualTo(installerPath));
			Assert.That(installer.StartedArguments, Is.EqualTo(update.InstallerArguments), "The arguments are handed over unchanged and are not interpreted.");
			Assert.That(installer.StartedWorkingDirectory, Is.EqualTo(folder));
		}
		using (Tools.Scope.DeleteDirOnDispose(folder))
			Assert.That(Directory.Exists(folder), Is.True, "A started installer keeps its folder, since it outlives this application.");
	}

	[TestCase("/qb", "/i \"{0}\" /qb")]
	[TestCase(null, "/i \"{0}\"")]
	public async Task AnMsiPackageIsRunThroughMsiexecWithTheVerifiedPath(string arguments, string expectedArguments) {
		var update = CreateUpdate(ContentHash);
		update.FileName = "setup.msi";
		update.InstallerArguments = arguments;
		using var installer = new SubstitutedInstaller(update);
		var installerPath = await installer.DownloadAsync();
		using (Tools.Scope.DeleteDirOnDispose(Path.GetDirectoryName(installerPath))) {
			await installer.LaunchAsync();
			Assert.That(installer.StartedFileName, Is.EqualTo(Path.Combine(Environment.SystemDirectory, "msiexec.exe")));
			Assert.That(installer.StartedArguments, Is.EqualTo(string.Format(expectedArguments, installerPath)));
		}
	}

	[Test]
	public async Task AnInstallerThatCannotBeStartedIsReleasedAndRemoved() {
		var failure = new InvalidOperationException("The operation was cancelled by the user.");
		using var installer = new SubstitutedInstaller(CreateUpdate(ContentHash)) { StartFailure = failure };
		await Assert.ThatAsync(async () => await installer.LaunchAsync(), Throws.InvalidOperationException, "Nothing is started before a verified download.");
		var installerPath = await installer.DownloadAsync();
		await Assert.ThatAsync(async () => await installer.LaunchAsync(), Throws.Exception.SameAs(failure));
		Assert.That(Directory.Exists(Path.GetDirectoryName(installerPath)), Is.False, "A failed or declined launch leaves nothing behind.");
		Assert.That(installer.InstallerPath, Is.Null);
	}

	[Test]
	public void AnInstallerNeedsItsUpdateAndItsProduct() {
		Assert.That(() => new ProductUpdateInstaller(null, new ProductInformation()), Throws.ArgumentNullException);
		Assert.That(() => new ProductUpdateInstaller(CreateUpdate(ContentHash), null), Throws.ArgumentNullException);
	}

	private static ReleaseServerProductUpdate CreateUpdate(string hash) => new() {
		Version = ApplicationVersion.Parse("2.0.0.0"),
		DownloadUrl = "https://updates.example.com/files/setup.exe",
		FileName = "setup.exe",
		Length = Content.Length,
		Hash = hash
	};

	// A file held by the installer can still be read, but no other handle can write, replace, move or delete it.
	private static void AssertHeld(string path) {
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(Content));
		Assert.That(() => File.WriteAllBytes(path, new byte[] { 1 }), Throws.InstanceOf<IOException>());
		Assert.That(() => new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite).Dispose(), Throws.InstanceOf<IOException>());
		Assert.That(() => File.Delete(path), Throws.InstanceOf<IOException>());
		Assert.That(() => File.Move(path, path + ".moved"), Throws.InstanceOf<IOException>());
		var replacement = path + ".replacement";
		File.WriteAllBytes(replacement, new byte[] { 1 });
		using (Tools.Scope.DeleteFileOnDispose(replacement)) {
			Assert.That(() => File.Copy(replacement, path, true), Throws.InstanceOf<IOException>());
			Assert.That(() => File.Replace(replacement, path, null), Throws.InstanceOf<IOException>());
		}
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(Content));
	}

	#region Inner Types

	/// <summary>An installer whose transfer and process start are supplied by the case instead of BITS and Windows.</summary>
	private sealed class SubstitutedInstaller : ProductUpdateInstaller {
		private readonly ConcurrentQueue<string> _errors;

		public SubstitutedInstaller(ReleaseServerProductUpdate update)
			: this(update, new ConcurrentQueue<string>()) {
		}

		private SubstitutedInstaller(ReleaseServerProductUpdate update, ConcurrentQueue<string> errors)
			: base(update, new ProductInformation { ProductName = "Example Product", ProductVersion = ApplicationVersion.Parse("1.0.0.0") }, new ActionLogger(_ => { }, _ => { }, _ => { }, errors.Enqueue)) {
			_errors = errors;
		}

		public Func<string, CancellationToken, Task> Transfer { get; set; } = (path, _) => File.WriteAllBytesAsync(path, Content);

		public Action Starting { get; set; }

		public Exception StartFailure { get; set; }

		public string[] Errors => _errors.ToArray();

		public string Url { get; private set; }

		public string DestinationPath { get; private set; }

		public int Starts { get; private set; }

		public string StartedFileName { get; private set; }

		public string StartedArguments { get; private set; }

		public string StartedWorkingDirectory { get; private set; }

		protected override Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken) {
			Url = url;
			DestinationPath = destinationPath;
			return Transfer(destinationPath, cancellationToken);
		}

		protected override Task StartInstallerAsync(string fileName, string arguments, string workingDirectory, IntPtr ownerWindow, CancellationToken cancellationToken) {
			Starts++;
			StartedFileName = fileName;
			StartedArguments = arguments;
			StartedWorkingDirectory = workingDirectory;
			Starting?.Invoke();
			return StartFailure != null ? Task.FromException(StartFailure) : Task.CompletedTask;
		}
	}

	#endregion
}
