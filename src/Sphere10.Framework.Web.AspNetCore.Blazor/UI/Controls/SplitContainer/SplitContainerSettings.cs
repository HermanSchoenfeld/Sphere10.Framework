// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls;

/// <summary>A <see cref="SplitContainer"/>'s remembered splitter position, saved through <c>UserSettings</c> under the container's <c>SettingsKey</c>.</summary>
public class SplitContainerSettings : SettingsObject {

	/// <summary>The first panel's share of the container, in percent.</summary>
	[DisplayName("Splitter position")]
	[DefaultValue(50D)]
	public double SplitterPosition { get; set; } = 50;
}
