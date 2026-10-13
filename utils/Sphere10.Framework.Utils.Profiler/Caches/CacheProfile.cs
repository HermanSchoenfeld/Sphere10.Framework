// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework.Utils.Profiler.Caches;

public enum CacheSizePattern {
	Unit,
	Weighted
}

public sealed class CacheProfile {
	public string Name { get; set; }

	public CacheReapPolicy ReapPolicy { get; set; } = CacheReapPolicy.LongestIdle;

	public int Capacity { get; set; } = 1024;

	public double ReapLevel { get; set; } = 0.25;

	public CacheSizePattern SizePattern { get; set; }

	public long MinimumBatchRelease => Math.Max(1, (long)Math.Ceiling(Capacity * ReapLevel));

	public void Validate() {
		Guard.ArgumentNotNullOrWhitespace(Name, nameof(Name));
		Guard.Argument(Name.All(character => char.IsAsciiLetterOrDigit(character) || character == '-'), nameof(Name), "Use letters, digits and hyphens in profile names.");
		Guard.Argument(ReapPolicy is CacheReapPolicy.LongestIdle or CacheReapPolicy.Oldest, nameof(ReapPolicy), "Cache profiles support LongestIdle (LRU) and Oldest (FIFO).");
		Guard.ArgumentInRange(Capacity, 16, 1048576, nameof(Capacity));
		Guard.Argument(double.IsFinite(ReapLevel) && ReapLevel > 0 && ReapLevel < 1, nameof(ReapLevel), "ReapLevel must be finite and in (0,1).");
		Guard.Argument(Enum.IsDefined(SizePattern), nameof(SizePattern), "Unknown size-estimator pattern.");
	}

	public static CacheProfile[] CreateSuite(string suite, int capacity, CacheSizePattern sizePattern) {
		Guard.ArgumentInRange(capacity, 16, 1048576, nameof(capacity));
		var profiles = new List<CacheProfile>();
		var minimalLevel = 1.0 / capacity;
		if (Math.Ceiling(capacity * minimalLevel) > 1)
			minimalLevel = Math.BitDecrement(minimalLevel);
		void Add(CacheReapPolicy policy, string level, double reapLevel) {
			profiles.Add(new CacheProfile { Name = $"{(policy == CacheReapPolicy.LongestIdle ? "lru" : "fifo")}-{level}-{sizePattern.ToString().ToLowerInvariant()}",
				ReapPolicy = policy, Capacity = capacity, ReapLevel = reapLevel, SizePattern = sizePattern });
		}
		switch (suite) {
			case "access": Add(CacheReapPolicy.LongestIdle, "quarter", 0.25); break;
			case "reaping":
				foreach (var policy in new[] { CacheReapPolicy.LongestIdle, CacheReapPolicy.Oldest }) {
					Add(policy, "minimal", minimalLevel);
					Add(policy, "quarter", 0.25);
					Add(policy, "half", 0.5);
				}
				break;
			default: throw new ArgumentException($"Unknown cache suite '{suite}'.", nameof(suite));
		}
		foreach (var profile in profiles)
			profile.Validate();
		return profiles.ToArray();
	}
}
