using System.Threading.Tasks;

namespace Sphere10.Framework;

public class NoOpLoadable : ILoadable {
	// This no-op implementation never raises load notifications.
	public event EventHandlerEx<object> Loading { add { } remove { } }
	public event EventHandlerEx<object> Loaded { add { } remove { } }
	public bool RequiresLoad => false;

	public static readonly NoOpLoadable Instance = new();

	public void Load() {
	}

	public Task LoadAsync() => Task.CompletedTask;
}

