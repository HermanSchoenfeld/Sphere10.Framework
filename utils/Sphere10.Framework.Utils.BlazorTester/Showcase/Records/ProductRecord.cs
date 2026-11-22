// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Utils.BlazorTester.Showcase.Records;

/// <summary>Sample product status shown by the power-user records screen.</summary>
public enum ProductStatus {
	Active,
	Retired
}

/// <summary>A circuit-local sample product with its editions, browsed by the power-user records screen.</summary>
public sealed class ProductRecord {

	/// <summary>The sample identity.</summary>
	public int Id { get; init; }

	/// <summary>The product name.</summary>
	public string Name { get; set; }

	/// <summary>The short product code.</summary>
	public string Code { get; set; }

	/// <summary>The current version.</summary>
	public string Version { get; set; }

	/// <summary>Whether the product is still sold.</summary>
	public ProductStatus Status { get; set; }

	/// <summary>The first release date.</summary>
	public DateTime Released { get; set; }

	/// <summary>The number of issued licenses.</summary>
	public int LicenseCount { get; set; }

	/// <summary>The editions sold for this product.</summary>
	public EditionRecord[] Editions { get; set; } = Array.Empty<EditionRecord>();
}
