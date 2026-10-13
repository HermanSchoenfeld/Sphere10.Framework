// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// Coordinates freeing capacity without depending on a cache's key and value representation.
/// </summary>
public interface ICacheReaper {

	/// <summary>Registers a cache so the reaper can manage its capacity.</summary>
	void Register(ICacheBase cache);

	/// <summary>Deregisters a previously registered cache.</summary>
	void Deregister(ICacheBase cache);

	/// <summary>Indicates how many size-estimator units are currently free.</summary>
	long AvailableSpace();

	/// <summary>Attempts to release the requested estimator units. The caller applies its ReapLevel before calling this method.</summary>
	long MakeSpace(ICacheBase requestingCache, long requestedSpace);

	#region Base Interface Bridges

	/// <summary>Checks available capacity for an active member. Existing custom reapers may retain their original availability implementation.</summary>
	long AvailableSpace(ICacheBase requestingCache) => AvailableSpace();

	#endregion

}
