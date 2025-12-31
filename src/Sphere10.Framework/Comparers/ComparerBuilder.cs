namespace Sphere10.Framework;

public static class ComparerBuilder {
	public static IdempotentComparer<T> For<T>() => IdempotentComparer<T>.Instance;
}

