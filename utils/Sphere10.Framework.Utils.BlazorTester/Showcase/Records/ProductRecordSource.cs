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

namespace Sphere10.Framework.Utils.BlazorTester.Showcase.Records;

/// <summary>Serves the records screen's sample products, sorted before paging; the screen adds, retires and removes rows through its commands.</summary>
public sealed class ProductRecordSource : ListDataSource<ProductRecord> {
	private static readonly string[] Names = {
		"Atlas Desktop", "Beacon Server", "Cobalt Reports", "Delta Sync", "Ember Studio", "Fjord Backup",
		"Granite CRM", "Harbor Gateway", "Iris Monitor", "Juniper Mail", "Krypton Vault", "Lumen Analytics"
	};
	private int _nextId;

	/// <summary>Creates the twelve sample products with three editions each.</summary>
	public ProductRecordSource()
		: base(new ExtendedList<ProductRecord>()) {
		for (var index = 0; index < Names.Length; index++)
			List.Add(Create(Names[index], index));
		_nextId = Names.Length + 1;
	}

	/// <summary>The rows in natural order.</summary>
	public ProductRecord[] Records => List.ToArray();

	/// <summary>Adds a product with the standard editions and returns it.</summary>
	public ProductRecord Add(string name, string code, string version, ProductStatus status) {
		var record = new ProductRecord {
			Id = _nextId++, Name = name, Code = code, Version = version, Status = status, Released = DateTime.Today,
			Editions = CreateEditions(code, 0)
		};
		List.Add(record);
		return record;
	}

	/// <summary>Removes a product.</summary>
	public void Remove(ProductRecord record) => List.Remove(record);

	/// <summary>Sorts the complete list by the requested column before taking the requested page.</summary>
	public override DataSourceItems<ProductRecord> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		IEnumerable<ProductRecord> query = List;
		if (!string.IsNullOrWhiteSpace(searchTerm))
			query = query.Where(record => record.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
		Func<ProductRecord, object> sortKey = sortProperty switch {
			nameof(ProductRecord.Name) => record => record.Name,
			nameof(ProductRecord.Code) => record => record.Code,
			nameof(ProductRecord.Version) => record => record.Version,
			nameof(ProductRecord.Status) => record => record.Status,
			nameof(ProductRecord.Released) => record => record.Released,
			nameof(ProductRecord.LicenseCount) => record => record.LicenseCount,
			_ => null
		};
		if (sortKey != null && sortDirection != SortDirection.None)
			query = sortDirection == SortDirection.Descending ? query.OrderByDescending(sortKey).ThenBy(record => record.Id) : query.OrderBy(sortKey).ThenBy(record => record.Id);
		var matches = query.ToArray();
		page = Math.Clamp(page, 0, Math.Max(0, (matches.Length - 1) / Math.Max(1, pageLength)));
		return new DataSourceItems<ProductRecord> {
			Items = matches.Skip(page * pageLength).Take(pageLength).ToArray(),
			Page = page,
			TotalCount = matches.Length
		};
	}

	// Derives stable sample values from the product's position.
	private static ProductRecord Create(string name, int index) {
		var code = string.Concat(name.Split(' ').Select(word => char.ToUpperInvariant(word[0]))) + (index + 1).ToString("00");
		return new ProductRecord {
			Id = index + 1, Name = name, Code = code, Version = $"{1 + index % 4}.{index * 3 % 10}.{index % 3}",
			Status = index % 5 == 4 ? ProductStatus.Retired : ProductStatus.Active, Released = new DateTime(2016 + index % 9, 1 + index % 12, 1 + index * 2 % 27),
			LicenseCount = 40 + index * 37 % 400, Editions = CreateEditions(code, index)
		};
	}

	// Every sample product sells the same three editions at prices derived from its position.
	private static EditionRecord[] CreateEditions(string code, int index) => new[] {
		new EditionRecord("Standard", code + "-STD", 49 + index * 5, 1),
		new EditionRecord("Professional", code + "-PRO", 149 + index * 10, 5),
		new EditionRecord("Enterprise", code + "-ENT", 499 + index * 25, 250)
	};
}
