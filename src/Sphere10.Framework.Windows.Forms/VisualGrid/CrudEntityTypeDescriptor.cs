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
using System.Globalization;
using System.Drawing.Design;
using System.Linq;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

// Descriptors belong to one editing session; model types and their global TypeDescriptor metadata remain untouched.
internal sealed class CrudEntityTypeDescriptor : CustomTypeDescriptor {
	private readonly object _entity;
	private readonly IDictionary<string, CrudReferenceBinding> _referenceBindings;
	private readonly bool _readOnly;
	private readonly HashSet<string> _hiddenProperties;
	private readonly Func<object, PropertyDescriptor, bool> _propertyFilter;
	private readonly List<PropertyEdit> _edits = new();
	private readonly List<CollectionEdit> _collectionEdits = new();

	public CrudEntityTypeDescriptor(object Entity, IDictionary<string, CrudReferenceBinding> ReferenceBindings, bool readOnly = false, IEnumerable<string> hiddenProperties = null,
		Func<object, PropertyDescriptor, bool> propertyFilter = null)
		: base(TypeDescriptor.GetProvider(Entity).GetTypeDescriptor(Entity)) {
		_entity = Entity;
		_referenceBindings = ReferenceBindings;
		_readOnly = readOnly;
		_hiddenProperties = new HashSet<string>(hiddenProperties ?? Array.Empty<string>(), StringComparer.Ordinal);
		_propertyFilter = propertyFilter;
	}

	public bool HasChanges => _edits.Any(Edit => Edit.Property.IsActive && !Equals(Edit.OriginalValue, Edit.Property.GetValue(null)))
		|| _collectionEdits.Any(Edit => Edit.Property.IsActive && !Edit.Items.SequenceEqual(Edit.Collection.Cast<object?>()));

	public override PropertyDescriptorCollection GetProperties() => GetProperties(null);

	public override PropertyDescriptorCollection GetProperties(Attribute[]? Attributes) => WrapProperties(_entity, base.GetProperties(Attributes), null);

	public override object GetPropertyOwner(PropertyDescriptor? Property) => _entity;

	public void AcceptChanges() {
		_edits.Clear();
		_collectionEdits.Clear();
	}

	public void UndoChanges() {
		// Restore actual owners in reverse edit order, preserving shared references and cycles.
		foreach (var Edit in _edits.AsEnumerable().Reverse())
			if (!Equals(Edit.Property.GetValue(null), Edit.OriginalValue))
				Edit.Property.RestoreValue(Edit.OriginalValue);
		foreach (var Edit in _collectionEdits) {
			if (Edit.Items.SequenceEqual(Edit.Collection.Cast<object?>()))
				continue;
			if (Edit.Collection.IsFixedSize) {
				for (var Index = 0; Index < Edit.Items.Length; Index++)
					Edit.Collection[Index] = Edit.Items[Index];
			} else {
				Edit.Collection.Clear();
				foreach (var Item in Edit.Items)
					Edit.Collection.Add(Item);
			}
		}
		AcceptChanges();
	}

	private PropertyDescriptorCollection WrapProperties(object Owner, PropertyDescriptorCollection Properties, EditablePropertyDescriptor? Parent) {
		var visible = Properties.Cast<PropertyDescriptor>().Where(property => (!ReferenceEquals(Owner, _entity) || !_hiddenProperties.Contains(property.Name))
			&& (_propertyFilter == null || _propertyFilter(Owner, property)));
		return new PropertyDescriptorCollection(visible.Select(property => new EditablePropertyDescriptor(this, Owner, property, Parent)).ToArray(), true);
	}

	private void RecordEdit(EditablePropertyDescriptor Property) {
		if (!_edits.Any(Edit => ReferenceEquals(Edit.Property.Owner, Property.Owner) && Edit.Property.Name == Property.Name))
			_edits.Add(new PropertyEdit(Property, Property.GetValue(null)));
	}

	private void CaptureEditorValues(EditablePropertyDescriptor Property, HashSet<object> Visited) {
		if (Property.CanRestore)
			RecordEdit(Property);
		var Value = Property.GetValue(null);
		if (Value == null || Value is string || Value.GetType().IsValueType || !Visited.Add(Value))
			return;
		if (Value is IList Collection && !Collection.IsReadOnly) {
			if (!_collectionEdits.Any(Edit => ReferenceEquals(Edit.Collection, Collection)))
				_collectionEdits.Add(new CollectionEdit(Property, Collection, Collection.Cast<object?>().ToArray()));
			return;
		}
		if (Value is IEnumerable)
			return;
		foreach (EditablePropertyDescriptor Child in WrapProperties(Value, TypeDescriptor.GetProperties(Value), Property))
			CaptureEditorValues(Child, Visited);
	}

	private sealed record PropertyEdit(EditablePropertyDescriptor Property, object? OriginalValue);

	private sealed record CollectionEdit(EditablePropertyDescriptor Property, IList Collection, object?[] Items);

	private sealed class EditablePropertyDescriptor : PropertyDescriptor {
		private readonly CrudEntityTypeDescriptor _session;
		private readonly PropertyDescriptor _property;
		private readonly EditablePropertyDescriptor? _parent;
		private readonly TypeConverter _converter;
		private readonly CrudReferenceBinding? _referenceBinding;

		public EditablePropertyDescriptor(CrudEntityTypeDescriptor Session, object Owner, PropertyDescriptor Property, EditablePropertyDescriptor? Parent)
			: base(Property) {
			_session = Session;
			this.Owner = Owner;
			_property = Property;
			_parent = Parent;
			Session._referenceBindings.TryGetValue(Path, out _referenceBinding);
			_converter = _referenceBinding != null ? new CrudReferenceConverter(_referenceBinding) : CanExpandAutomatically || Property.Converter.GetPropertiesSupported()
				? new NestedPropertyConverter(this, Property.Converter)
				: Property.Converter;
		}

		public object Owner { get; }

		public string Path => _parent == null ? Name : $"{_parent.Path}.{Name}";

		public bool IsActive => _parent == null || _parent.IsActive && ReferenceEquals(_parent.GetValue(null), Owner);

		public bool IsCircular {
			get {
				var Value = GetValue(null);
				for (var Ancestor = this; Ancestor != null; Ancestor = Ancestor._parent)
					if (ReferenceEquals(Value, Ancestor.Owner))
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

		public override object? GetValue(object? Component) => _property.GetValue(Owner);

		public override void SetValue(object? Component, object? Value) {
			Guard.Ensure(!IsReadOnly, $"The property '{Name}' is read-only.");
			if (CanCreate && Value is string)
				Value = Converter.ConvertFrom(Value);
			_session.RecordEdit(this);
			_property.SetValue(Owner, Value);
		}

		public override void ResetValue(object Component) {
			Guard.Ensure(!IsReadOnly, $"The property '{Name}' is read-only.");
			_session.RecordEdit(this);
			_property.ResetValue(Owner);
		}

		public override bool CanResetValue(object Component) => !IsReadOnly && _property.CanResetValue(Owner);

		public override bool ShouldSerializeValue(object Component) => _property.ShouldSerializeValue(Owner);

		public override object? GetEditor(Type EditorBaseType) {
			if (_session._readOnly)
				return null;
			if (_referenceBinding != null && EditorBaseType == typeof(UITypeEditor))
				return IsReadOnly ? null : new CrudReferenceEditor(_referenceBinding);
			var Editor = _property.GetEditor(EditorBaseType);
			return Editor is UITypeEditor ValueEditor ? new RecordingValueEditor(this, ValueEditor) : Editor;
		}

		public void CaptureEditorValues() => _session.CaptureEditorValues(this, new HashSet<object>(ReferenceEqualityComparer.Instance));

		public override void AddValueChanged(object Component, EventHandler Handler) => _property.AddValueChanged(Owner, Handler);

		public override void RemoveValueChanged(object Component, EventHandler Handler) => _property.RemoveValueChanged(Owner, Handler);

		public void RestoreValue(object? Value) => _property.SetValue(Owner, Value);

		public PropertyDescriptorCollection GetChildren(object Value, Attribute[]? Attributes, ITypeDescriptorContext? Context) {
			// Declared dictionary roles are fixed navigation keys, with editable nested values instead of an opaque collection dialog.
			var Properties = Value is IDictionary dictionary
				? new PropertyDescriptorCollection(dictionary.Keys.Cast<object>().Select(key => new DictionaryValueProperty(dictionary, key)).ToArray(), true)
				: Value is IList list
					? new PropertyDescriptorCollection(Enumerable.Range(0, list.Count).Select(index => new ListValueProperty(list, index)).ToArray(), true)
					: CanExpandAutomatically ? TypeDescriptor.GetProperties(Value, Attributes) : _property.Converter.GetProperties(Context, Value, Attributes);
			// Let WinForms manage boxed values and immutable converters through their owning property setter.
			return Value.GetType().IsValueType && !_session._readOnly ? Properties ?? new PropertyDescriptorCollection(null)
				: _session.WrapProperties(Value, Properties ?? new PropertyDescriptorCollection(null), this);
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

		public RecordingValueEditor(EditablePropertyDescriptor Property, UITypeEditor Editor) {
			_property = Property;
			_editor = Editor;
		}

		public override bool IsDropDownResizable => _editor.IsDropDownResizable;

		public override object? EditValue(ITypeDescriptorContext? Context, IServiceProvider Provider, object? Value) {
			_property.CaptureEditorValues();
			return _editor.EditValue(Context, Provider, Value);
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? Context) => _editor.GetEditStyle(Context);

		public override bool GetPaintValueSupported(ITypeDescriptorContext? Context) => _editor.GetPaintValueSupported(Context);

		public override void PaintValue(PaintValueEventArgs Args) => _editor.PaintValue(Args);
	}

	private sealed class NestedPropertyConverter : TypeConverter {
		private const string CreateValue = "(Create new)";
		private const string EmptyValue = "(none)";
		private readonly EditablePropertyDescriptor _property;
		private readonly TypeConverter _converter;

		public NestedPropertyConverter(EditablePropertyDescriptor Property, TypeConverter Converter) {
			_property = Property;
			_converter = Converter;
		}

		public override bool CanConvertFrom(ITypeDescriptorContext? Context, Type SourceType) =>
			_property.CanCreate && SourceType == typeof(string) || _converter.CanConvertFrom(Context, SourceType);

		public override bool CanConvertTo(ITypeDescriptorContext? Context, Type? DestinationType) => _converter.CanConvertTo(Context, DestinationType);

		public override object? ConvertFrom(ITypeDescriptorContext? Context, CultureInfo? Culture, object Value) {
			if (_property.CanCreate && Value is string Text) {
				if (Text == CreateValue)
					return Tools.Object.Create(_property.PropertyType);
				if (Text == EmptyValue)
					return null;

				// PropertyGrid paints standard choices by round-tripping their displayed captions.
				if (_property.GetValue(null) is { } Current && Text == ConvertToString(Context, Culture, Current))
					return Current;
			}
			return _converter.ConvertFrom(Context, Culture, Value);
		}

		public override object? ConvertTo(ITypeDescriptorContext? Context, CultureInfo? Culture, object? Value, Type DestinationType) {
			if (DestinationType == typeof(string)) {
				if (Value is IDictionary dictionary)
					return dictionary.Count == 1 ? "1 entry" : $"{dictionary.Count} entries";
				if (Value is ICollection collection)
					return collection.Count == 1 ? "1 item" : $"{collection.Count} items";
			}
			if (_property.CanExpandAutomatically && DestinationType == typeof(string)) {
				if (Value == null)
					return EmptyValue;
				if (Value is string)
					return Value;
				var Summary = Value.ToString();
				var valueType = Value.GetType();
				// Preserve entity captions and meaningful ToString overrides; marked options use an expansion prompt.
				if (Summary == valueType.FullName || string.IsNullOrWhiteSpace(Summary))
					Summary = valueType.IsDefined(typeof(UserEditableAttribute), true) ? "Expand to edit" : valueType.Name;
				return _property.IsCircular ? $"{Summary} (already shown)" : Summary;
			}
			return _converter.ConvertTo(Context, Culture, Value, DestinationType);
		}

		public override bool GetPropertiesSupported(ITypeDescriptorContext? Context) => !_property.IsCircular
			&& (_property.CanExpandAutomatically || _converter.GetPropertiesSupported(Context));

		public override PropertyDescriptorCollection? GetProperties(ITypeDescriptorContext? Context, object Value, Attribute[]? Attributes) =>
			_property.IsCircular ? new PropertyDescriptorCollection(null) : _property.GetChildren(Value, Attributes, Context);

		public override bool GetStandardValuesSupported(ITypeDescriptorContext? Context) => _property.CanCreate || _converter.GetStandardValuesSupported(Context);

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext? Context) => _property.CanCreate || _converter.GetStandardValuesExclusive(Context);

		public override StandardValuesCollection? GetStandardValues(ITypeDescriptorContext? Context) =>
			_property.CanCreate ? new StandardValuesCollection(_property.GetValue(null) is { } Current
				? new object?[] { Current, null, CreateValue } : new object?[] { null, CreateValue }) : _converter.GetStandardValues(Context);

		public override bool GetCreateInstanceSupported(ITypeDescriptorContext? Context) => _converter.GetCreateInstanceSupported(Context);

		public override object? CreateInstance(ITypeDescriptorContext? Context, IDictionary PropertyValues) => _converter.CreateInstance(Context, PropertyValues);

		public override bool IsValid(ITypeDescriptorContext? Context, object? Value) => _converter.IsValid(Context, Value);
	}
}
