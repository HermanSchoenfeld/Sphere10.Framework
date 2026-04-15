namespace Sphere10.Framework;

public interface IValueTypeActivatingSerializer : IItemSerializer {
	bool ShouldNotifyInstanceActivation { get; set; }
}
