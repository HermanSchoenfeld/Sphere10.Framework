// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading.Tasks;
using Sphere10.Framework.Windows.Forms.SourceGrid;

namespace Sphere10.Framework.Windows.Forms;

internal class UpdateEntityOnValueChangedController : SourceGrid.Cells.Controllers.ControllerBase {
	private readonly CrudGrid _grid;
	private readonly IDataSource<object> _dataSource;
	private readonly ICrudGridColumn _columnBinding;
	private object _entity;

	public UpdateEntityOnValueChangedController(CrudGrid grid, IDataSource<object> dataSource, ICrudGridColumn column, object entity) {
		_grid = grid;
		_dataSource = dataSource;
		_columnBinding = column;
		_entity = entity;
	}

	public override async void OnValueChanged(CellContext sender, EventArgs e) {
		// set the entity's property with the new cell value
		_columnBinding.SetCellValue(_entity, sender.Value);

		// validate the change via the data source
		var result = await _dataSource.ValidateAsync(_entity, CrudAction.Update);
		if (result.IsFailure) {
			DialogEx.Show(sender.Grid, SystemIconType.Error, "Unable to update", result.ErrorMessages.ToParagraphCase(), "OK");
			_entity = await _dataSource.RefreshAsync(_entity);
		} else {
			await _dataSource.UpdateAsync(_entity);
		}
		_grid.NotifyEntityUpdated(_entity);
	}
}

