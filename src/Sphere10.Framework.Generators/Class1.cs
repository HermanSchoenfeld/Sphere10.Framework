using System.ComponentModel;

namespace Sphere10.Framework.Generators {
	
	public interface IObjectSpaceObject : INotifyPropertyChanging, INotifyPropertyChanged {
		bool Dirty { get; set; }
	}

	public class Class1 : INotifyPropertyChanging, INotifyPropertyChanged {

		public event PropertyChangingEventHandler? PropertyChanging;
		public event PropertyChangedEventHandler? PropertyChanged;
	}
}

