using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class EqualityComparerAttribute(Type type) : Attribute {

	public Type EqualityComparerType { get; } = type;

}

