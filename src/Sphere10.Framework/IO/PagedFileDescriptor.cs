namespace Sphere10.Framework;

public record PagedFileDescriptor {

	public string Path { get; init; } 

	public string CaseCorrectPath => Tools.FileSystem.GetCaseCorrectFilePath(Path);

	public long PageSize { get; init; }

	public long MaxMemory { get; init; }

	public static PagedFileDescriptor From(string path) 
		=> From(path, Sphere10FrameworkDefaults.TransactionalPageSize, Sphere10FrameworkDefaults.MaxMemoryPerCollection);

	public static PagedFileDescriptor From(string path, long pageSize = Sphere10FrameworkDefaults.TransactionalPageSize, long maxMemory = Sphere10FrameworkDefaults.MaxMemoryPerCollection) 
		=> new() { 
			Path = path,
			PageSize = pageSize,
			MaxMemory = maxMemory
		};

}


