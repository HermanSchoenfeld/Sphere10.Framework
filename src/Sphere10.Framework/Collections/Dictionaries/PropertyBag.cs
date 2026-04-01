using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Sphere10.Framework.Collections;

/// <summary>
/// A string-keyed property bag that serializes to XML with each key as an element
/// and each value as the element's text content, preserving the value type via a <c>type</c> attribute.
/// <example>
/// <code><![CDATA[
/// <PropertyBag>
///   <Name type="System.String">Alice</Name>
///   <Age type="System.Int32">30</Age>
///   <Active type="System.Boolean">True</Active>
/// </PropertyBag>
/// ]]></code>
/// </example>
/// </summary>
public class PropertyBag : IXmlSerializable {
	internal readonly IDictionary<string, object> _properties = new Dictionary<string, object>();

	public void Set<T>(string name, T value) {
		_properties[name] = value;
	}

	public T Get<T>(string name) {
		if (_properties.TryGetValue(name, out var Value)) {
			if (Value is T TypedValue)
				return TypedValue;
			throw new InvalidCastException($"Property '{name}' is not of type {typeof(T).FullName}.");
		}
		throw new KeyNotFoundException($"Property '{name}' not found.");
	}

	public bool TryGet<T>(string name, out T value) {
		if (_properties.TryGetValue(name, out var Raw) && Raw is T TypedValue) {
			value = TypedValue;
			return true;
		}
		value = default;
		return false;
	}

	public bool Contains(string name) => _properties.ContainsKey(name);

	public void Remove(string name) {
		_properties.Remove(name);
	}

	public IEnumerable<string> Keys => _properties.Keys;

	public IEnumerable<object> Values => _properties.Values;

	#region IXmlSerializable

	XmlSchema IXmlSerializable.GetSchema() => null;

	void IXmlSerializable.ReadXml(XmlReader reader) {
		if (reader.IsEmptyElement) {
			reader.Read();
			return;
		}
		reader.ReadStartElement();
		while (reader.NodeType != XmlNodeType.EndElement) {
			if (reader.NodeType != XmlNodeType.Element) {
				reader.Read();
				continue;
			}
			var Name = reader.LocalName;
			var TypeName = reader.GetAttribute("type");
			var TextValue = reader.ReadElementContentAsString();
			if (TypeName != null) {
				var Type = System.Type.GetType(TypeName);
				if (Type != null) {
					var Converter = TypeDescriptor.GetConverter(Type);
					_properties[Name] = Converter.CanConvertFrom(typeof(string))
						? Converter.ConvertFromInvariantString(TextValue)
						: TextValue;
				} else {
					_properties[Name] = TextValue;
				}
			} else {
				_properties[Name] = TextValue;
			}
		}
		reader.ReadEndElement();
	}

	void IXmlSerializable.WriteXml(XmlWriter writer) {
		foreach (var Kvp in _properties) {
			writer.WriteStartElement(Kvp.Key);
			if (Kvp.Value != null) {
				writer.WriteAttributeString("type", Kvp.Value.GetType().FullName);
				var Converter = TypeDescriptor.GetConverter(Kvp.Value.GetType());
				writer.WriteValue(Converter.ConvertToInvariantString(Kvp.Value));
			}
			writer.WriteEndElement();
		}
	}

	#endregion

}

