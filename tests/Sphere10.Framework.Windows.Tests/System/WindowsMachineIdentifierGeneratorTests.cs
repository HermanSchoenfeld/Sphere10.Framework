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
using IdentityComponent = Sphere10.Framework.Windows.MachineIdentity.Component;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WindowsMachineIdentifierGeneratorTests {
	private static readonly IdentityComponent[] SampleMachine = {
		new("BaseBoard.Manufacturer", "Contoso"),
		new("Processor", "BFEBFBFF000906EA"),
		new("Bios", "SN-1")
	};

	[Test]
	public void GenerateIsDeterministicAcrossCallsAndInstances() {
		var generator = new WindowsMachineIdentifierGenerator();
		var first = GenerateOrIgnore(generator);
		Assert.That(generator.Generate(), Is.EqualTo(first), "The same instance returns the same identifier.");
		Assert.That(GenerateOrIgnore(new WindowsMachineIdentifierGenerator()), Is.EqualTo(first), "A new instance reads the same hardware and computes the same identifier.");
		Assert.That(GenerateOrIgnore(new WindowsMachineIdentifierGenerator(MachineIdentitySources.Default)), Is.EqualTo(first), "The parameterless selection is the default selection.");
	}

	[Test]
	public void GenerateReturnsUpperCaseHexadecimalOfSha256Length() {
		var identifier = GenerateOrIgnore(new WindowsMachineIdentifierGenerator());
		Assert.That(identifier, Has.Length.EqualTo(Hashers.GetDigestSizeBytes(CHF.SHA2_256) * 2));
		Assert.That(identifier, Does.Match("^[0-9A-F]{64}$"));
	}

	[Test]
	public void GenerateChangesWhenTheSelectedSourcesChange() {
		var narrow = MachineIdentitySources.BaseBoard;
		var added = MachineIdentitySources.Processor;
		var addedComponents = ReadOrIgnore(added);
		Assume.That(addedComponents.Any(component => !MachineIdentity.IsPlaceholder(MachineIdentity.Normalize(component.Value))), Is.True,
			"This machine reports no processor value, so adding it cannot change its identifier.");
		Assert.That(GenerateOrIgnore(new WindowsMachineIdentifierGenerator(narrow | added)), Is.Not.EqualTo(GenerateOrIgnore(new WindowsMachineIdentifierGenerator(narrow))));
	}

	[Test]
	public void ReadComponentsReadsOnlyTheSelectedSources() {
		Assert.That(ReadOrIgnore(MachineIdentitySources.Processor).Select(component => component.Name), Is.All.EqualTo(nameof(MachineIdentitySources.Processor)));
		Assert.That(ReadOrIgnore(MachineIdentitySources.BaseBoard).Select(component => component.Name), Is.All.StartsWith(nameof(MachineIdentitySources.BaseBoard) + "."));
		Assert.That(ReadOrIgnore(MachineIdentitySources.PhysicalNetworkAdapters).Select(component => component.Name), Is.All.EqualTo(nameof(MachineIdentitySources.PhysicalNetworkAdapters)));
		Assert.That(ReadOrIgnore(MachineIdentitySources.SystemDisk).Select(component => component.Name), Is.All.EqualTo(nameof(MachineIdentitySources.SystemDisk)));
		Assert.That(ReadOrIgnore(MachineIdentitySources.Default).Select(component => component.Name), Has.None.EqualTo("SystemUuid"), "The system UUID belongs to BIOSMachineIdentifier only.");
	}

	[Test]
	public void ComputeIdentifierIsTheSha256OfTheOrderedSourceLines() {
		// These vectors pin the composition, so identifiers that services have stored stay valid across releases.
		Assert.That(MachineIdentity.ComputeIdentifier(new[] { new IdentityComponent("Bios", "ABC") }),
			Is.EqualTo("A145FFCDA838EC13722378870E0C8CBD39CA79C712AD386C4A1F394500F09356"));
		Assert.That(MachineIdentity.ComputeIdentifier(SampleMachine),
			Is.EqualTo("50D077EF4697C00E22B32F6D19E157A23D32CD4B389FCCF3C4F42920549F72B7"));
	}

	[Test]
	public void ComputeIdentifierIgnoresEnumerationOrderAndDuplicates() {
		var reordered = SampleMachine.Reverse().Concat(new[] { SampleMachine[1] }).ToArray();
		Assert.That(MachineIdentity.ComputeIdentifier(reordered), Is.EqualTo(MachineIdentity.ComputeIdentifier(SampleMachine)));
	}

	[Test]
	public void ComputeIdentifierNormalisesWhitespaceAndCase() {
		var raw = new[] {
			new IdentityComponent("BaseBoard.Manufacturer", "contoso "),
			new IdentityComponent("Processor", "\tbfebfbff000906ea"),
			new IdentityComponent("Bios", "\r\nsn-1")
		};
		Assert.That(MachineIdentity.ComputeIdentifier(raw), Is.EqualTo(MachineIdentity.ComputeIdentifier(SampleMachine)));
	}

	[TestCase("To be filled by O.E.M.")]
	[TestCase("To Be Filled By O.E.M.")]
	[TestCase("Default string")]
	[TestCase("None")]
	[TestCase("Not Specified")]
	[TestCase("System Serial Number")]
	[TestCase("")]
	[TestCase("   ")]
	[TestCase(null)]
	[TestCase("00000000-0000-0000-0000-000000000000")]
	[TestCase("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF")]
	[TestCase("ffffffff-ffff-ffff-ffff-ffffffffffff")]
	[TestCase("00:00:00:00:00:00")]
	[TestCase("0000000000")]
	public void ComputeIdentifierDropsPlaceholderValues(string placeholder) {
		var withPlaceholder = SampleMachine.Append(new IdentityComponent("SystemDisk", placeholder)).ToArray();
		Assert.That(MachineIdentity.IsPlaceholder(MachineIdentity.Normalize(placeholder)), Is.True);
		Assert.That(MachineIdentity.ComputeIdentifier(withPlaceholder), Is.EqualTo(MachineIdentity.ComputeIdentifier(SampleMachine)));
	}

	[TestCase("4C4C4544-0042-3510-8051-B4C04F563232")]
	[TestCase("00:1A:2B:3C:4D:5E")]
	[TestCase("BFEBFBFF000906EA")]
	[TestCase("CONTOSO")]
	public void IsPlaceholderKeepsMeaningfulValues(string value)
		=> Assert.That(MachineIdentity.IsPlaceholder(MachineIdentity.Normalize(value)), Is.False);

	[Test]
	public void ComputeIdentifierChangesWhenAValueChanges() {
		var changed = SampleMachine.Select(component => component.Name == "Bios" ? component with { Value = "SN-2" } : component).ToArray();
		Assert.That(MachineIdentity.ComputeIdentifier(changed), Is.Not.EqualTo(MachineIdentity.ComputeIdentifier(SampleMachine)));
	}

	[Test]
	public void ComputeIdentifierChangesWhenASourceIsAddedOrRemoved() {
		var baseline = MachineIdentity.ComputeIdentifier(SampleMachine);
		var added = SampleMachine.Append(new IdentityComponent("SystemDisk", "S3Z9NB0K123456")).ToArray();
		Assert.That(MachineIdentity.ComputeIdentifier(added), Is.Not.EqualTo(baseline));
		Assert.That(MachineIdentity.ComputeIdentifier(SampleMachine.Skip(1)), Is.Not.EqualTo(baseline));
	}

	[Test]
	public void ComputeIdentifierSeparatesTheSameValueUnderDifferentSources()
		=> Assert.That(MachineIdentity.ComputeIdentifier(new[] { new IdentityComponent("Bios", "ABC") }),
			Is.Not.EqualTo(MachineIdentity.ComputeIdentifier(new[] { new IdentityComponent("SystemDisk", "ABC") })));

	[Test]
	public void ComputeIdentifierThrowsWhenNoSourceYieldsAValue() {
		var placeholders = new[] { new IdentityComponent("Bios", "To be filled by O.E.M."), new IdentityComponent("BaseBoard.SerialNumber", "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF") };
		Assert.That(() => MachineIdentity.ComputeIdentifier(placeholders), Throws.InvalidOperationException);
		Assert.That(() => MachineIdentity.ComputeIdentifier(Array.Empty<IdentityComponent>()), Throws.InvalidOperationException);
	}

	[TestCase(@"PCI\VEN_8086&DEV_15F3&SUBSYS_00008086&REV_03\3&11583659&0&E8", true)]
	[TestCase(@"pci\ven_10ec&dev_8168", true)]
	[TestCase(@"USB\VID_0BDA&PID_8153\000001", false)]
	[TestCase(@"ROOT\VMS_MP\0000", false)]
	[TestCase(@"ROOT\NET\0000", false)]
	[TestCase(@"SWD\MSRRAS\MS_NDISWANIP", false)]
	[TestCase(@"VMBUS\{F8615163-DF3E-46C5-913F-F2D2F965ED0E}\{1234}", false)]
	[TestCase(@"BTH\MS_BTHPAN\6&2B7F1A2E&0&2", false)]
	[TestCase(@"COMPOSITE\{12345678}\0000", false)]
	[TestCase(null, false)]
	public void OnlyAdaptersInstalledOnThePciBusAreAccepted(string pnpDeviceId, bool expected)
		=> Assert.That(WindowsMachineIdentifierGenerator.IsInstalledPciDevice(pnpDeviceId), Is.EqualTo(expected));

	[Test]
	public void ConstructorSelectsTheDefaultSourcesAndRejectsAnEmptyOrUndefinedSelection() {
		Assert.That(new WindowsMachineIdentifierGenerator().Sources, Is.EqualTo(MachineIdentitySources.Default));
		Assert.That(MachineIdentitySources.Default, Is.EqualTo(MachineIdentitySources.BaseBoard | MachineIdentitySources.Processor | MachineIdentitySources.Bios));
		Assert.That(() => new WindowsMachineIdentifierGenerator(MachineIdentitySources.None), Throws.ArgumentException);
		Assert.That(() => new WindowsMachineIdentifierGenerator((MachineIdentitySources)(1 << 5)), Throws.ArgumentException);
	}

	private static string GenerateOrIgnore(WindowsMachineIdentifierGenerator generator) {
		try {
			return generator.Generate();
		} catch (ManagementException error) when (error.ErrorCode is ManagementStatus.InvalidClass or ManagementStatus.InvalidNamespace or ManagementStatus.AccessDenied) {
			Assert.Ignore($"Windows provider unavailable: {error.ErrorCode}. No identifier was manufactured.");
			return null;
		}
	}

	private static IdentityComponent[] ReadOrIgnore(MachineIdentitySources sources) {
		try {
			return WindowsMachineIdentifierGenerator.ReadComponents(sources);
		} catch (ManagementException error) when (error.ErrorCode is ManagementStatus.InvalidClass or ManagementStatus.InvalidNamespace or ManagementStatus.AccessDenied) {
			Assert.Ignore($"Windows provider unavailable for {sources}: {error.ErrorCode}. No identity values were manufactured.");
			return null;
		}
	}
}
