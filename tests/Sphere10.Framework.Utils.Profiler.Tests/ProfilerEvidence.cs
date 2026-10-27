// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Utils.Profiler.Tests;

internal static class ProfilerEvidence {
	internal const string MissingInputMessage = "Historical profiler captures are optional external input. Supply the ProfilerResultsRoot NUnit parameter to inspect them.";

	internal static bool IsConfigured => !string.IsNullOrWhiteSpace(TestContext.Parameters.Get("ProfilerResultsRoot", string.Empty));

	internal static string GetRoot() {
		if (!IsConfigured)
			Assert.Ignore(MissingInputMessage);
		return Path.GetFullPath(TestContext.Parameters.Get("ProfilerResultsRoot", string.Empty));
	}
}
