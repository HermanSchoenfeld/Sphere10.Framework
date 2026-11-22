// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application;

/// <summary>What <see cref="ProductUpdaterBase.InstallAsync"/> did with an update.</summary>
public enum ProductUpdateInstallResult {

	/// <summary>The user declined in the channel's own prompt, such as the Windows Store's. Nothing was installed.</summary>
	Declined,

	/// <summary>The update's installer was started, and the application exits so that it can replace the application's files.</summary>
	ExitRequired,

	/// <summary>The channel installed the update and closes and restarts the application itself, as the Windows Store does.</summary>
	Completed
}
