using System;

namespace Sphere10.Framework.Tests.ObjectSpaces;

[Flags]
public enum TestTraits {
	MemoryMapped,
	FileMapped,
	Merklized,
	PersistentIgnorant
}

