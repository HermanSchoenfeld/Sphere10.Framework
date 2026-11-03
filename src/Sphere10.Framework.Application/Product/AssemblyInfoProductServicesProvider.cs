// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Reflection;

namespace Sphere10.Framework.Application;

/// <summary>Reads product information from assembly attributes and delegates operational services to their existing registrations.</summary>
public class AssemblyInfoProductServicesProvider : ProductServicesProvider {
	/// <summary>Selects the supplied product assembly, or the entry assembly when omitted.</summary>
	public AssemblyInfoProductServicesProvider(IServiceProvider services, Assembly assembly = null)
		: base(new AssemblyAttributesProductInformationProvider(assembly ?? Tools.Runtime.GetEntryAssembly()), services) {
	}
}
