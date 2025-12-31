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
using Sphere10.Framework.DApp.Presentation.WidgetGallery;

namespace Sphere10.Framework.DApp.Presentation.Loader.Plugins;

/// <summary>
/// Static plugin locator - knows the plugins and has direct references available.
/// </summary>
public class StaticPluginLocator : IPluginLocator {
	/// <summary>
	/// Locate plugins.
	/// </summary>
	/// <returns> <see cref="IPlugin"/> implementing plugin types.</returns>
	public IEnumerable<Type> LocatePlugins() {
		return new[] { typeof(Sphere10Plugin), typeof(WidgetGalleryPlugin) };
	}
}


