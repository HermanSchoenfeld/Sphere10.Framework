// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

/// <summary>
/// Cache-wide synchronization, diagnostics and capacity operations independent of key and value representation.
/// </summary>
public interface ICacheBase : ISynchronizedObject {

	int ItemCount { get; }

	long CurrentSize { get; }

	long MaxCapacity { get; }

	/// <summary>Fraction of admission capacity targeted for release under pressure. Must be greater than zero and less than one.</summary>
	double ReapLevel { get; }

	DateTime LastUpdateOn { get; }

	DateTime LastAccessedOn { get; }

	long TotalAccesses { get; }

	TimeSpan ExpirationDuration { get; }

	ExpirationPolicy ExpirationPolicy { get; }

	CacheReapPolicy ReapPolicy { get; }

	NullValuePolicy NullValuePolicy { get; }

	/// <summary>Attempts to release the requested estimator units, respecting policy and pinned entries. Returns the net capacity released.</summary>
	long Reap(long minimumRelease);

	/// <summary>Removes the entries present at the beginning of the operation.</summary>
	void Purge();

}
