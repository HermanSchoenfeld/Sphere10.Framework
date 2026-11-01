// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application.UI;

/// <summary>An owner-thread value snapshot with runtime revision information for ordered background persistence.</summary>
public sealed class ApplicationSettingsCapture {
	// Only the owning profile creates captures, after copying values on their model's owner thread.
	internal ApplicationSettingsCapture(ApplicationSettingsProfile profile, object values, object owner, long baseRevision, long ownerVersion) {
		Profile = profile;
		Values = values;
		Owner = owner;
		BaseRevision = baseRevision;
		OwnerVersion = ownerVersion;
	}

	/// <summary>The accepted revision last applied by this producer when its values were captured.</summary>
	public long BaseRevision { get; }

	/// <summary>A monotonically increasing value version within this producer's lifetime.</summary>
	public long OwnerVersion { get; }

	internal ApplicationSettingsProfile Profile { get; }

	internal object Values { get; }

	internal object Owner { get; }
}
