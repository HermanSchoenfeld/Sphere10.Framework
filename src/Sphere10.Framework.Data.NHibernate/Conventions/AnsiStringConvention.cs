// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.Instances;

namespace Sphere10.Framework.Data.NHibernate;

public class AnsiStringConvention : IPropertyConvention {
	public void Apply(IPropertyInstance instance) {
		if (instance.Property.PropertyType == typeof(string))
			instance.CustomType("AnsiString");
	}
}

