// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Management;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BIOSMachineIdentifierTests {
	private const string SampleUuid = "4C4C4544-0042-3510-8051-B4C04F563232";

	[Test]
	public void GenerateIsDeterministicUpperCaseHexadecimalAcrossCallsAndInstances() {
		var identifier = new BIOSMachineIdentifier();
		var first = GenerateOrIgnore(identifier);
		Assert.That(first, Does.Match("^[0-9A-F]{64}$"));
		Assert.That(identifier.Generate(), Is.EqualTo(first));
		Assert.That(GenerateOrIgnore(new BIOSMachineIdentifier()), Is.EqualTo(first));
	}

	[Test]
	public void GenerateHashesTheFirmwareSystemUuid() {
		var products = Tools.Windows.Computer.GetSystemProducts();
		Assume.That(products.Any(product => !MachineIdentity.IsPlaceholder(MachineIdentity.Normalize(product.Uuid))), Is.True, "This machine's firmware reports no system UUID.");
		Assert.That(GenerateOrIgnore(new BIOSMachineIdentifier()), Is.EqualTo(BIOSMachineIdentifier.ComputeIdentifier(products)));
		Assert.That(GenerateOrIgnore(new BIOSMachineIdentifier()), Is.Not.EqualTo(new WindowsMachineIdentifierGenerator().Generate()), "The UUID identity is not the hardware-source identity.");
	}

	[Test]
	public void ComputeIdentifierIsTheSha256OfTheNormalisedUuidLine() {
		// This vector pins the composition, so identifiers that services have stored stay valid across releases.
		Assert.That(BIOSMachineIdentifier.ComputeIdentifier(new[] { Product(SampleUuid) }), Is.EqualTo("B43339C890F1A36F1C6A2BEAE6E88C4B505F9BA0FE09D3C7EBD2B82AF47E47EF"));
		Assert.That(BIOSMachineIdentifier.ComputeIdentifier(new[] { Product(" " + SampleUuid.ToLowerInvariant() + "\t") }), Is.EqualTo("B43339C890F1A36F1C6A2BEAE6E88C4B505F9BA0FE09D3C7EBD2B82AF47E47EF"));
	}

	[TestCase("00000000-0000-0000-0000-000000000000")]
	[TestCase("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")]
	[TestCase("To be filled by O.E.M.")]
	[TestCase("")]
	[TestCase(null)]
	public void ComputeIdentifierRefusesAPlaceholderUuid(string uuid)
		=> Assert.That(() => BIOSMachineIdentifier.ComputeIdentifier(new[] { Product(uuid) }), Throws.InvalidOperationException);

	[Test]
	public void ComputeIdentifierRefusesFirmwareWithoutAProduct()
		=> Assert.That(() => BIOSMachineIdentifier.ComputeIdentifier(Array.Empty<WindowsSystemProductInfo>()), Throws.InvalidOperationException);

	private static WindowsSystemProductInfo Product(string uuid) => new("Product", "Vendor", "1.0", "Serial", uuid);

	private static string GenerateOrIgnore(BIOSMachineIdentifier identifier) {
		try {
			return identifier.Generate();
		} catch (ManagementException error) when (error.ErrorCode is ManagementStatus.InvalidClass or ManagementStatus.InvalidNamespace or ManagementStatus.AccessDenied) {
			Assert.Ignore($"Windows provider unavailable: {error.ErrorCode}. No identifier was manufactured.");
			return null;
		}
	}
}
