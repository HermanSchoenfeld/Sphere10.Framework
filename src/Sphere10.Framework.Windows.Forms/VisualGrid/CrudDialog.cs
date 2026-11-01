// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

public partial class CrudDialog : Form {
	private Func<Task> _delayedInitializationAction;

	public CrudDialog() {
		InitializeComponent();
		_delayedInitializationAction = null;
	}

	protected override async void OnLoad(EventArgs e) {
		base.OnLoad(e);
		try {
			if (_delayedInitializationAction != null) {
				var Initialization = _delayedInitializationAction;
				_delayedInitializationAction = null;
				await Initialization();
			}
			await _crudGrid.RefreshGrid();
		} catch (Exception Error) {
			await ExceptionDialog.ShowAsync(this, Error);
		}
	}

	public static Task ShowAsync<TEntity>(IWin32Window window, string title, IEnumerable<ICrudGridColumn> gridBindings, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource) {
		return ShowAsync(window, title, gridBindings, typeof(DefaultCrudEntityEditor), capabilities, dataSource);
	}

	public static Task ShowAsync<TEntity>(string title, IEnumerable<ICrudGridColumn> gridBindings, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource) {
		return ShowAsync(null, title, gridBindings, typeof(DefaultCrudEntityEditor), capabilities, dataSource);
	}

	public static Task ShowAsync<TEntity>(string title, IEnumerable<ICrudGridColumn> gridBindings, Type entityEditorType, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource) {
		return ShowAsync(null, title, gridBindings, entityEditorType, capabilities, dataSource);
	}

	public static async Task ShowAsync<TEntity>(IWin32Window window, string title, IEnumerable<ICrudGridColumn> gridBindings, Type entityEditorType, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource) {
		using var crudDialog = new CrudDialog();
		crudDialog.Text = title;
		await crudDialog.SetCrudParameters(gridBindings, entityEditorType, capabilities, dataSource);
		await crudDialog.ShowDialogAsync(window);
	}

	public async Task SetCrudParameters<TEntity>(IEnumerable<ICrudGridColumn> gridBindings, Type entityEditorType, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource) {
		var initializationAction =
			new Func<Task>(async () => {
				try {
					if (entityEditorType != null)
						_crudGrid.SetEntityEditor<TEntity>(entityEditorType);
					_crudGrid.GridBindings = gridBindings;
					await _crudGrid.SetDataSource(dataSource);
					_crudGrid.Capabilities = capabilities;
				} catch (Exception error) {
					await ExceptionDialog.ShowAsync(this, error);
				}
			});

		if (!IsHandleCreated)
			_delayedInitializationAction = initializationAction;
		else
			await initializationAction();
	}

	private async void _okButton_Click(object sender, EventArgs e) {
		try {
			Close();
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}


}

