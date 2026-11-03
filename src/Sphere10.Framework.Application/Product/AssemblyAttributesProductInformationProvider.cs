// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Reflection;

namespace Sphere10.Framework.Application;

/// <summary>Reads and retains product information from the selected assembly's attributes.</summary>
public class AssemblyAttributesProductInformationProvider : IProductInformationProvider {
	private readonly Assembly _assembly;

	/// <summary>Uses the application's entry assembly for product information.</summary>
	public AssemblyAttributesProductInformationProvider()
		: this(Tools.Runtime.GetEntryAssembly()) {
	}

	/// <summary>Uses the supplied product assembly without changing process-wide assembly selection.</summary>
	public AssemblyAttributesProductInformationProvider(Assembly assembly) {
		Guard.ArgumentNotNull(assembly, nameof(assembly));
		_assembly = assembly;
	}

	/// <summary>The product information read once from this provider's assembly.</summary>
	public ProductInformation ProductInformation {
		get {
			if (field != null)
				return field;
			lock (this) {
				field ??= GetProductInformation();
			}
			return field;
		}
	}

	/// <summary>Reads the existing product attributes through their shared helper.</summary>
	protected ProductInformation GetProductInformation() {

		var version = ApplicationVersion.Parse(Sphere10AssemblyAttributesHelper.GetAssemblyVersion(_assembly));
		version.Distribution = Sphere10AssemblyAttributesHelper.GetAssemblyProductDistribution(_assembly) ?? ProductDistribution.ReleaseCandidate;

		return new ProductInformation {
			CompanyName = Sphere10AssemblyAttributesHelper.GetAssemblyCompany(_assembly) ?? string.Empty,
			CompanyNumber = Sphere10AssemblyAttributesHelper.GetAssemblyCompanyNumber(_assembly) ?? string.Empty,
			CompanyUrl = Sphere10AssemblyAttributesHelper.GetAssemblyCompanyLink(_assembly) ?? string.Empty,
			CopyrightNotice = Sphere10AssemblyAttributesHelper.GetAssemblyCopyright(_assembly) ?? string.Empty,
			ProductCode = Sphere10AssemblyAttributesHelper.GetAssemblyProductCode(_assembly) ?? Guid.Empty,
			ProductDescription = Sphere10AssemblyAttributesHelper.GetAssemblyDescription(_assembly) ?? string.Empty,
			ProductName = Sphere10AssemblyAttributesHelper.GetAssemblyProduct(_assembly) ?? string.Empty,
			ProductPurchaseUrl = Sphere10AssemblyAttributesHelper.GetAssemblyProductPurchaseLink(_assembly) ?? string.Empty,
			ProductUrl = Sphere10AssemblyAttributesHelper.GetAssemblyProductLink(_assembly) ?? string.Empty,
			AuthorName = Sphere10AssemblyAttributesHelper.GetAssemblyAuthorName(_assembly) ?? string.Empty,
			AuthorEmail = Sphere10AssemblyAttributesHelper.GetAssemblyAuthorEmail(_assembly) ?? string.Empty,
			ProductVersion = version,
			HelpResources = Sphere10AssemblyAttributesHelper.GetAssemblyProductHelpResources(_assembly)
		};
	}

}

