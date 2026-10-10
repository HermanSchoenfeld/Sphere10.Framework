using System.Collections.Generic;

namespace Sphere10.Framework;

public interface IReadOnlyDictionaryList<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IReadOnlyList<TValue> {
	int IndexOf(TKey key);

	#region Base Interface Bridges

	int IReadOnlyCollection<TValue>.Count => ((IReadOnlyDictionary<TKey, TValue>)this).Count;

	IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => ((IReadOnlyDictionary<TKey, TValue>)this).Values.GetEnumerator();

	#endregion
}

