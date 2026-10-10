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

namespace Sphere10.Framework.Utils.BlazorTester.Showcase.Cards;

/// <summary>Scoped sample data shared by the gallery and retained detail screens; refreshing the browser resets it.</summary>
public sealed class CollectionCatalogue : SynchronizedObject {
	public event EventHandlerEx Changed;

	private readonly Dictionary<string, CreativeCollection> _collections;

	public CollectionCatalogue() {
		var updated = new DateTime(2026, 10, 1);
		_collections = new[] {
			Create("coastal-notes", "Coastal notes", "Photography", "Alex Morgan", "Light, texture and quiet moments along the coast.", CollectionStatus.Active, 42, 78, 0, updated),
			Create("orbital-studies", "Orbital studies", "Illustration", "Sam Rivera", "A visual exploration of motion, rhythm and negative space.", CollectionStatus.Draft, 18, 32, 1, updated.AddDays(-1)),
			Create("city-in-colour", "City in colour", "Photography", "Taylor Chen", "Everyday architecture seen through an unexpected palette.", CollectionStatus.Active, 64, 92, 2, updated.AddDays(-2)),
			Create("field-guide", "Field guide", "Research", "Jordan Lee", "A living collection of observations from the natural world.", CollectionStatus.Active, 27, 56, 3, updated.AddDays(-3)),
			Create("sound-and-shape", "Sound & shape", "Illustration", "Sam Rivera", "Abstract compositions inspired by a favourite playlist.", CollectionStatus.Draft, 12, 24, 4, updated.AddDays(-4)),
			Create("quiet-interiors", "Quiet interiors", "Photography", "Alex Morgan", "Warm spaces, soft shadows and considered details.", CollectionStatus.Archived, 38, 100, 5, updated.AddDays(-5)),
			Create("material-library", "Material library", "Research", "Taylor Chen", "A tactile reference of surfaces, finishes and sustainable materials.", CollectionStatus.Active, 51, 68, 6, updated.AddDays(-6)),
			Create("weekend-sketches", "Weekend sketches", "Illustration", "Jordan Lee", "Small ideas captured without an agenda.", CollectionStatus.Draft, 23, 41, 7, updated.AddDays(-7)),
			Create("summer-archive", "Summer archive", "Photography", "Alex Morgan", "A completed visual journal of a long summer.", CollectionStatus.Archived, 86, 100, 8, updated.AddDays(-8))
		}.ToDictionary(item => item.Id, StringComparer.Ordinal);
	}

	public CreativeCollection[] ReadAll() {
		using var scope = EnterReadScope();
		return _collections.Values.Select(item => item.Copy()).ToArray();
	}

	public CreativeCollection Read(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		using var scope = EnterReadScope();
		return _collections.TryGetValue(id, out var item) ? item.Copy() : null;
	}

	public void Save(CreativeCollection item) {
		Guard.ArgumentNotNull(item, nameof(item));
		Guard.ArgumentNotNullOrEmpty(item.Id, nameof(item));
		Guard.ArgumentNotNullOrWhitespace(item.Name, nameof(item));
		Guard.ArgumentNotNullOrWhitespace(item.Owner, nameof(item));
		Guard.ArgumentInRange(item.Progress, 0, 100, nameof(item));
		Guard.Argument(item.Status is CollectionStatus.Draft or CollectionStatus.Active or CollectionStatus.Archived, nameof(item), "Unknown collection status.");
		using (EnterWriteScope()) {
			Guard.Argument(_collections.ContainsKey(item.Id), nameof(item), "The collection must exist.");
			var saved = item.Copy();
			saved.Name = saved.Name.Trim();
			saved.Owner = saved.Owner.Trim();
			saved.Updated = DateTime.Today;
			_collections[item.Id] = saved;
		}
		Changed?.Invoke();
	}

	private static CreativeCollection Create(string id, string name, string category, string owner, string description,
		CollectionStatus status, int itemCount, int progress, int artwork, DateTime updated) => new() {
		Id = id, Name = name, Category = category, Owner = owner, Description = description, Status = status,
		ItemCount = itemCount, Progress = progress, Artwork = artwork, Updated = updated
	};
}
