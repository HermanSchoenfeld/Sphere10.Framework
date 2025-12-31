using System.Collections.Generic;

namespace Sphere10.Framework;

public interface IReadOnlyDictionaryList<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IReadOnlyList<TValue> {
	int IndexOf(TKey key);
}

