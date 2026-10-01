// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Typed display, editing and source-sort metadata for one grid column.</summary>
public class BlazorGridColumn<T> {
	private static readonly PropertyMetadata[] _properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
		.Where(property => property.GetIndexParameters().Length == 0 && !property.PropertyType.IsByRef && !property.PropertyType.IsByRefLike && !property.PropertyType.IsPointer
			&& property.GetMethod is { IsPublic: true, IsStatic: false })
		.Select(property => new PropertyMetadata(property)).ToArray();

	public string ColumnName { get; set; }

	public string PropertyName { get; set; }

	public string SortName { get; set; }

	public Type DataType { get; set; } = typeof(string);

	public Func<T, object> PropertyValue { get; set; }

	public Action<T, object> SetPropertyValue { get; set; }

	public bool CanEditCell { get; set; } = true;

	public Func<T, bool> PropertyHasValue { get; set; } = _ => true;

	public int Width { get; set; } = 140;

	public bool ExpandsToFit { get; set; }

	public RenderFragment<T> Template { get; set; }

	public RenderFragment<BlazorGridCellEditContext<T>> EditorTemplate { get; set; }

	public string Format { get; set; }

	public bool CanEdit(T item) => SetPropertyValue != null && CanEditCell && (PropertyHasValue?.Invoke(item) ?? true);

	/// <summary>Builds a column for a direct public property. Nested projections can supply explicit getter and setter delegates.</summary>
	public static BlazorGridColumn<T> For<TValue>(Expression<Func<T, TValue>> property, string columnName = null) {
		Guard.ArgumentNotNull(property, nameof(property));
		var body = property.Body;
		if (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion)
			body = conversion.Operand;
		Guard.Argument(body is MemberExpression { Member: PropertyInfo } member && member.Expression == property.Parameters[0],
			nameof(property), "Select a direct public instance property.");
		var selectedProperty = (PropertyInfo)((MemberExpression)body).Member;
		var metadata = _properties.FirstOrDefault(candidate => candidate.Property.Name == selectedProperty.Name && candidate.Property.DeclaringType == selectedProperty.DeclaringType);
		Guard.Argument(metadata != null, nameof(property), "Select a readable public instance property without index parameters.");
		var column = metadata.CreateColumn();
		if (columnName != null)
			column.ColumnName = columnName;
		return column;
	}

	/// <summary>Creates independent column definitions from cached accessors and component-model metadata.</summary>
	public static BlazorGridColumn<T>[] AutoGenerate() =>
		_properties.Where(property => property.IsBrowsable).Select(property => property.CreateColumn()).ToArray();

	private sealed class PropertyMetadata {
		private readonly Func<T, object> _getter;
		private readonly Action<T, object> _setter;
		private readonly string _displayName;

		public PropertyMetadata(PropertyInfo property) {
			Property = property;
			IsBrowsable = property.GetCustomAttribute<BrowsableAttribute>(true)?.Browsable ?? true;
			_displayName = property.GetCustomAttribute<DisplayNameAttribute>(true)?.DisplayName ?? property.Name;
			var item = Expression.Parameter(typeof(T), "item");
			var access = Expression.Property(item, property);
			_getter = Expression.Lambda<Func<T, object>>(Expression.Convert(access, typeof(object)), item).Compile();
			var setter = property.SetMethod;
			if (!typeof(T).IsValueType && setter is { IsPublic: true, IsStatic: false }
				&& !setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit))
				&& property.GetCustomAttribute<ReadOnlyAttribute>(true)?.IsReadOnly != true) {
				var value = Expression.Parameter(typeof(object), "value");
				_setter = Expression.Lambda<Action<T, object>>(Expression.Call(item, setter, Expression.Convert(value, property.PropertyType)), item, value).Compile();
			}
		}

		public PropertyInfo Property { get; }

		public bool IsBrowsable { get; }

		public BlazorGridColumn<T> CreateColumn() => new() {
			ColumnName = _displayName,
			PropertyName = Property.Name,
			SortName = Property.Name,
			DataType = Property.PropertyType,
			PropertyValue = _getter,
			SetPropertyValue = _setter,
			CanEditCell = Tools.BlazorGrid.IsSupportedType(Property.PropertyType)
		};
	}
}
