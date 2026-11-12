// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using FluentNHibernate.Conventions;
using FluentNHibernate.Conventions.AcceptanceCriteria;
using FluentNHibernate.Conventions.Inspections;
using FluentNHibernate.Conventions.Instances;

namespace Sphere10.Framework.Data.NHibernate;

/// <summary>Selects provider decimal reads for ordinary decimal properties without changing their SQL column definitions.</summary>
public class DecimalColumnConvention : IPropertyConvention, IPropertyConventionAcceptance {
	public void Accept(IAcceptanceCriteria<IPropertyInspector> criteria)
		=> criteria.Expect(property => property.Type == typeof(decimal) || property.Type == typeof(decimal?));

	public void Apply(IPropertyInstance instance) => instance.CustomType<DecimalColumnType>();
}
