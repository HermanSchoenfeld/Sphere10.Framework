// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Storage;

/// <summary>Runs the in-box chkdsk.exe in read-only mode. No repair, dismount, recovery or boot scheduling flags are passed.</summary>
public class VolumeCheckSentry : ProcessSentry {
	public VolumeCheckSentry()
		: base(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "chkdsk.exe")) {
	}

	public virtual Task<ProcessExecutionResult> CheckAsync(string driveRoot, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrWhitespace(driveRoot, nameof(driveRoot));
		Guard.Argument(driveRoot.Length is 2 or 3 && char.IsAsciiLetter(driveRoot[0]) && driveRoot[1] == ':'
			&& (driveRoot.Length == 2 || driveRoot[2] is '\\' or '/'), nameof(driveRoot), "Select one local drive letter, such as C:\\.");
		return RunCapturedAsync(new[] { driveRoot[..2].ToUpperInvariant() }, cancellationToken);
	}
}
