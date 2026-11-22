// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms;

public class VisualEntityPropertyChangedEventArgs(object entity, string propertyName, object oldValue, object newValue) : EventArgs {
	public object Entity { get; set; } = entity;
	public string PropertyName { get; } = propertyName;
	public object OldValue { get; } = oldValue;
	public object NewValue { get; } = newValue;

}
