// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Utils.BlazorTester.Showcase.Cards;

/// <summary>A circuit-local sample collection, deliberately independent of any client application.</summary>
public sealed class CreativeCollection {
	public string Id { get; set; }

	public string Name { get; set; }

	public string Category { get; set; }

	public string Owner { get; set; }

	public string Description { get; set; }

	public CollectionStatus Status { get; set; }

	public int ItemCount { get; set; }

	public int Progress { get; set; }

	public int Artwork { get; set; }

	public DateTime Updated { get; set; }

	public CreativeCollection Copy() => (CreativeCollection)MemberwiseClone();

	public bool HasSameEdits(CreativeCollection other) => other != null && Name == other.Name && Owner == other.Owner
		&& Description == other.Description && Status == other.Status && Progress == other.Progress;
}

public enum CollectionStatus {
	Draft,
	Active,
	Archived
}
