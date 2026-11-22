// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Utils.BlazorTester.Showcase.Records;

/// <summary>The values entered in the one-step New Product wizard.</summary>
public sealed class NewProductDraft {

	/// <summary>The product name.</summary>
	public string Name { get; set; }

	/// <summary>The short product code.</summary>
	public string Code { get; set; }

	/// <summary>The first version.</summary>
	public string Version { get; set; } = "1.0.0";

	/// <summary>The initial status.</summary>
	public ProductStatus Status { get; set; }
}
