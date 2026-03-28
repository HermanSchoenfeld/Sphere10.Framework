using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sphere10.Framework;

public class TupleComparer<T1, T2> : IComparer<(T1, T2)> {
	private readonly IComparer<T1> _item1Comparer;
	private readonly IComparer<T2> _item2Comparer;
	public TupleComparer(IComparer<T1> item1Comparer, IComparer<T2> item2Comparer) {
		_item1Comparer = item1Comparer;
		_item2Comparer = item2Comparer;
	}
	public int Compare((T1, T2) x, (T1, T2) y) {
		var result = _item1Comparer.Compare(x.Item1, y.Item1);
		return result == 0 ? _item2Comparer.Compare(x.Item2, y.Item2) : result;
	}
}


public class TupleComparer<T1, T2, T3> : IComparer<(T1, T2, T3)> {
	private readonly IComparer<T1> _item1Comparer;
	private readonly IComparer<T2> _item2Comparer;
	private readonly IComparer<T3> _item3Comparer;
	public TupleComparer(IComparer<T1> item1Comparer, IComparer<T2> item2Comparer, IComparer<T3> item3Comparer) {
		_item1Comparer = item1Comparer;
		_item2Comparer = item2Comparer;
		_item3Comparer = item3Comparer;
	}
	public int Compare((T1, T2, T3) x, (T1, T2, T3) y) {
		var result = _item1Comparer.Compare(x.Item1, y.Item1);
		if (result != 0) return result;
		result = _item2Comparer.Compare(x.Item2, y.Item2);
		return result == 0 ? _item3Comparer.Compare(x.Item3, y.Item3) : result;
	}
}