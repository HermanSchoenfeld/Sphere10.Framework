// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests;

/// <summary>Checks local settings value restoration without changing provider ownership or persistence policies.</summary>
[TestFixture, Parallelizable(ParallelScope.Children)]
public class SettingsObjectJsonSerializerTests {
	/// <summary>Local backups restore full values and getter-only membership without publishing or replacing the actual root.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void LocalValuesRestoreCollectionsAndSuppressedValuesWhileRetainingRuntimeOwnership(bool toolsFacade) {
		var provider = new ProhibitedSettingsProvider();
		var values = new Preferences { ID = "owner", Provider = provider, Caption = "Accepted", HiddenValue = "Remembered" };
		values.Items.Add("first");
		values.Roles.Add("reader", 1);
		var items = values.Items;
		var roles = values.Roles;
		var notifications = 0;
		values.OnChanged += () => notifications++;
		var backup = toolsFacade ? Tools.Application.SerializeSettingsValues(values) : SettingsObjectJsonSerializer.SerializeValues(values);

		// Cancel must recover values omitted by ordinary persistence and preserve transient state.
		values.Caption = "Unapplied";
		values.HiddenValue = "Unapplied hidden";
		values.Items.Clear();
		values.Items.Add("second");
		values.Roles.Clear();
		values.Roles.Add("writer", 2);
		values.Runtime = "Current runtime";
		if (toolsFacade)
			Tools.Application.PopulateSettingsValues(backup, values);
		else
			SettingsObjectJsonSerializer.PopulateValues(backup, values);

		Assert.That(values.Caption, Is.EqualTo("Accepted"));
		Assert.That(values.HiddenValue, Is.EqualTo("Remembered"));
		Assert.That(values.Items, Is.SameAs(items).And.EqualTo(new[] { "first" }));
		Assert.That(values.Roles, Is.SameAs(roles));
		Assert.That(values.Roles, Has.Count.EqualTo(1));
		Assert.That(values.Roles["reader"], Is.EqualTo(1));
		Assert.That(values.Runtime, Is.EqualTo("Current runtime"));
		Assert.That(values.ID, Is.EqualTo("owner"));
		Assert.That(values.Provider, Is.SameAs(provider));
		Assert.That(notifications, Is.Zero);

		// Capturing a backup never changes the separate ordinary serializer's omission policy.
		using var writer = new StringWriter();
		new SettingsObjectJsonSerializer().Serialize(writer, values);
		Assert.That(writer.ToString(), Does.Not.Contain(nameof(Preferences.HiddenValue)).And.Not.Contain(nameof(Preferences.Runtime)));
	}

	/// <summary>A serialized record cannot overwrite the identity selected by its actual owner.</summary>
	[Test]
	public void PopulatingValuesPreservesTheOwnersCurrentIdentity() {
		var provider = new ProhibitedSettingsProvider();
		var values = new Preferences { ID = "owner", Provider = provider };
		values.Items.Add("Existing default");

		SettingsObjectJsonSerializer.PopulateValues("{\"id\":\"other-record\",\"Caption\":\"Loaded\"}", values);

		Assert.That(values.Caption, Is.EqualTo("Loaded"));
		Assert.That(values.ID, Is.EqualTo("owner"));
		Assert.That(values.Provider, Is.SameAs(provider));
		Assert.That(values.Items, Is.EqualTo(new[] { "Existing default" }), "Missing collection properties retain their declared values.");
	}

	#region Inner Types

	/// <summary>Supplies persisted, conditional, getter-only and transient settings values.</summary>
	public class Preferences : SettingsObject {
		/// <summary>The editable heading stored normally.</summary>
		public string Caption { get; set; }

		/// <summary>A value intentionally excluded from normal persistence.</summary>
		public string HiddenValue { get; set; }

		/// <summary>The getter-only list whose ownership remains with this settings instance.</summary>
		public List<string> Items { get; } = new();

		/// <summary>The getter-only dictionary whose saved membership can be restored.</summary>
		public Dictionary<string, int> Roles { get; } = new();

		/// <summary>Runtime state excluded from persistence and local backups.</summary>
		[Transient]
		public string Runtime { get; set; }

		/// <summary>Models a persistence-only omission rule used by control settings.</summary>
		public bool ShouldSerializeHiddenValue() => false;
	}

	#endregion
}
