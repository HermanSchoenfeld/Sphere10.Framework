using System;

namespace Sphere10.Framework.Tests.ObjectSpaces;

[Flags]
public enum TestTraits {
	MemoryMapped = 1 << 0,
	FileMapped = 1 << 1,
	Merklized = 1 << 2,
	PersistentIgnorant = 1 << 3
}

