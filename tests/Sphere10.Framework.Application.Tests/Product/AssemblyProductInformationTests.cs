// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Application.Tests;

/// <summary>Verifies assembly selection for the existing product-information reader.</summary>
[TestFixture, Parallelizable(ParallelScope.Children)]
public class AssemblyProductInformationTests {
	[Test]
	public void SuppliedAssemblyProvidesItsProductAttributes() {
		var assembly = CreateAssembly();
		var productCode = Guid.NewGuid();
		assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(AssemblyProductAttribute).GetConstructor([typeof(string)]), ["Explicit product"]));
		assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(AssemblyCompanyAttribute).GetConstructor([typeof(string)]), ["Explicit company"]));
		assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(AssemblyDescriptionAttribute).GetConstructor([typeof(string)]), ["Explicit description"]));
		assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(AssemblyCopyrightAttribute).GetConstructor([typeof(string)]), ["Explicit copyright"]));
		assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(AssemblyCompanyNumberAttribute).GetConstructor([typeof(string)]), ["123456"]));
		assembly.SetCustomAttribute(
			new CustomAttributeBuilder(
				typeof(AssemblyCompanyLinkAttribute).GetConstructor([typeof(string)]),
				["https://company.example.invalid"]
			)
		);
		assembly.SetCustomAttribute(
			new CustomAttributeBuilder(
				typeof(AssemblyProductLinkAttribute).GetConstructor([typeof(string)]),
				["https://product.example.invalid"]
			)
		);
		assembly.SetCustomAttribute(
			new CustomAttributeBuilder(
				typeof(AssemblyProductPurchaseLinkAttribute).GetConstructor([typeof(string)]),
				["https://purchase.example.invalid"]
			)
		);
		assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(AssemblyProductCodeAttribute).GetConstructor([typeof(string)]), [productCode.ToString()]));
		assembly.SetCustomAttribute(
			new CustomAttributeBuilder(
				typeof(AssemblyAuthorAttribute).GetConstructor([typeof(string), typeof(string)]),
				["Explicit author", "author@example.invalid"]
			)
		);
		assembly.SetCustomAttribute(
			new CustomAttributeBuilder(
				typeof(AssemblyProductDistributionAttribute).GetConstructor([typeof(ProductDistribution)]),
				[ProductDistribution.Beta]
			)
		);
		assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(AssemblyProductHelpCHMAttribute).GetConstructor([typeof(string)]), ["explicit-help.chm"]));
		assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(AssemblyProductHelpPDFAttribute).GetConstructor([typeof(string)]), ["explicit-help.pdf"]));

		var provider = new AssemblyAttributesProductInformationProvider(assembly);
		var information = provider.ProductInformation;

		Assert.That(information.ProductName, Is.EqualTo("Explicit product"));
		Assert.That(information.CompanyName, Is.EqualTo("Explicit company"));
		Assert.That(information.CompanyNumber, Is.EqualTo("123456"));
		Assert.That(information.CompanyUrl, Is.EqualTo("https://company.example.invalid"));
		Assert.That(information.ProductDescription, Is.EqualTo("Explicit description"));
		Assert.That(information.CopyrightNotice, Is.EqualTo("Explicit copyright"));
		Assert.That(information.ProductUrl, Is.EqualTo("https://product.example.invalid"));
		Assert.That(information.ProductPurchaseUrl, Is.EqualTo("https://purchase.example.invalid"));
		Assert.That(information.ProductCode, Is.EqualTo(productCode));
		Assert.That(information.AuthorName, Is.EqualTo("Explicit author"));
		Assert.That(information.AuthorEmail, Is.EqualTo("author@example.invalid"));
		Assert.That(information.ProductVersion.Major, Is.EqualTo(2));
		Assert.That(information.ProductVersion.Minor, Is.EqualTo(3));
		Assert.That(information.ProductVersion.Build, Is.EqualTo(4));
		Assert.That(information.ProductVersion.Revision, Is.EqualTo(5));
		Assert.That(information.ProductVersion.Distribution, Is.EqualTo(ProductDistribution.Beta));
		Assert.That(information.HelpResources, Is.EquivalentTo(new[] {
			Tuple.Create(HelpType.CHM, "explicit-help.chm"),
			Tuple.Create(HelpType.PDF, "explicit-help.pdf")
		}));
		Assert.That(provider.ProductInformation, Is.SameAs(information));
	}

	[Test]
	public void ParameterlessProviderUsesTheEntryAssembly() {
		var provider = new AssemblyAttributesProductInformationProvider();
		var expected = new AssemblyAttributesProductInformationProvider(Tools.Runtime.GetEntryAssembly()).ProductInformation;
		var information = provider.ProductInformation;

		Assert.That(information.ProductName, Is.EqualTo(expected.ProductName));
		Assert.That(information.CompanyName, Is.EqualTo(expected.CompanyName));
		Assert.That(information.ProductCode, Is.EqualTo(expected.ProductCode));
		Assert.That(information.ProductVersion, Is.EqualTo(expected.ProductVersion));
		Assert.That(information.HelpResources, Is.EquivalentTo(expected.HelpResources));
		Assert.That(provider.ProductInformation, Is.SameAs(information));
	}

	[Test]
	public void MissingAttributesDoNotUseAnotherAssemblysProductInformation() {
		var information = new AssemblyAttributesProductInformationProvider(CreateAssembly()).ProductInformation;

		Assert.That(information.ProductName, Is.Empty);
		Assert.That(information.CompanyName, Is.Empty);
		Assert.That(information.ProductCode, Is.EqualTo(Guid.Empty));
		Assert.That(information.HelpResources, Is.Empty);
		Assert.That(information.ProductVersion.Distribution, Is.EqualTo(ProductDistribution.ReleaseCandidate));
	}

	[Test]
	public void ExplicitAssemblyCannotBeNull() {
		Assert.That(() => new AssemblyAttributesProductInformationProvider(null), Throws.ArgumentNullException);
	}

	// Each fixture owns a collectible assembly so product attributes cannot affect process-wide entry metadata.
	private static AssemblyBuilder CreateAssembly() {
		var name = new AssemblyName("ProductInformation_" + Guid.NewGuid().ToString("N")) { Version = new Version(2, 3, 4, 5) };
		return AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
	}
}
