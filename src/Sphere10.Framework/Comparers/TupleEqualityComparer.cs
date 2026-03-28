using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere10.Framework;

public class TupleEqualityComparer<T1, T2> : IEqualityComparer<(T1, T2)> {
	private readonly IEqualityComparer<T1> _item1Comparer;
	private readonly IEqualityComparer<T2> _item2Comparer;
	public TupleEqualityComparer(IEqualityComparer<T1> item1Comparer, IEqualityComparer<T2> item2Comparer) {
		_item1Comparer = item1Comparer;
		_item2Comparer = item2Comparer;
	}
	public bool Equals((T1, T2) x, (T1, T2) y) => _item1Comparer.Equals(x.Item1, y.Item1) && _item2Comparer.Equals(x.Item2, y.Item2);
	public int GetHashCode((T1, T2) obj) => HashCode.Combine(_item1Comparer.GetHashCode(obj.Item1), _item2Comparer.GetHashCode(obj.Item2));
}

public class TupleEqualityComparer<T1, T2, T3> : IEqualityComparer<(T1, T2, T3)> {
	private readonly IEqualityComparer<T1> _item1Comparer;
	private readonly IEqualityComparer<T2> _item2Comparer;
	private readonly IEqualityComparer<T3> _item3Comparer;
	public TupleEqualityComparer(IEqualityComparer<T1> item1Comparer, IEqualityComparer<T2> item2Comparer, IEqualityComparer<T3> item3Comparer) {
		_item1Comparer = item1Comparer;
		_item2Comparer = item2Comparer;
		_item3Comparer = item3Comparer;
	}
	public bool Equals((T1, T2, T3) x, (T1, T2, T3) y) => 
		_item1Comparer.Equals(x.Item1, y.Item1) && 
		_item2Comparer.Equals(x.Item2, y.Item2) && 
		_item3Comparer.Equals(x.Item3, y.Item3);
	public int GetHashCode((T1, T2, T3) obj) => HashCode.Combine(
		_item1Comparer.GetHashCode(obj.Item1), 
		_item2Comparer.GetHashCode(obj.Item2), 
		_item3Comparer.GetHashCode(obj.Item3));
}