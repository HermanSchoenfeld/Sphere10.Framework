// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application;

/// <summary>Declares a screen's settings schema without constructing its operational screen.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class SettingsAttribute : UseSettingsAttribute {

	/// <summary>Identifies the SettingsObject type activated for each screen registration.</summary>
	public SettingsAttribute(Type settingsType)
		: base(settingsType) {
	}
}
