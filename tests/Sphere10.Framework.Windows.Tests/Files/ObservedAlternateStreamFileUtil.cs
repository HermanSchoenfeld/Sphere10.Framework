// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using Sphere10.Framework.Windows.Files;

namespace Sphere10.Framework.Windows.Tests.Files;

/// <summary>A caller-owned open probe used to verify asynchronous dispatch without touching external files.</summary>
internal sealed class ObservedAlternateStreamFileUtil : FileUtil {
	public Action BeforeOpen { get; init; }

	public override FileStream OpenAlternateStream(string filePath, string streamName, FileMode mode, FileAccess access) {
		BeforeOpen?.Invoke();
		return base.OpenAlternateStream(filePath, streamName, mode, access);
	}
}
