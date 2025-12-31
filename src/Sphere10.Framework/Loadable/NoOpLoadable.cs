using System.Threading.Tasks;

namespace Sphere10.Framework;

public class NoOpLoadable : ILoadable {
	public event EventHandlerEx<object> Loading;
	public event EventHandlerEx<object> Loaded;
	public bool RequiresLoad => false;

	public static readonly NoOpLoadable Instance = new();

	public void Load() {
	}

	public Task LoadAsync() => Task.CompletedTask;
}

