using System;
using System.Collections.Generic;
using System.Text;

namespace Sphere10.Framework;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class TransientAttribute : Attribute {
}

