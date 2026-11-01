// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Application.UI;

/// <summary>A detached persistence envelope whose migration identity is separate from resettable preference values.</summary>
public sealed class ApplicationSettingsStoredProfile {
	/// <summary>The ordinary settings values accepted by their owning registration.</summary>
	public object Values { get; set; }

	/// <summary>Initialized named sections and their consumer-owned migration versions.</summary>
	public Dictionary<string, int> SectionVersions { get; set; } = new();
}
