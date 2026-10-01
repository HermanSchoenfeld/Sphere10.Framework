// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;

/// <summary>
/// VelocityNET application plugin. VelocityNET client application will locate implementations of this
/// interface and 
/// </summary>
public interface IBlazorRoutedPlugin {
	/// <summary>
	/// Gets the applications this plugin provides.
	/// </summary>
	IBlazorRoutedApplication[] Apps { get; }

	/// <summary>
	/// Configure the service collection with this plugin's services.
	/// </summary>
	/// <param name="serviceCollection"> services</param>
	IServiceCollection ConfigureServices(IServiceCollection serviceCollection);
}


