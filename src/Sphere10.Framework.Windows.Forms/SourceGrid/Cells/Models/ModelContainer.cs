// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

/// <summary>
/// A objectStream for the model classes. THe only required model is the Value model, that you can assign using the ValueModel property.
/// </summary>
public class ModelContainer {
	private ModelList _modelList = null;
	private IValueModel _valueModel;

	/// <summary>
	/// Constructor
	/// </summary>
	public ModelContainer() {
	}

	public virtual IValueModel ValueModel {
		get => _valueModel;
		set => _valueModel = value;
	}

	/// <summary>
	/// Returns null if not exist
	/// </summary>
	/// <param name="modelType"></param>
	/// <returns></returns>
	public virtual IModel FindModel(Type modelType) {
		if (_valueModel != null && modelType.IsAssignableFrom(_valueModel.GetType()))
			return _valueModel;
		if (_modelList == null)
			_modelList = new ModelList();

		return _modelList.GetByType(modelType);
	}

	public virtual ModelContainer AddModel(IModel model) {
		if (model == null)
			throw new ArgumentNullException();
		var type = model.GetType();

		if (typeof(IValueModel).IsAssignableFrom(type))
			_valueModel = (IValueModel)model;
		else {
			if (_modelList == null)
				_modelList = new ModelList();

			_modelList.Add(model);
		}
		return this;
	}

	public virtual ModelContainer RemoveModel(IModel model) {
		if (object.ReferenceEquals(model, _valueModel))
			_valueModel = null;
		else if (_modelList != null)
			_modelList.Remove(model);
		return this;
	}

	/// <summary>
	/// A collection of elements of type IModel
	/// </summary>
	public class ModelList : List<IModel> {
		/// <summary>Returns the first model assignable to the requested type.</summary>
		public IModel GetByType(Type searchType) => this.FirstOrDefault(model => searchType.IsAssignableFrom(model.GetType()));
	}
}
