// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Sphere10.Framework.DApp.Presentation.Plugins;

namespace Sphere10.Framework.DApp.Presentation.Loader.Plugins;

/// <summary>
/// Finds available plugin types.
/// </summary>
public interface IPluginLocator {
	/// <summary>
	/// Locate plugins.
	/// </summary>
	/// <returns> <see cref="IPlugin"/> implementing plugin types.</returns>
	IEnumerable<Type> LocatePlugins();
}


