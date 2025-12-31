using System;
using System.Collections;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework;

public interface IStreamMappedCollection {
	ObjectStream ObjectStream { get; }
	
	void Clear();
}

public interface IStreamMappedCollection<TItem> : IStreamMappedCollection {
	new ObjectStream<TItem> ObjectStream { get; }

}

