// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

public static partial class WinAPI {
	public static class WINTRUST {
		public static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
		public const uint WTD_UI_NONE = 2;
		public const uint WTD_CHOICE_FILE = 1;
		public const uint WTD_STATEACTION_VERIFY = 1;
		public const uint WTD_STATEACTION_CLOSE = 2;
		public const uint WTD_REVOCATION_CHECK_NONE = 0x10;
		public const uint WTD_CACHE_ONLY_URL_RETRIEVAL = 0x1000;
		public const int TRUST_E_NOSIGNATURE = unchecked((int)0x800B0100);
		public const int TRUST_E_SUBJECT_FORM_UNKNOWN = unchecked((int)0x800B0003);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct WINTRUST_FILE_INFO {
			public uint Size;
			[MarshalAs(UnmanagedType.LPWStr)] public string FilePath;
			public IntPtr FileHandle;
			public IntPtr KnownSubject;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct WINTRUST_DATA {
			public uint Size;
			public IntPtr PolicyCallbackData;
			public IntPtr SIPClientData;
			public uint UIChoice;
			public uint RevocationChecks;
			public uint UnionChoice;
			public IntPtr FileInfo;
			public uint StateAction;
			public IntPtr StateData;
			[MarshalAs(UnmanagedType.LPWStr)] public string URLReference;
			public uint ProviderFlags;
			public uint UIContext;
			public IntPtr SignatureSettings;
		}

		/// <summary>Returns the native trust-provider status; only zero denotes success. StateAction CLOSE releases provider state.</summary>
		[DllImport("wintrust.dll", ExactSpelling = true)]
		public static extern int WinVerifyTrust(IntPtr window, ref Guid action, ref WINTRUST_DATA data);

		[StructLayout(LayoutKind.Sequential)]
		public struct CRYPT_PROVIDER_CERT {
			public uint Size;
			public IntPtr Certificate;
			[MarshalAs(UnmanagedType.Bool)] public bool Commercial;
			[MarshalAs(UnmanagedType.Bool)] public bool TrustedRoot;
			[MarshalAs(UnmanagedType.Bool)] public bool SelfSigned;
			[MarshalAs(UnmanagedType.Bool)] public bool TestCertificate;
			public uint RevokedReason;
			public uint Confidence;
			public uint Error;
			public IntPtr TrustListContext;
			[MarshalAs(UnmanagedType.Bool)] public bool TrustListSignerCertificate;
			public IntPtr CtlContext;
			public uint CtlError;
			[MarshalAs(UnmanagedType.Bool)] public bool Cyclic;
			public IntPtr ChainElement;
		}

		[DllImport("wintrust.dll", ExactSpelling = true)]
		public static extern IntPtr WTHelperProvDataFromStateData(IntPtr stateData);

		[DllImport("wintrust.dll", ExactSpelling = true)]
		public static extern IntPtr WTHelperGetProvSignerFromChain(IntPtr provider, uint signerIndex, [MarshalAs(UnmanagedType.Bool)] bool counterSigner, uint counterSignerIndex);

		[DllImport("wintrust.dll", ExactSpelling = true)]
		public static extern IntPtr WTHelperGetProvCertFromChain(IntPtr signer, uint certificateIndex);
	}
}

