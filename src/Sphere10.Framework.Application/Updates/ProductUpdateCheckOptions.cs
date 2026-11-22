// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application;

/// <summary>When <see cref="ProductUpdateChecker"/> checks for an update. Configure it through <c>UseProductUpdateChecks</c>.</summary>
public class ProductUpdateCheckOptions {

	/// <summary>The time between starting the checker and its first check. The default is 30 seconds.</summary>
	public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(30);

	/// <summary>The time between the end of one check and the start of the next. The default is one hour.</summary>
	public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
}
