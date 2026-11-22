// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public abstract class VisualEntityEditorAdapterBase : IVisualEntityEditor<object> {
	public abstract event EventHandlerEx<VisualEntityPropertyChangedEventArgs> PropertyChanged;

	public abstract bool HasChanges { get; }

	public abstract void SetAdaptee(object @object);

	public abstract Control AsControl();

	public abstract void SetEntity(DataSourceCapabilities capabilities, object entity, bool isNewEntity);

	public abstract object GetEntityWithChanges();

	public abstract void UndoChanges();

	public abstract void AcceptChanges();
}
