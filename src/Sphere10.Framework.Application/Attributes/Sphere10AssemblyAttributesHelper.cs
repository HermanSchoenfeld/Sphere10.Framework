// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Sphere10.Framework.Application;

/// <summary>Reads product attributes from a supplied assembly or the application entry assembly.</summary>
internal static class Sphere10AssemblyAttributesHelper {

	/// <summary>Reads the assembly title, falling back to its file name.</summary>
	public static string GetAssemblyTitle(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		var attributes = assembly.GetCustomAttributesOfType<AssemblyTitleAttribute>(false);
		if (attributes.Any()) {
			var firstTitleAttr = attributes.First();
			if (!string.IsNullOrWhiteSpace(firstTitleAttr.Title)) {
				return firstTitleAttr.Title;
			}
		}
		return Path.GetFileNameWithoutExtension(assembly.Location);
	}

	/// <summary>Reads the assembly identity version.</summary>
	public static string GetAssemblyVersion(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		return assembly.GetName().Version?.ToString();
	}

	/// <summary>Reads and formats the product description.</summary>
	public static string GetAssemblyDescription(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		var attributes = assembly.GetCustomAttributesOfType<AssemblyDescriptionAttribute>(false);
		return !attributes.Any() ? null : StringFormatter.FormatEx(attributes.First().Description);
	}

	/// <summary>Reads and formats the company website.</summary>
	public static string GetAssemblyCompanyLink(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		var attributes = assembly.GetCustomAttributesOfType<AssemblyCompanyLinkAttribute>(false);
		return !attributes.Any() ? null : StringFormatter.FormatEx(attributes.First().CompanyLink);
	}

	/// <summary>Reads and formats the product website.</summary>
	public static string GetAssemblyProductLink(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		var attributes = assembly.GetCustomAttributesOfType<AssemblyProductLinkAttribute>(false);
		return !attributes.Any() ? null : StringFormatter.FormatEx(attributes.First().ProductLink);
	}

	/// <summary>Reads and formats the product purchase link.</summary>
	public static string GetAssemblyProductPurchaseLink(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		var attributes = assembly.GetCustomAttributesOfType<AssemblyProductPurchaseLinkAttribute>(false);
		return !attributes.Any() ? null : StringFormatter.FormatEx(attributes.First().ProductPurchaseLink);
	}

	/// <summary>Reads and formats the product name.</summary>
	public static string GetAssemblyProduct(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		var attributes = assembly.GetCustomAttributesOfType<AssemblyProductAttribute>(false);
		return !attributes.Any() ? null : StringFormatter.FormatEx(attributes.First().Product);
	}

	/// <summary>Reads the product identifier when declared.</summary>
	public static Guid? GetAssemblyProductCode(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		var attributes = assembly.GetCustomAttributesOfType<AssemblyProductCodeAttribute>(false);
		return !attributes.Any() ? null : attributes.First().ProductCode;
	}

	/// <summary>Reads the product distribution when declared.</summary>
	public static ProductDistribution? GetAssemblyProductDistribution(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		var attributes = assembly.GetCustomAttributesOfType<AssemblyProductDistributionAttribute>(false);
		return !attributes.Any() ? null : attributes.First().Distribution;
	}

	/// <summary>Reads and formats the copyright notice.</summary>
	public static string GetAssemblyCopyright(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		var attributes = assembly.GetCustomAttributesOfType<AssemblyCopyrightAttribute>(false);
		return !attributes.Any() ? null : StringFormatter.FormatEx(attributes.First().Copyright);
	}

	/// <summary>Reads and formats the company name.</summary>
	public static string GetAssemblyCompany(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		var attributes = assembly.GetCustomAttributesOfType<AssemblyCompanyAttribute>(false);
		return !attributes.Any() ? null : StringFormatter.FormatEx(attributes.First().Company);
	}

	/// <summary>Reads and formats the company registration number.</summary>
	public static string GetAssemblyCompanyNumber(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		var attributes = assembly.GetCustomAttributesOfType<AssemblyCompanyNumberAttribute>(false);
		return !attributes.Any() ? null : StringFormatter.FormatEx(attributes.First().CompanyNumber);
	}

	//public static int GetHelpTopic(ApplicationScreen screen) {
	//    int retval = 0;
	//    object[] attributes = screen.GetType().GetCustomAttributes(typeof(HelpIDAttribute), true);
	//    // return first one
	//    if (attributes.Length > 0) {
	//        retval = ((HelpIDAttribute)attributes[0]).HelpID;
	//    }
	//    return retval;
	//}

	/// <summary>Reads the declared product help resources.</summary>
	public static IList<Tuple<HelpType, string>> GetAssemblyProductHelpResources(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		return (
				from helpAttribute in assembly.GetCustomAttributesOfType<AssemblyProductHelpResourceAttribute>(false)
				select Tuple.Create(helpAttribute.HelpType, helpAttribute.Path)
			)
			.ToList()
			.AsReadOnly();
	}

	/// <summary>Reads and formats the author name.</summary>
	public static string GetAssemblyAuthorName(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		var attributes = assembly.GetCustomAttributesOfType<AssemblyAuthorAttribute>(false);
		return !attributes.Any() ? null : StringFormatter.FormatEx(attributes.First().Name);
	}

	/// <summary>Reads and formats the author email address.</summary>
	public static string GetAssemblyAuthorEmail(Assembly assembly = null) {
		assembly ??= Tools.Runtime.GetEntryAssembly();
		var attributes = assembly.GetCustomAttributesOfType<AssemblyAuthorAttribute>(false);
		return !attributes.Any() ? null : StringFormatter.FormatEx(attributes.First().Email ?? string.Empty);
	}

}

