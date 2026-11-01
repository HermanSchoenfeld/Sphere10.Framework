using System;

namespace Sphere10.Framework.Windows.Forms;

public class CrudEntityPropertyChangedEventArgs(object entity, string propertyName, object oldValue, object newValue) : EventArgs {
	public object Entity { get; set; } = entity;
	public string PropertyName { get; } = propertyName;
	public object OldValue { get; } = oldValue;
	public object NewValue { get; } = newValue;

}
