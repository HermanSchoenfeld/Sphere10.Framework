using System;

namespace Sphere10.Framework;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class IdentityAttribute : Attribute {
	public string IndexName { get; set; } = null;
}


