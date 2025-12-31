using System;
using System.Linq.Expressions;

namespace Sphere10.Framework.Mapping;

public static class ReflectionExtensions {
	public static Member ToMember<TMapping, TReturn>(this Expression<Func<TMapping, TReturn>> propertyExpression) {
		return Tools.Mapping.GetMember(propertyExpression);
	}
}

