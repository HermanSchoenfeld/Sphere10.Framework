// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
public class BlazorGridSettingsTests {
	[Test]
	public void LegacyWidthDictionaryRetainsItsJsonShapeAndMutableValues() {
		var serializer = JsonSerializer.Create(new JsonSerializerSettings());
		var settings = JObject.Parse("{\"PageMode\":1,\"PageSize\":7,\"ColumnWidths\":{\"name\":185}}")
			.ToObject<BlazorGridSettings>(serializer);
		Assert.That(settings.PageMode, Is.EqualTo(BlazorGridPageMode.Auto));
		Assert.That(settings.PageSize, Is.EqualTo(7));
		Assert.That(settings.Columns, Is.Empty);
		settings.ColumnWidths["name"] = 210;
		Assert.That((double)JObject.FromObject(settings, serializer)["ColumnWidths"]["name"], Is.EqualTo(210));
	}

	[Test]
	public void DeclaredColumnWidthsArePersistedOnceWithoutTheLegacyDictionary() {
		var settings = new BlazorGridSettings {
			ColumnWidths = new Dictionary<string, double> { ["name"] = 999 },
			Columns = new[] { new ApplicationGridColumnPreferences { Key = "name", Title = "Name", Width = 185 } }
		};
		var payload = JObject.FromObject(settings, JsonSerializer.Create(new JsonSerializerSettings()));
		Assert.That(payload.Property("ColumnWidths"), Is.Null);
		Assert.That((double)payload["Columns"][0]["Width"], Is.EqualTo(185));
	}

	[TestCase(-1)]
	[TestCase(99)]
	public void InvalidModeOrColumnValuesFailBothRootAndNestedValidation(int pageMode) {
		var settings = new BlazorGridSettings {
			PageMode = (BlazorGridPageMode)pageMode,
			Columns = new[] { new ApplicationGridColumnPreferences { Key = "name", Width = double.NaN } }
		};
		Assert.That(settings.Validate().IsFailure, Is.True);
		Assert.That(Validator.TryValidateObject(settings, new ValidationContext(settings), new List<ValidationResult>(), true), Is.False);
	}
}
