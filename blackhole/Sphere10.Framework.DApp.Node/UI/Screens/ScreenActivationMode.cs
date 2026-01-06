// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.DApp.Node.UI;

/// <summary>
/// Determine when the screen is created and destroyed
/// </summary>
public enum ScreenLifetime {
	/// <summary>
	/// Screen is created on application startup and destroyed on application shutdown
	/// </summary>
	Application,

	/// <summary>
	/// Screen is created on first show and destroyed on application shutdown
	/// </summary>
	LazyLoad,

	/// <summary>
	/// Screen is created on when shown and destroyed not shown
	/// </summary>
	WhenVisible,

}

