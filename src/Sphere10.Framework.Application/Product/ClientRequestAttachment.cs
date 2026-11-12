// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Reflection;

namespace Sphere10.Framework.Application;

/// <summary>A file a user attaches to a support request, such as a screenshot or a log; at most <see cref="MaxBytes"/> bytes.</summary>
[Obfuscation(Exclude = true)]
public class ClientRequestAttachment {
	public const int MaxBytes = 1048576;
	public const int MaxFileNameLength = 255;

	public ClientRequestAttachment() {
	}

	public ClientRequestAttachment(string fileName, byte[] data) {
		FileName = fileName;
		Data = data;
	}

	public string FileName { get; set; } = string.Empty;

	public byte[] Data { get; set; } = Array.Empty<byte>();
}
