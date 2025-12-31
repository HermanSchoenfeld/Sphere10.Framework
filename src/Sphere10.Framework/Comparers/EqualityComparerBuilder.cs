namespace Sphere10.Framework;

public static class EqualityComparerBuilder {
	public static IdempotentEqualityComparer<T> For<T>() => IdempotentEqualityComparer<T>.Instance;
}

