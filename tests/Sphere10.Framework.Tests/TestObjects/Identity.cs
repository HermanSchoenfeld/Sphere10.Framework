using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Tests;

public class Identity {

	public DSS DSS { get; set; }

	[UniqueIndex]
	public byte[] Key { get; set; }

	[Index]
	public int Group { get; set; }

	[Transient]
	public bool Dirty { get; set; }

}

