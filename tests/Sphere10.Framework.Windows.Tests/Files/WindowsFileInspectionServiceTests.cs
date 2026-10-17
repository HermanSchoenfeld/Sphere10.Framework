// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Files;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[SupportedOSPlatform("windows6.1")]
public class WindowsFileInspectionServiceTests {
	[Test]
	public async Task UnsignedOwnedInfShowsItsRealHashAndDoesNotConfuseCatalogSigningWithAnEmbeddedSignature() {
		var path = Path.Combine(Path.GetTempPath(), "Sphere10-Driver-Inspection-" + Guid.NewGuid().ToString("N") + ".inf");
		var contents = Encoding.UTF8.GetBytes("[Version]\r\nSignature=\"$Windows NT$\"\r\nProvider=Fixture\r\n");
		{
			using var deleteInspectedFile = Tools.Scope.ExecuteOnDispose(() => {
				File.Delete(path);
			});
			await File.WriteAllBytesAsync(path, contents);
			var service = new WindowsFileInspectionService();
			for (var i = 0; i < 3; i++) {
				var result = await service.InspectAsync(path);
				Assert.That(result.Path, Is.EqualTo(path));
				Assert.That(result.SizeBytes, Is.EqualTo(contents.Length));
				Assert.That(result.Sha256, Is.EqualTo(Convert.ToHexString(SHA256.HashData(contents))));
				Assert.That(result.TrustStatus, Is.EqualTo(WinAPI.WINTRUST.TRUST_E_NOSIGNATURE).Or.EqualTo(WinAPI.WINTRUST.TRUST_E_SUBJECT_FORM_UNKNOWN));
				Assert.That(result.Signer, Is.Empty);
			}
			using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			Assert.That(exclusive.Length, Is.EqualTo(contents.Length), "Inspection must release its file handle and native trust-provider state.");
		}
	}

	[Test]
	public async Task ARealWindowsBinaryProvidesVersionAndTrustDetailsWithoutExecutingIt() {
		var path = Path.Combine(Environment.SystemDirectory, "kernel32.dll");
		var result = await new WindowsFileInspectionService().InspectAsync(path);
		Assert.That(result.SizeBytes, Is.GreaterThan(0));
		Assert.That(result.FileVersion, Is.Not.Empty);
		Assert.That(result.Company, Does.Contain("Microsoft"));
		Assert.That(result.Sha256.Length, Is.EqualTo(64));
		Assert.That(result.TrustStatus, Is.TypeOf<int>(), "The reusable wrapper returns the native trust status rather than an application display label.");
	}

	[Test]
	public async Task CancelledInspectionDoesNotOpenItsMissingFile() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var task = new WindowsFileInspectionService().InspectAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), cancellation.Token);
		await Assert.ThatAsync(async () => await task, Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public void NativeTrustAndShellStructuresMatchThePointerSizedWindowsLayout() {
		Assert.That(Marshal.SizeOf<WinAPI.WINTRUST.WINTRUST_FILE_INFO>(), Is.EqualTo(IntPtr.Size == 8 ? 32 : 16));
		Assert.That(Marshal.SizeOf<WinAPI.WINTRUST.WINTRUST_DATA>(), Is.EqualTo(IntPtr.Size == 8 ? 88 : 52));
		Assert.That(Marshal.SizeOf<WinAPI.SHELL32.SHELLEXECUTEINFO>(), Is.EqualTo(IntPtr.Size == 8 ? 112 : 60));
		Assert.That(Marshal.OffsetOf<WinAPI.WINTRUST.CRYPT_PROVIDER_CERT>(nameof(WinAPI.WINTRUST.CRYPT_PROVIDER_CERT.Certificate)).ToInt32(), Is.EqualTo(IntPtr.Size == 8 ? 8 : 4));
		Assert.That(Marshal.SizeOf<WinAPI.CRYPT32.CERT_CONTEXT>(), Is.EqualTo(IntPtr.Size == 8 ? 40 : 20));
	}
}

