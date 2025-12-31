using Sphere10.Framework;


public interface IStreamMappedRecylableList : IRecyclableList, IStreamMappedCollection {
	long Count { get; }
}

