using System;

namespace Sphere10.Framework;

[Flags]
public enum EntropyPolicy {
	UseGuid = 1 << 0,
	UseDateTime = 1 << 1,
	UseEnvironment = 1 << 2,
	UseBigEndianNotLittle = 1 << 3,
	Default = UseGuid | UseDateTime | UseEnvironment
}
