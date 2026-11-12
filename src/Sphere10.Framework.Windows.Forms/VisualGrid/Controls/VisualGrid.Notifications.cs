// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;

namespace Sphere10.Framework.Windows.Forms;

public partial class VisualGrid {
	private DataSourceViewSubscription<object> _sourceViewSubscription;
	private int _sourceNotificationEdits;
	private bool _sourceNotificationRefreshing;

	private void ObserveDataSourceNotifications(IDataSource<object> source, DataSourceCapabilities available) {
		if (!available.HasFlag(DataSourceCapabilities.CanNotify))
			return;
		_sourceViewSubscription = new DataSourceViewSubscription<object>(this, source, () => RequestGridRefresh(true),
			error => (ResolveSettingsLogger() ?? SystemLog.Logger).Exception(error, "Could not present changed grid data."),
			() => {
				if (!ReferenceEquals(source, Volatile.Read(ref _dataSource)))
					return false;
				NotifyDataSourceChanged();
				return true;
			}, () => State == VisualState.Normal && _crudCapabilities.HasFlag(DataSourceCapabilities.CanRead)
				&& _sourceNotificationEdits == 0 && !IsPreferenceCellEditing());
	}

	private void ReleaseDataSourceNotifications() {
		_sourceViewSubscription?.Dispose();
		_sourceViewSubscription = null;
	}

	private void QueueDataSourceNotification() {
		_sourceViewSubscription?.TryPresent();
	}

	private IDisposable EnterSourceNotificationEdit() {
		_sourceNotificationEdits++;
		return Tools.Scope.ExecuteOnDispose(() => {
			_sourceNotificationEdits--;
			QueueDataSourceNotification();
		});
	}
}
