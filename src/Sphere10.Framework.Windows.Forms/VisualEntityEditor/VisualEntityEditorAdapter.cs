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

public class VisualEntityEditorAdapter<TEntity> : VisualEntityEditorAdapterBase {
	public override event EventHandlerEx<VisualEntityPropertyChangedEventArgs> PropertyChanged {
		add => _adaptee.PropertyChanged += value;
		remove => _adaptee.PropertyChanged -= value;
	}

	private IVisualEntityEditor<TEntity> _adaptee;

	public VisualEntityEditorAdapter() {
	}

	public VisualEntityEditorAdapter(IVisualEntityEditor<TEntity> adaptee) => SetAdaptee(adaptee);

	public override bool HasChanges => _adaptee.HasChanges;

	public override void SetAdaptee(object @object) {
		Guard.Argument(@object is IVisualEntityEditor<TEntity>, null, $"Does not implement {typeof(IVisualEntityEditor<TEntity>).Name}");
		_adaptee = (IVisualEntityEditor<TEntity>)@object;
	}

	public override Control AsControl() => _adaptee.AsControl();

	public override void SetEntity(DataSourceCapabilities capabilities, object entity, bool isNewEntity)
		=> _adaptee.SetEntity(capabilities, (TEntity)entity, isNewEntity);

	public override object GetEntityWithChanges() => _adaptee.GetEntityWithChanges();

	public override void UndoChanges() => _adaptee.UndoChanges();

	public override void AcceptChanges() => _adaptee.AcceptChanges();
}
