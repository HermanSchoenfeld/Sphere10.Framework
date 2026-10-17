// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;

namespace Sphere10.Framework.Windows.Storage;

public sealed class WindowsDriveInfo {
	public string Name { get; internal set; }
	public DriveType DriveType { get; internal set; }
	public bool? IsReady { get; internal set; }
	public string Format { get; internal set; }
	public string Label { get; internal set; }
	public long? CapacityBytes { get; internal set; }
	public long? FreeBytes { get; internal set; }
	public long? AvailableBytes { get; internal set; }
	public Exception ReadError { get; internal set; }
}
