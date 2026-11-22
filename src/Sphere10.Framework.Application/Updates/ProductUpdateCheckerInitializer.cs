// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application;

/// <summary>Process initializer that starts the <see cref="ProductUpdateChecker"/> schedule. It only schedules, so start-up does not wait for a check.</summary>
public class ProductUpdateCheckerInitializer : SyncApplicationInitializerBase {

	public ProductUpdateCheckerInitializer(ProductUpdateChecker checker) {
		Guard.ArgumentNotNull(checker, nameof(checker));
		Checker = checker;
	}

	protected ProductUpdateChecker Checker { get; }

	public override void Initialize() => Checker.Start();
}
