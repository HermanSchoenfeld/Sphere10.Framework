namespace Sphere10.Framework;

public record Sphere10FrameworkFileDescriptor : TransactionalFileDescriptor {

	public int ClusterSize { get; init; }

	public ClusteredStreamsPolicy ContainerPolicy { get; init; }

	public Endianness Endianness { get; init; }

	public new static Sphere10FrameworkFileDescriptor From(string path) 
		=> From(path, Sphere10FrameworkDefaults.TransactionalPageFolder, Sphere10FrameworkDefaults.TransactionalPageSize, Sphere10FrameworkDefaults.MaxMemoryPerCollection, Sphere10FrameworkDefaults.ClusterSize, Sphere10FrameworkDefaults.ContainerPolicy);

	public new static Sphere10FrameworkFileDescriptor From(string path, long pageSize = Sphere10FrameworkDefaults.TransactionalPageSize, long maxMemory = Sphere10FrameworkDefaults.MaxMemoryPerCollection)
		=> From(path, Sphere10FrameworkDefaults.TransactionalPageFolder, pageSize, maxMemory);

	private new static Sphere10FrameworkFileDescriptor From(string path, string pagesDirectoryPath, long pageSize = Sphere10FrameworkDefaults.TransactionalPageSize, long maxMemory = Sphere10FrameworkDefaults.MaxMemoryPerCollection) 
		=> From(path, pagesDirectoryPath, pageSize, maxMemory, Sphere10FrameworkDefaults.ClusterSize, Sphere10FrameworkDefaults.ContainerPolicy);


	public static Sphere10FrameworkFileDescriptor From(string path, long pageSize = Sphere10FrameworkDefaults.TransactionalPageSize, long maxMemory = Sphere10FrameworkDefaults.MaxMemoryPerCollection, int clusterSize = Sphere10FrameworkDefaults.ClusterSize, ClusteredStreamsPolicy containerPolicy = Sphere10FrameworkDefaults.ContainerPolicy)
		=> From(path, Sphere10FrameworkDefaults.TransactionalPageFolder, pageSize, maxMemory, clusterSize, containerPolicy);

	public static Sphere10FrameworkFileDescriptor From(string path, string pagesDirectoryPath, long pageSize = Sphere10FrameworkDefaults.TransactionalPageSize, long maxMemory = Sphere10FrameworkDefaults.MaxMemoryPerCollection, int clusterSize = Sphere10FrameworkDefaults.ClusterSize, ClusteredStreamsPolicy containerPolicy = Sphere10FrameworkDefaults.ContainerPolicy, Endianness endianness = Sphere10FrameworkDefaults.Endianness) 
		=> new() { 
			Path = path,
			PagesDirectoryPath = pagesDirectoryPath,
			PageSize = pageSize,
			MaxMemory = maxMemory,
			ClusterSize = clusterSize,
			ContainerPolicy = containerPolicy,
			Endianness = endianness
		};
}


