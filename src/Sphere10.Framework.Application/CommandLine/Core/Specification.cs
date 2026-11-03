// Copyright 2005-2015 Giacomo Stelluti Scala & Contributors. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommandLine.Infrastructure;
using CSharpx;

namespace CommandLine.Core;

enum SpecificationType {
	Option,
	Value
}


enum TargetType {
	Switch,
	Scalar,
	Sequence
}


abstract class Specification {

	/// This information is denormalized to decouple Specification from PropertyInfo.
	private readonly Type conversionType;

	protected Specification(SpecificationType tag, bool required, Maybe<int> min, Maybe<int> max,
	                        Maybe<object> defaultValue, string helpText, string metaValue, IEnumerable<string> enumValues,
	                        Type conversionType, TargetType targetType, bool hidden = false) {
		Tag = tag;
		Required = required;
		Min = min;
		Max = max;
		DefaultValue = defaultValue;
		this.conversionType = conversionType;
		TargetType = targetType;
		HelpText = helpText;
		MetaValue = metaValue;
		EnumValues = enumValues;
		Hidden = hidden;
	}

	public SpecificationType Tag { get; }

	public bool Required { get; }

	public Maybe<int> Min { get; }

	public Maybe<int> Max { get; }

	public Maybe<object> DefaultValue { get; }

	public string HelpText { get; }

	public string MetaValue { get; }

	public IEnumerable<string> EnumValues { get; }

	public Type ConversionType => conversionType;

	public TargetType TargetType { get; }

	public bool Hidden { get; }

	public static Specification FromProperty(PropertyInfo property) {
		var attrs = property.GetCustomAttributes(true);
		var oa = attrs.OfType<OptionAttribute>();
		if (oa.Count() == 1) {
			var spec = OptionSpecification.FromAttribute(oa.Single(),
				property.PropertyType,
				ReflectionHelper.GetNamesOfEnum(property.PropertyType));

			if (spec.ShortName.Length == 0 && spec.LongName.Length == 0) {
				return spec.WithLongName(property.Name.ToLowerInvariant());
			}
			return spec;
		}

		var va = attrs.OfType<ValueAttribute>();
		if (va.Count() == 1) {
			return ValueSpecification.FromAttribute(va.Single(),
				property.PropertyType,
				property.PropertyType.GetTypeInfo().IsEnum
					? Enum.GetNames(property.PropertyType)
					: Enumerable.Empty<string>());
		}

		throw new InvalidOperationException();
	}
}

