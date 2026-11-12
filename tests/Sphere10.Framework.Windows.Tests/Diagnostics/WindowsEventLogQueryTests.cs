// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

using NUnit.Framework;
using Sphere10.Framework.Windows.EventLogs;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public sealed class WindowsEventLogQueryTests {
	[Test]
	public void StructuredPredicatesUseLiteralValuesUtcAndDetachedIdArrays() {
		var query = new WindowsEventLogQuery { Provider = "User's Provider", EventIds = new[] { 7, 42 }, MaximumLevel = 3,
			WriterProcessId = 99, ActivityId = Guid.Parse("d2e0b0b0-12d4-4cbf-bbb0-667bffb533b1"),
			FromUtc = new DateTime(2026, 11, 3, 1, 2, 3, DateTimeKind.Utc), EventDataName = "Path", EventDataEquals = "C:\\'literal'" };
		var copy = query.Copy();
		query.EventIds[0] = 99;
		var xpath = copy.ToXPath();
		Assert.That(xpath, Does.Contain("EventID=7 or EventID=42"));
		Assert.That(xpath, Does.Contain("@Name=\"User's Provider\""));
		Assert.That(xpath, Does.Contain("2026-11-03T01:02:03.0000000Z"));
		Assert.That(xpath, Does.Contain("Execution[@ProcessID=99]"));
		Assert.That(xpath, Does.Contain("EventData[Data[@Name='Path']="));
		Assert.That(new WindowsEventLogQuery().ToNativeQuery(), Is.EqualTo("*"));
	}

	[Test]
	public void LargeIdSetsUseBoundedStructuredSelectorsAndInvalidPredicatesAreRejected() {
		var query = new WindowsEventLogQuery { EventIds = Enumerable.Range(1, 64).ToArray() };
		var xml = XDocument.Parse(query.ToNativeQuery());
		Assert.That(xml.Descendants("Select").Count(), Is.EqualTo(8));
		Assert.That(xml.Descendants("Select").All(select => (string)select.Attribute("Path") == "Application"), Is.True);
		query.Provider = "both'\"quotes";
		Assert.That(query.ToXPath, Throws.ArgumentException);
		query.Provider = string.Empty;
		query.FromUtc = DateTime.UtcNow;
		query.ToUtc = query.FromUtc.Value.AddSeconds(-1);
		Assert.That(query.ToXPath, Throws.ArgumentException);
	}

	[Test]
	[Platform("Win")]
	public async Task NativeHistoricalReaderRetainsXmlAndNativeVariantTypesSeparatelyFromMessage() {
		using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		var rows = await Tools.Windows.EventLogs.ReadEventsAsync(new WindowsEventLogQuery(), new WindowsEventLogReadOptions(), 1,
			TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(10), cancellationToken: deadline.Token);
		if (rows.Length == 0)
			Assert.Inconclusive("This host has no Application events to inspect.");
		var row = rows.Single();
		Assert.That(row.XmlError, Is.Null);
		Assert.That(XDocument.Parse(row.Xml).Root.Name.LocalName, Is.EqualTo("Event"));
		Assert.That(row.PropertiesError, Is.Null);
		Assert.That(row.Properties.All(field => field.ValueType == (field.Value?.GetType().FullName ?? "null")), Is.True);
		var matching = await Tools.Windows.EventLogs.ReadEventsAsync(new WindowsEventLogQuery { Provider = row.Provider, EventIds = new[] { row.EventId } },
			new WindowsEventLogReadOptions(), 2, TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(10), cancellationToken: deadline.Token);
		Assert.That(matching.All(item => item.EventId == row.EventId && item.Provider == row.Provider), Is.True);
	}
}
