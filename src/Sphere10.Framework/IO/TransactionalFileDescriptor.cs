namespace Sphere10.Framework;

public record TransactionalFileDescriptor : PagedFileDescriptor {
	
	public string PagesDirectoryPath { get; init; }

	public new static TransactionalFileDescriptor From(string path) 
		=> From(path, Sphere10FrameworkDefaults.TransactionalPageFolder,  Sphere10FrameworkDefaults.TransactionalPageSize, Sphere10FrameworkDefaults.MaxMemoryPerCollection);

	public new static TransactionalFileDescriptor From(string path, long pageSize = Sphere10FrameworkDefaults.TransactionalPageSize, long maxMemory = Sphere10FrameworkDefaults.MaxMemoryPerCollection)
		=> From(path, Sphere10FrameworkDefaults.TransactionalPageFolder, pageSize, maxMemory);

	public static TransactionalFileDescriptor From(string path, string pagesDirectoryPath, long pageSize = Sphere10FrameworkDefaults.TransactionalPageSize, long maxMemory = Sphere10FrameworkDefaults.MaxMemoryPerCollection) 
		=> new() { 
			Path = path,
			PagesDirectoryPath = pagesDirectoryPath,
			PageSize = pageSize,
			MaxMemory = maxMemory
		};

}


