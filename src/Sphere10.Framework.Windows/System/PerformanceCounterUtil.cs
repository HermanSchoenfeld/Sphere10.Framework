// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Creates caller-owned PDH queries. Sampling, counter selection and aggregation belong to the consumer.</summary>
public class PerformanceCounterUtil {
	public virtual WindowsPerformanceQuery OpenQuery(string[] englishCounterPaths) => new(englishCounterPaths);

	public virtual Task<WindowsPerformanceQuery> OpenQueryAsync(string[] englishCounterPaths, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(englishCounterPaths, nameof(englishCounterPaths));
		var paths = (string[])englishCounterPaths.Clone();
		return Task.Run(() => OpenQuery(paths), cancellationToken);
	}
}

