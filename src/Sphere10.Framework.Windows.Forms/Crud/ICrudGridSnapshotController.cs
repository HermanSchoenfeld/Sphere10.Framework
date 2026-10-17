// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Nonvisual snapshot binding operations shared by concrete nongeneric grid surfaces.</summary>
public interface ICrudGridSnapshotController {
	long Count { get; }

	Task RefreshAsync(CancellationToken cancellationToken = default);

	void SetDefaultSort(string property, SortDirection direction);

	object CaptureSnapshot();
}
