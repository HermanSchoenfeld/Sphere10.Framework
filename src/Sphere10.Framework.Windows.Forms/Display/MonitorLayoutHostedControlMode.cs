// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Where <see cref="MonitorLayoutControl"/> shows the controls it hosts beneath its tiles.</summary>
public enum MonitorLayoutHostedControlMode {
	/// <summary>No hosted controls are shown and the tiles keep the layout of a control without them.</summary>
	None,

	/// <summary>Each tile's control, set through <see cref="MonitorLayoutControl.SetTileControl"/>, sits centred beneath that tile.</summary>
	PerTile,

	/// <summary><see cref="MonitorLayoutControl.SharedControl"/> sits centred beneath the whole arrangement.</summary>
	Shared
}
