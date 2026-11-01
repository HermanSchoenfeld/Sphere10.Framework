// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Stores declared preference values and migration presence together through an existing native settings provider.</summary>
public sealed class WinFormsApplicationSettingsRecord : SettingsObject {
	/// <summary>Values decoded using the registration's known settings type, without serialized runtime type names.</summary>
	public JToken Values { get; set; }

	/// <summary>Section migration versions retained independently of resettable preference values.</summary>
	public Dictionary<string, int> SectionVersions { get; set; } = new();
}
