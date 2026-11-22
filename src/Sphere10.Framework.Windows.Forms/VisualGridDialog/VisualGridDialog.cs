// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

// Configures and shows the modal grid over a borrowed source.
// VisualGridDialog.Designer.cs owns the form controls and native layout.
public partial class VisualGridDialog : Form {
	private Func<Task> _delayedInitializationAction;

	public VisualGridDialog() => InitializeComponent();

	public static Task ShowAsync<TEntity>(IWin32Window window, string title, IEnumerable<IVisualGridColumn> gridBindings, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource)
		=> ShowAsync(window, title, gridBindings, typeof(VisualEntityEditor), capabilities, dataSource);

	public static Task ShowAsync<TEntity>(string title, IEnumerable<IVisualGridColumn> gridBindings, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource)
		=> ShowAsync(null, title, gridBindings, typeof(VisualEntityEditor), capabilities, dataSource);

	public static Task ShowAsync<TEntity>(string title, IEnumerable<IVisualGridColumn> gridBindings, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type entityEditorType, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource)
		=> ShowAsync(null, title, gridBindings, entityEditorType, capabilities, dataSource);

	public static async Task ShowAsync<TEntity>(IWin32Window window, string title, IEnumerable<IVisualGridColumn> gridBindings, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type entityEditorType, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource) {
		using (var gridDialog = new VisualGridDialog()) {
			gridDialog.Text = title;
			await gridDialog.SetParametersAsync(gridBindings, entityEditorType, capabilities, dataSource);
			await gridDialog.ShowDialogAsync(window);
		}
	}

	public async Task SetParametersAsync<TEntity>(IEnumerable<IVisualGridColumn> gridBindings, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type entityEditorType, DataSourceCapabilities capabilities, IDataSource<TEntity> dataSource) {
		var initializationAction =
			new Func<Task>(async () => {
				try {
					if (entityEditorType != null)
						_visualGrid.SetEntityEditor<TEntity>(entityEditorType);
					_visualGrid.GridBindings = gridBindings?.ToArray();
					await _visualGrid.SetDataSource(dataSource, capabilities);
				} catch (Exception error) {
					await ExceptionDialog.ShowAsync(this, error);
				}
			});

		if (!IsHandleCreated)
			_delayedInitializationAction = initializationAction;
		else
			await initializationAction();
	}

	protected override async void OnLoad(EventArgs e) {
		base.OnLoad(e);
		try {
			if (_delayedInitializationAction != null) {
				var initialization = _delayedInitializationAction;
				_delayedInitializationAction = null;
				await initialization();
			}
			await _visualGrid.RefreshGrid();
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void _okButton_Click(object sender, EventArgs e) {
		try {
			Close();
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}


}

