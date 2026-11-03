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
public interface IVisualGridSnapshotController {
	/// <summary>The number of currently published source rows.</summary>
	long Count {
		get;
	}

	/// <summary>Collects and publishes a new snapshot on the grid's owner context.</summary>
	Task RefreshAsync(CancellationToken cancellationToken = default);

	/// <summary>Configures fallback sorting for subsequent source reads.</summary>
	void SetDefaultSort(string property, SortDirection direction);

	/// <summary>Returns the controller's currently published snapshot.</summary>
	object CaptureSnapshot();
}
