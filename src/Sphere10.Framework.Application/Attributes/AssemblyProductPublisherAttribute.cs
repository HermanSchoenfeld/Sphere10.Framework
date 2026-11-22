// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Application;

/// <summary>
/// Declares who publishes the product: a company, or a person publishing under their own name. Product information reads it
/// into <see cref="ProductInformation.PublisherName"/> and <see cref="ProductInformation.PublisherIsCompany"/>.
/// </summary>
[ComVisible(true)]
[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class AssemblyProductPublisherAttribute : Attribute {

	public AssemblyProductPublisherAttribute(string name) {
		Name = name;
	}

	public string Name { get; init; }

	public bool IsCompany { get; set; }

}
