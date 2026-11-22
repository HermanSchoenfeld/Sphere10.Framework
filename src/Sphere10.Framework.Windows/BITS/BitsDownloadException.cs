// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.BITS;

/// <summary>A <see cref="BitsFileDownload"/> did not produce its file. The message carries the description BITS gave for the failure.</summary>
public class BitsDownloadException : SoftwareException {

	public BitsDownloadException(string message)
		: base(message) {
	}

	public BitsDownloadException(string message, Exception innerException)
		: base(message, innerException) {
	}
}
