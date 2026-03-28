using System.Diagnostics;
using Sphere10.Framework;

namespace SystemExpert.Screens;

public class ProcessInfoDataSource : ProjectedDataSource<Process, ProcessInfo> {
	private readonly ProcessDataSource _processDataSource;

	public ProcessInfoDataSource()
		: this(new ProcessDataSource()) {
	}

	private ProcessInfoDataSource(ProcessDataSource processDataSource)
		: base(processDataSource, ProcessInfo.FromProcess) {
		_processDataSource = processDataSource;
	}

	public void Invalidate() => _processDataSource.Invalidate();
}
