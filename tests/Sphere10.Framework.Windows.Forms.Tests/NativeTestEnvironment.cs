// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;

namespace Sphere10.Framework;

/// <summary>Initializes native tests with the visual styles used by application entry points.</summary>
[SetUpFixture]
public sealed class NativeTestEnvironment {

	/// <summary>Enables visual styles before any fixture registers native control classes.</summary>
	[OneTimeSetUp]
	public void InitializeNativeControls() => System.Windows.Forms.Application.EnableVisualStyles();
}
