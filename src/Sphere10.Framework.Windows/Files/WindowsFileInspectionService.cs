// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Files;

/// <summary>On-demand local file version, hash and embedded Authenticode inspection without UI or network retrieval.</summary>
public class WindowsFileInspectionService {
	public virtual Task<WindowsFileInspection> InspectAsync(string path, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrWhitespace(path, nameof(path));
		return Task.Run(() => Inspect(path, () => cancellationToken.ThrowIfCancellationRequested()), cancellationToken);
	}

	public WindowsFileInspection Inspect(string path) => Inspect(path, null);

	private WindowsFileInspection Inspect(string path, Action checkpoint) {
		checkpoint?.Invoke();
		path = Path.GetFullPath(path);
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
		using var hashing = Hashers.EnterBorrowHasherScope(CHF.SHA2_256, out var hasher);
		var buffer = new byte[65536];
		int count;
		while ((count = stream.Read(buffer)) > 0) {
			checkpoint?.Invoke();
			hasher.Transform(buffer.AsSpan(0, count));
		}
		var hash = Convert.ToHexString(hasher.GetResult());
		stream.Position = 0;
		var version = FileVersionInfo.GetVersionInfo(path);
		checkpoint?.Invoke();
		var status = VerifyEmbeddedSignature(path, stream.SafeFileHandle.DangerousGetHandle(), out var encodedCertificate);
		var signer = string.Empty;
		var issuer = string.Empty;
		var thumbprint = string.Empty;
		var certificateError = 0;
		if (encodedCertificate.Length > 0) {
			Tools.Exceptions.ExecuteCapturingException<CryptographicException>(() => {
				using var certificate = X509CertificateLoader.LoadCertificate(encodedCertificate);
				signer = certificate.Subject;
				issuer = certificate.Issuer;
				thumbprint = certificate.Thumbprint;
			}, out var error);
			certificateError = error?.HResult ?? 0;
		}
		checkpoint?.Invoke();
		return new WindowsFileInspection {
			Path = path, SizeBytes = stream.Length, LastWriteUtc = File.GetLastWriteTimeUtc(path), Sha256 = hash,
			Company = version.CompanyName, Product = version.ProductName, Description = version.FileDescription,
			FileVersion = version.FileVersion, ProductVersion = version.ProductVersion, OriginalFilename = version.OriginalFilename,
			TrustStatus = status, Signer = signer, Issuer = issuer, Thumbprint = thumbprint, CertificateErrorCode = certificateError
		};
	}

	private static int VerifyEmbeddedSignature(string path, IntPtr handle, out byte[] encodedCertificate) {
		encodedCertificate = Array.Empty<byte>();
		var file = new WinAPI.WINTRUST.WINTRUST_FILE_INFO {
			Size = (uint)Marshal.SizeOf<WinAPI.WINTRUST.WINTRUST_FILE_INFO>(), FilePath = path, FileHandle = handle
		};
		using var allocation = new UnmanagedMemoryScope(Marshal.SizeOf<WinAPI.WINTRUST.WINTRUST_FILE_INFO>());
		var pointer = allocation.Item;
		var initialized = false;
		using (Tools.Scope.ExecuteOnDispose(() => {
			if (initialized)
				Marshal.DestroyStructure<WinAPI.WINTRUST.WINTRUST_FILE_INFO>(pointer);
		})) {
			Marshal.StructureToPtr(file, pointer, false);
			initialized = true;
			var data = new WinAPI.WINTRUST.WINTRUST_DATA {
				Size = (uint)Marshal.SizeOf<WinAPI.WINTRUST.WINTRUST_DATA>(), UIChoice = WinAPI.WINTRUST.WTD_UI_NONE,
				UnionChoice = WinAPI.WINTRUST.WTD_CHOICE_FILE, FileInfo = pointer, StateAction = WinAPI.WINTRUST.WTD_STATEACTION_VERIFY,
				ProviderFlags = WinAPI.WINTRUST.WTD_REVOCATION_CHECK_NONE | WinAPI.WINTRUST.WTD_CACHE_ONLY_URL_RETRIEVAL
			};
			var action = WinAPI.WINTRUST.WINTRUST_ACTION_GENERIC_VERIFY_V2;
			using (Tools.Scope.ExecuteOnDispose(() => {
				data.StateAction = WinAPI.WINTRUST.WTD_STATEACTION_CLOSE;
				WinAPI.WINTRUST.WinVerifyTrust((IntPtr)(-1), ref action, ref data);
			})) {
				var status = WinAPI.WINTRUST.WinVerifyTrust((IntPtr)(-1), ref action, ref data);
				// Provider pointers are borrowed and valid only until CLOSE. Copy the encoded leaf certificate before releasing that state.
				var provider = data.StateData == IntPtr.Zero ? IntPtr.Zero : WinAPI.WINTRUST.WTHelperProvDataFromStateData(data.StateData);
				var signer = provider == IntPtr.Zero ? IntPtr.Zero : WinAPI.WINTRUST.WTHelperGetProvSignerFromChain(provider, 0, false, 0);
				var pointerToCertificate = signer == IntPtr.Zero ? IntPtr.Zero : WinAPI.WINTRUST.WTHelperGetProvCertFromChain(signer, 0);
				if (pointerToCertificate != IntPtr.Zero) {
					var certificate = Marshal.PtrToStructure<WinAPI.WINTRUST.CRYPT_PROVIDER_CERT>(pointerToCertificate);
					if (certificate.Certificate != IntPtr.Zero) {
						var context = Marshal.PtrToStructure<WinAPI.CRYPT32.CERT_CONTEXT>(certificate.Certificate);
						Guard.Ensure(context.EncodedLength <= 1024 * 1024, "The signer certificate exceeds the 1 MiB inspection limit.");
						if (context.EncodedCertificate != IntPtr.Zero && context.EncodedLength > 0) {
							encodedCertificate = new byte[context.EncodedLength];
							Marshal.Copy(context.EncodedCertificate, encodedCertificate, 0, encodedCertificate.Length);
						}
					}
				}
				return status;
			}
		}
	}
}
