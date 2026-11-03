// Copyright 2005-2015 Giacomo Stelluti Scala & Contributors. All rights reserved. See License.md in the project root for license information.

using System;
using System.Reflection;
using CSharpx;

namespace CommandLine.Core;

class SpecificationProperty {
	private readonly Maybe<object> value;

	private SpecificationProperty(Specification specification, PropertyInfo property, Maybe<object> value) {
		Property = property;
		Specification = specification;
		this.value = value;
	}

	public static SpecificationProperty Create(Specification specification, PropertyInfo property, Maybe<object> value) {
		if (value == null) throw new ArgumentNullException("value");

		return new SpecificationProperty(specification, property, value);
	}

	public Specification Specification { get; }

	public PropertyInfo Property { get; }

	public Maybe<object> Value => value;
}

