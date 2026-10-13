// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// Inheritance foundation for isolated and shared-capacity cache reapers.
/// </summary>
public abstract class CacheReaperBase : SynchronizedObject, ICacheReaper {

	public abstract void Register(ICacheBase cache);

	public abstract void Deregister(ICacheBase cache);

	public abstract long AvailableSpace();

	public virtual long AvailableSpace(ICacheBase requestingCache) => AvailableSpace();

	public abstract long MakeSpace(ICacheBase requestingCache, long requestedSpace);

}
