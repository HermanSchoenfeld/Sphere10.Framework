// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application;

/// <summary>
/// Describes a version this installation can upgrade to, as every update channel presents it. A <see cref="ProductUpdaterBase"/>
/// finds it with <see cref="ProductUpdaterBase.CheckForUpdateAsync"/> and installs it with <see cref="ProductUpdaterBase.InstallAsync"/>.
/// </summary>
/// <remarks>
/// A channel derives from it to carry what its installation needs: <see cref="ReleaseServerProductUpdate"/> adds the release server's
/// installer, and the Windows Store channel adds the Store's package updates.
/// </remarks>
public class ProductUpdate {

	/// <summary>The version the update upgrades this installation to.</summary>
	public ApplicationVersion Version { get; set; }

	/// <summary>Text describing the release, shown to the user as supplied, or null when the channel publishes none.</summary>
	public string ReleaseNotes { get; set; }

	/// <summary>Whether the publisher requires this update, as the Windows Store marks a mandatory package update.</summary>
	public bool IsMandatory { get; set; }
}
