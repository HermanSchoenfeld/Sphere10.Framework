// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application;

/// <summary>The stage of an installation that <see cref="ProductUpdateProgress"/> reports.</summary>
public enum ProductUpdateStage {

	/// <summary>The download has not started, for example because it is queued.</summary>
	Waiting,

	/// <summary>The download is connecting to its server.</summary>
	Connecting,

	/// <summary>The connection was interrupted and the download is trying again.</summary>
	Retrying,

	/// <summary>The update is being downloaded.</summary>
	Downloading,

	/// <summary>The downloaded update is being installed.</summary>
	Installing
}
