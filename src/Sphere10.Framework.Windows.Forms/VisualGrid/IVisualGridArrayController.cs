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

/// <summary>Nonvisual array binding operations shared by concrete nongeneric grid surfaces.</summary>
public interface IVisualGridArrayController {
	/// <summary>The number of currently published rows.</summary>
	long Count {
		get;
	}

	/// <summary>Refreshes presentation on the grid's owner context. A source-backed controller reads published rows without acquiring data; collector-backed controllers retain their collection contract.</summary>
	Task RefreshAsync(CancellationToken cancellationToken = default);

	/// <summary>Configures fallback sorting for subsequent reads.</summary>
	void SetDefaultSort(string property, SortDirection direction);

	/// <summary>Returns the controller's currently published rows.</summary>
	object CaptureRows();
}
