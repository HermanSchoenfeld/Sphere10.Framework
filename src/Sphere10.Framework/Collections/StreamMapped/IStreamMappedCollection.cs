using System;
using System.Collections;
using System.Collections.Generic;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework;

public interface IStreamMappedCollection {
	ObjectStream ObjectStream { get; }
	
	void Clear();
}

public interface IStreamMappedCollection<TItem> : IStreamMappedCollection {
	new ObjectStream<TItem> ObjectStream { get; }

	#region Base Interface Bridges

	ObjectStream IStreamMappedCollection.ObjectStream => ObjectStream;

	#endregion
}

/// <summary>Defines a stream-mapped collection of logical key/value entries independently of its physical storage layout.</summary>
public interface IStreamMappedCollection<TKey, TValue> : IStreamMappedCollection, ICollection<KeyValuePair<TKey, TValue>> {
	new void Clear();

	#region Base Interface Bridges

	void IStreamMappedCollection.Clear() => Clear();

	void ICollection<KeyValuePair<TKey, TValue>>.Clear() => Clear();

	#endregion
}

