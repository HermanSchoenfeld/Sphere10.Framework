// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Provides ordinary editable values shared by the raw SourceGrid binding examples.</summary>
public class SourceGridGalleryRow {
	public string Name { get; set; } = "New item";

	public int Quantity {
		get; set;
	}

	[DisplayName("Unit price")]
	public decimal UnitPrice {
		get; set;
	}

	[DisplayName("Due date")]
	public DateTime DueDate { get; set; } = new(2026, 11, 2);

	public bool Approved {
		get; set;
	}
}
