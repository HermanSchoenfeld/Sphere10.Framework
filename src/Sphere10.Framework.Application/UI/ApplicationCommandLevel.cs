// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application.UI;

/// <summary>The lifetime and composition scope of a menu or toolbar contribution.</summary>
public enum ApplicationCommandLevel {
	/// <summary>Commands owned by the running application.</summary>
	Application,

	/// <summary>Commands owned by the visible screen's block.</summary>
	Block,

	/// <summary>Commands owned by the visible screen.</summary>
	Screen,

	/// <summary>Commands owned by the visible screen's source plugin.</summary>
	Plugin
}
