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
	public static class CRYPT32 {
		[StructLayout(LayoutKind.Sequential)]
		public struct CERT_CONTEXT {
			public uint EncodingType;
			public IntPtr EncodedCertificate;
			public uint EncodedLength;
			public IntPtr CertificateInfo;
			public IntPtr Store;
		}
	}
}

