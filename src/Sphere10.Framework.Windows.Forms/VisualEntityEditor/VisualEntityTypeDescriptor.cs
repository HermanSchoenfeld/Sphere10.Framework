// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

// Descriptors belong to one editing session; model types and their global TypeDescriptor metadata remain untouched.
internal sealed class VisualEntityTypeDescriptor : CustomTypeDescriptor {
	private readonly object _entity;
	private readonly IDictionary<string, VisualGridReferenceBindingBase> _referenceBindings;
	private readonly bool _readOnly;
	private readonly HashSet<string> _hiddenProperties;
	private readonly Func<object, PropertyDescriptor, bool> _propertyFilter;
	private readonly List<PropertyEdit> _edits = new();
	private readonly List<CollectionEdit> _collectionEdits = new();

	public VisualEntityTypeDescriptor(object entity, IDictionary<string, VisualGridReferenceBindingBase> referenceBindings, bool readOnly = false, IEnumerable<string> hiddenProperties = null,
		Func<object, PropertyDescriptor, bool> propertyFilter = null
	)
		: base(TypeDescriptor.GetProvider(entity).GetTypeDescriptor(entity)) {
		_entity = entity;
		_referenceBindings = referenceBindings;
		_readOnly = readOnly;
		_hiddenProperties = new HashSet<string>(hiddenProperties ?? Array.Empty<string>(), StringComparer.Ordinal);
		_propertyFilter = propertyFilter;
	}

	public bool HasChanges => _edits.Any(edit => edit.Property.IsActive && !Equals(edit.OriginalValue, edit.Property.GetValue(null)))
		|| _collectionEdits.Any(edit => edit.Property.IsActive && !edit.Items.SequenceEqual(edit.Collection.Cast<object>()));

	public override PropertyDescriptorCollection GetProperties() => GetProperties(null);

	public override PropertyDescriptorCollection GetProperties(Attribute[] attributes) => WrapProperties(_entity, base.GetProperties(attributes), null);

	public override object GetPropertyOwner(PropertyDescriptor property) => _entity;

	public void AcceptChanges() {
		_edits.Clear();
		_collectionEdits.Clear();
	}

	public void UndoChanges() {
		// Restore actual owners in reverse edit order, preserving shared references and cycles.
		foreach (var edit in _edits.AsEnumerable().Reverse())
			if (!Equals(edit.Property.GetValue(null), edit.OriginalValue))
				edit.Property.RestoreValue(edit.OriginalValue);
		foreach (var edit in _collectionEdits) {
			if (edit.Items.SequenceEqual(edit.Collection.Cast<object>()))
				continue;
			if (edit.Collection.IsFixedSize) {
				for (var index = 0; index < edit.Items.Length; index++)
					edit.Collection[index] = edit.Items[index];
			} else {
				edit.Collection.Clear();
				foreach (var item in edit.Items)
					edit.Collection.Add(item);
			}
		}
		AcceptChanges();
	}

	private PropertyDescriptorCollection WrapProperties(object owner, PropertyDescriptorCollection properties, EditablePropertyDescriptor parent) {
		var visible = properties.Cast<PropertyDescriptor>().Where(property => (!ReferenceEquals(owner, _entity) || !_hiddenProperties.Contains(property.Name))
			&& (_propertyFilter == null || _propertyFilter(owner, property)));
		return new PropertyDescriptorCollection(visible.Select(property => new EditablePropertyDescriptor(this, owner, property, parent)).ToArray(), true);
	}

	private void RecordEdit(EditablePropertyDescriptor property) {
		if (!_edits.Any(edit => ReferenceEquals(edit.Property.Owner, property.Owner) && edit.Property.Name == property.Name))
			_edits.Add(new PropertyEdit(property, property.GetValue(null)));
	}

	private void CaptureEditorValues(EditablePropertyDescriptor property, ReferenceHashSet<object> visited) {
		if (property.CanRestore)
			RecordEdit(property);
		var value = property.GetValue(null);
		if (value == null || value is string || value.GetType().IsValueType || !visited.Add(value))
			return;
		if (value is IList collection && !collection.IsReadOnly) {
			if (!_collectionEdits.Any(edit => ReferenceEquals(edit.Collection, collection)))
				_collectionEdits.Add(new CollectionEdit(property, collection, collection.Cast<object>().ToArray()));
			return;
		}
		if (value is IEnumerable)
			return;
		foreach (var child in WrapProperties(value, TypeDescriptor.GetProperties(value), property).Cast<EditablePropertyDescriptor>())
			CaptureEditorValues(child, visited);
	}

	#region Inner Types

	private sealed record PropertyEdit(EditablePropertyDescriptor Property, object OriginalValue);

	private sealed record CollectionEdit(EditablePropertyDescriptor Property, IList Collection, object[] Items);

	private sealed class EditablePropertyDescriptor : PropertyDescriptor {
		private readonly VisualEntityTypeDescriptor _session;
		private readonly PropertyDescriptor _property;
		private readonly EditablePropertyDescriptor _parent;
		private readonly TypeConverter _converter;
		private readonly VisualGridReferenceBindingBase _referenceBinding;
		private readonly PathSelectorAttribute _pathSelector;

		public EditablePropertyDescriptor(VisualEntityTypeDescriptor session, object owner, PropertyDescriptor property, EditablePropertyDescriptor parent)
			: base(property) {
			_session = session;
			Owner = owner;
			_property = property;
			_parent = parent;
			session._referenceBindings.TryGetValue(Path, out _referenceBinding);
			// A marked string is chosen with the path selector; so is each entry of a marked string collection, which keeps its own editor.
			_pathSelector = property.PropertyType != typeof(string) ? null
				: (property is ListValueProperty ? parent?._property : property)?.Attributes.OfType<PathSelectorAttribute>().FirstOrDefault();
			_converter = _referenceBinding != null ? new VisualGridReferenceConverter(_referenceBinding) : CanExpandAutomatically || property.Converter.GetPropertiesSupported()
				? new NestedPropertyConverter(this, property.Converter)
				: property.Converter;
		}

		public object Owner { get; }

		public string Path => _parent == null ? Name : $"{_parent.Path}.{Name}";

		public bool IsActive => _parent == null || _parent.IsActive && ReferenceEquals(_parent.GetValue(null), Owner);

		public bool IsCircular {
			get {
				var value = GetValue(null);
				for (var ancestor = this; ancestor != null; ancestor = ancestor._parent)
					if (ReferenceEquals(value, ancestor.Owner))
						return true;
				return false;
			}
		}

		public bool CanExpandAutomatically => _referenceBinding == null && (PropertyType.IsClass || PropertyType.IsInterface) && PropertyType != typeof(string)
			&& (!typeof(IEnumerable).IsAssignableFrom(PropertyType) || typeof(IDictionary).IsAssignableFrom(PropertyType) || typeof(IList).IsAssignableFrom(PropertyType)) && !typeof(Delegate).IsAssignableFrom(PropertyType)
			&& (typeof(IDictionary).IsAssignableFrom(PropertyType) || typeof(IList).IsAssignableFrom(PropertyType) || _property.Converter.GetType() == typeof(TypeConverter) || PropertyType.IsInterface && _property.Converter.GetType() == typeof(ReferenceConverter));

		public bool CanCreate => CanExpandAutomatically && !IsReadOnly && !PropertyType.IsAbstract && PropertyType.GetConstructor(Type.EmptyTypes) != null;

		public override string DisplayName => _property.DisplayName;

		public override Type ComponentType => _property.ComponentType;

		public override Type PropertyType => _property.PropertyType;

		public override bool IsReadOnly => _session._readOnly || _property.IsReadOnly || (_parent?.IsReadOnly ?? false);

		public bool CanRestore => !_property.IsReadOnly;

		public override TypeConverter Converter => _converter;

		public override bool SupportsChangeEvents => _property.SupportsChangeEvents;

		public override object GetValue(object component) => _property.GetValue(Owner);

		public override void SetValue(object component, object value) {
			Guard.Ensure(!IsReadOnly, $"The property '{Name}' is read-only.");
			if (CanCreate && value is string)
				value = Converter.ConvertFrom(value);
			_session.RecordEdit(this);
			_property.SetValue(Owner, value);
		}

		public override void ResetValue(object component) {
			Guard.Ensure(!IsReadOnly, $"The property '{Name}' is read-only.");
			_session.RecordEdit(this);
			_property.ResetValue(Owner);
		}

		public override bool CanResetValue(object component) => !IsReadOnly && _property.CanResetValue(Owner);

		public override bool ShouldSerializeValue(object component) => _property.ShouldSerializeValue(Owner);

		public override object GetEditor(Type editorBaseType) {
			if (_session._readOnly)
				return null;
			if (_referenceBinding != null && editorBaseType == typeof(UITypeEditor))
				return IsReadOnly ? null : new VisualGridReferenceEditor(_referenceBinding);
			if (_pathSelector != null && editorBaseType == typeof(UITypeEditor))
				return IsReadOnly ? null : new PathSelectorEditor(_pathSelector);
			var editor = _property.GetEditor(editorBaseType);
			return editor is UITypeEditor valueEditor ? new RecordingValueEditor(this, valueEditor) : editor;
		}

		public void CaptureEditorValues() => _session.CaptureEditorValues(this, new ReferenceHashSet<object>());

		public override void AddValueChanged(object component, EventHandler handler) => _property.AddValueChanged(Owner, handler);

		public override void RemoveValueChanged(object component, EventHandler handler) => _property.RemoveValueChanged(Owner, handler);

		public void RestoreValue(object value) => _property.SetValue(Owner, value);

		public PropertyDescriptorCollection GetChildren(object value, Attribute[] attributes, ITypeDescriptorContext context) {
			// Declared dictionary roles are fixed navigation keys, with editable nested values instead of an opaque collection dialog.
			var properties = value is IDictionary dictionary
				? new PropertyDescriptorCollection(dictionary.Keys.Cast<object>().Select(key => new DictionaryValueProperty(dictionary, key)).ToArray(), true)
				: value is IList list
					? new PropertyDescriptorCollection(Enumerable.Range(0, list.Count).Select(index => new ListValueProperty(list, index)).ToArray(), true)
					: CanExpandAutomatically ? TypeDescriptor.GetProperties(value, attributes) : _property.Converter.GetProperties(context, value, attributes);
			// Let WinForms manage boxed values and immutable converters through their owning property setter.
			return value.GetType().IsValueType && !_session._readOnly ? properties ?? new PropertyDescriptorCollection(null)
				: _session.WrapProperties(value, properties ?? new PropertyDescriptorCollection(null), this);
		}
	}

	// This descriptor edits a declared role's value; adding/removing/renaming role keys remains product schema configuration.
	private sealed class DictionaryValueProperty(IDictionary dictionary, object key) : PropertyDescriptor(key.ToString(), null) {
		public override Type ComponentType => dictionary.GetType();

		public override Type PropertyType => dictionary[key]?.GetType() ?? typeof(object);

		public override bool IsReadOnly => dictionary.IsReadOnly;

		public override object GetValue(object component) => dictionary[key];

		public override void SetValue(object component, object value) => dictionary[key] = value;

		public override bool CanResetValue(object component) => false;

		public override void ResetValue(object component) { }

		public override bool ShouldSerializeValue(object component) => false;
	}

	// Ordinal paths remain stable inside the editor; meaningful value labels make nested column widths readable.
	private sealed class ListValueProperty(IList list, int index) : PropertyDescriptor($"[{index}]", null) {
		public override Type ComponentType => list.GetType();

		public override Type PropertyType => list.GetType().IsArray ? list.GetType().GetElementType()
			: list.GetType().GetInterfaces().FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>))?.GetGenericArguments()[0]
				?? list[index]?.GetType() ?? typeof(object);

		public override string DisplayName {
			get {
				var value = list[index];
				var label = value?.ToString();
				return value != null && value is not string && !value.GetType().IsValueType && !string.IsNullOrWhiteSpace(label)
					&& label != value.GetType().FullName && label != value.GetType().Name ? label : base.DisplayName;
			}
		}

		public override bool IsReadOnly => list.IsReadOnly;

		public override object GetValue(object component) => list[index];

		public override void SetValue(object component, object value) => list[index] = value;

		public override bool CanResetValue(object component) => false;

		public override void ResetValue(object component) { }

		public override bool ShouldSerializeValue(object component) => false;
	}

	private sealed class RecordingValueEditor : UITypeEditor {
		private readonly EditablePropertyDescriptor _property;
		private readonly UITypeEditor _editor;

		public RecordingValueEditor(EditablePropertyDescriptor property, UITypeEditor editor) {
			_property = property;
			_editor = editor;
		}

		public override bool IsDropDownResizable => _editor.IsDropDownResizable;

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
			_property.CaptureEditorValues();
			return _editor.EditValue(context, provider, value);
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => _editor.GetEditStyle(context);

		public override bool GetPaintValueSupported(ITypeDescriptorContext context) => _editor.GetPaintValueSupported(context);

		public override void PaintValue(PaintValueEventArgs args) => _editor.PaintValue(args);
	}

	private sealed class NestedPropertyConverter : TypeConverter {
		private const string CreateValue = "(Create new)";
		private const string EmptyValue = "(none)";
		private readonly EditablePropertyDescriptor _property;
		private readonly TypeConverter _converter;

		public NestedPropertyConverter(EditablePropertyDescriptor property, TypeConverter converter) {
			_property = property;
			_converter = converter;
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
			_property.CanCreate && sourceType == typeof(string) || _converter.CanConvertFrom(context, sourceType);

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => _converter.CanConvertTo(context, destinationType);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
			if (_property.CanCreate && value is string text) {
				if (text == CreateValue)
					return Tools.Object.Create(_property.PropertyType);
				if (text == EmptyValue)
					return null;

				// PropertyGrid paints standard choices by round-tripping their displayed captions.
				if (_property.GetValue(null) is { } current && text == ConvertToString(context, culture, current))
					return current;
			}
			return _converter.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
			if (destinationType == typeof(string)) {
				if (value is IDictionary dictionary)
					return dictionary.Count == 1 ? "1 entry" : $"{dictionary.Count} entries";
				if (value is ICollection collection)
					return collection.Count == 1 ? "1 item" : $"{collection.Count} items";
			}
			if (_property.CanExpandAutomatically && destinationType == typeof(string)) {
				if (value == null)
					return EmptyValue;
				if (value is string)
					return value;
				var summary = value.ToString();
				var valueType = value.GetType();
				// Preserve entity captions and meaningful ToString overrides; marked options use an expansion prompt.
				if (summary == valueType.FullName || string.IsNullOrWhiteSpace(summary))
					summary = valueType.IsDefined(typeof(UserEditableAttribute), true) ? "Expand to edit" : valueType.Name;
				return _property.IsCircular ? $"{summary} (already shown)" : summary;
			}
			return _converter.ConvertTo(context, culture, value, destinationType);
		}

		public override bool GetPropertiesSupported(ITypeDescriptorContext context) => !_property.IsCircular
			&& (_property.CanExpandAutomatically || _converter.GetPropertiesSupported(context));

		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) =>
			_property.IsCircular ? new PropertyDescriptorCollection(null) : _property.GetChildren(value, attributes, context);

		public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => _property.CanCreate || _converter.GetStandardValuesSupported(context);

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => _property.CanCreate || _converter.GetStandardValuesExclusive(context);

		public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) =>
			_property.CanCreate ? new StandardValuesCollection(_property.GetValue(null) is { } current
				? new object[] { current, null, CreateValue } : new object[] { null, CreateValue }) : _converter.GetStandardValues(context);

		public override bool GetCreateInstanceSupported(ITypeDescriptorContext context) => _converter.GetCreateInstanceSupported(context);

		public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues) => _converter.CreateInstance(context, propertyValues);

		public override bool IsValid(ITypeDescriptorContext context, object value) => _converter.IsValid(context, value);
	}

	#endregion
}
