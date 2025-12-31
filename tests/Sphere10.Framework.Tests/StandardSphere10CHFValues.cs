using NUnit.Framework;

namespace Sphere10.Framework.Tests;

internal class StandardSphere10CHFValues : ValuesAttribute {
	public StandardSphere10CHFValues()
		: base(CHF.ConcatBytes, CHF.SHA2_256, CHF.SHA2_384, CHF.SHA2_512, CHF.SHA1_160, CHF.Blake2b_512, CHF.Blake2b_384, CHF.Blake2b_256, CHF.Blake2b_160, CHF.Blake2b_128) {
	}
}

