// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Edits settings values while excluding transient members and runtime ownership.</summary>
public class ApplicationSettingsEditor : DefaultCrudEntityEditor {

	/// <summary>Applies the Framework's existing settings visibility rules to the native property editor and opens its first level.</summary>
	public ApplicationSettingsEditor() {
		AutoExpandFirstLevel = true;
		HiddenProperties = Array.Empty<string>();
		PropertyFilter = (owner, property) => ApplicationHelper.IsSettingsMember(property.PropertyType, property.Attributes.Cast<Attribute>())
			&& (owner is not SettingsObject || property.Name is not (nameof(SettingsObject.ID) or nameof(SettingsObject.Provider)));
	}
}
