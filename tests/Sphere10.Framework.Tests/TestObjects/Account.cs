using System.Collections;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Tests;

[EqualityComparer(typeof(AccountEqualityComparer))]
public class Account {

	[Identity]
	public string Name { get; set; }

	public decimal Quantity { get; set; }

	[UniqueIndex]
	public long UniqueNumber { get; set; }	

	public Identity Identity { get; set; }

	[Transient]
	public bool Dirty { get; set; }

}

