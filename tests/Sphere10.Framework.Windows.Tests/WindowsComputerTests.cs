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
using NUnit.Framework;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[NonParallelizable]
public class WindowsComputerTests {
	[Test]
	public async Task ReadOnlyIdentityProcessorsAndOperatingSystemReturnTypedDetachedData() {
		var computer = new ComputerUtil();
		var identity = await computer.QueryAsync();
		Assert.That(identity.Name, Is.EqualTo(Environment.MachineName).IgnoreCase);
		Assert.That(identity.TotalPhysicalMemory, Is.GreaterThan(0));
		var processors = await computer.GetProcessorsAsync();
		Assert.That(processors, Is.Not.Empty);
		Assert.That(processors.All(processor => !string.IsNullOrEmpty(processor.DeviceId)), Is.True);
		var systems = await computer.GetOperatingSystemsAsync();
		Assert.That(systems, Is.Not.Empty);
		Assert.That(systems.All(system => !string.IsNullOrEmpty(system.Name) && !string.IsNullOrEmpty(system.Version)), Is.True);
		Assert.That(systems.Any(system => string.Equals(system.SystemDirectory, Environment.SystemDirectory, StringComparison.OrdinalIgnoreCase)), Is.True);
		Assert.That(systems.Where(system => system.LastBootUpTime.HasValue).All(system => system.LastBootUpTime.Value.Kind == DateTimeKind.Utc), Is.True);
	}

	[Test]
	public async Task FirmwareAndProductQueriesReturnDetachedArraysWithoutRequiringOptionalHardwareFields() {
		var computer = new ComputerUtil();
		var bios = await computer.GetBiosAsync();
		var boards = await computer.GetBaseboardsAsync();
		var products = await computer.GetSystemProductsAsync();
		Assert.That(bios, Is.Not.Null);
		Assert.That(boards, Is.Not.Null);
		Assert.That(products, Is.Not.Null);
		Assert.That(bios.All(row => row != null), Is.True);
		Assert.That(boards.All(row => row != null), Is.True);
		Assert.That(products.All(row => row != null), Is.True);
		Assert.That(bios.Where(row => row.ReleaseDate.HasValue).All(row => row.ReleaseDate.Value.Kind == DateTimeKind.Utc), Is.True);
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("**************.******+***")]
	[TestCase("2026**********.000000+000")]
	[TestCase("not-a-dmtf-date")]
	public void UnknownOrMalformedProviderDatesRemainUnavailableInsteadOfFailingTheInventory(string text) {
		Assert.That(WindowsComputer.ParseDmtfDate(text), Is.Null);
	}

	[Test]
	public void CompleteProviderDatesAreConvertedToUtc() {
		Assert.That(WindowsComputer.ParseDmtfDate("20260102030405.000000+000"), Is.EqualTo(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)));
	}

	[TestCase("identity")]
	[TestCase("products")]
	[TestCase("bios")]
	[TestCase("baseboards")]
	[TestCase("processors")]
	[TestCase("os")]
	public async Task PreCanceledQueriesDoNotStartWindowsProviderCollection(string query) {
		var computer = new ComputerUtil();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var pending = query switch {
			"identity" => (Task)computer.QueryAsync(cancellation.Token),
			"products" => computer.GetSystemProductsAsync(cancellation.Token),
			"bios" => computer.GetBiosAsync(cancellation.Token),
			"baseboards" => computer.GetBaseboardsAsync(cancellation.Token),
			"processors" => computer.GetProcessorsAsync(cancellation.Token),
			_ => computer.GetOperatingSystemsAsync(cancellation.Token)
		};
		await Assert.ThatAsync(async () => await pending, Throws.InstanceOf<OperationCanceledException>());
	}
}
