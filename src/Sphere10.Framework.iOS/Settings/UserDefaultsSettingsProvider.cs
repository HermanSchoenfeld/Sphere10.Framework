// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

#if __IOS__
#define INCLUDE_FILE
#endif

#if MONOMAC
#define INCLUDE_FILE
#endif

#if INCLUDE_FILE

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Foundation;
using Sphere10.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.iOS;

/// <summary>Persists typed settings in the platform user-defaults store.</summary>
public class UserDefaultsSettingsProvider : SyncSettingsProviderBase {
	private const string _nullValueString = "NULL!8be93b18-cecb-4be0-8d05-d6c43123aa4e";

	/// <inheritdoc />
	public override bool ContainsSetting(Type settingsObjectType, object id = null) {
		Preconditions.CheckNotNull(settingsObjectType, "settingsObjectType");
		var key = new NSString(PropertyNameToKey(settingsObjectType, id, "ID"));
		var result = NSUserDefaults.StandardUserDefaults.ToDictionary().ContainsKey(key);
		return result;
	}

	// Hydrate the fields and properties that participate in platform settings storage.
	protected override SettingsObject LoadInternal(Type settingsObjectType, object id = null) {
		// Create the typed settings instance before filling its persisted members.
		var setting = Tools.Object.Create(settingsObjectType) as SettingsObject;
		setting.ID = id;
		foreach (var field in GetSettingsFields(settingsObjectType))
			field.SetValue(setting, LoadPropertyValue(settingsObjectType, id, field.Name, field.FieldType));

		// Hydrate settings properties after their public fields.
		foreach (var property in GetSettingsProperties(settingsObjectType))
			property.SetValue(setting, LoadPropertyValue(settingsObjectType, id, property.Name, property.PropertyType));
		return setting;
	}

	// Persist each supported settings member and synchronize the platform store.
	protected override void SaveInternal(SettingsObject settings) {
		var settingType = settings.GetType();
		foreach (var field in GetSettingsFields(settings.GetType()))
			SavePropertyValue(settingType, settings.ID, field.Name, field.GetValue(settings));

		// Persist settings properties after their public fields.
		foreach (var property in GetSettingsProperties(settings.GetType()))
			SavePropertyValue(settingType, settings.ID, property.Name, property.GetValue(settings, null));

		// Flush the complete set of member writes to the platform store.
		NSUserDefaults.StandardUserDefaults.Synchronize();
	}

	/// <inheritdoc />
	public override void DeleteSetting(SettingsObject settings) {
		var settingType = settings.GetType();
		foreach (var field in GetSettingsFields(settingType))
			DeletePropertyValue(settingType, settings.ID, field.Name);

		// Persist settings properties after their public fields.
		foreach (var property in GetSettingsProperties(settings.GetType()))
			DeletePropertyValue(settingType, settings.ID, property.Name);
	}

	/// <inheritdoc />
	public override void ClearSettings() {
		NSUserDefaults.ResetStandardUserDefaults();
		NSUserDefaults.StandardUserDefaults.Synchronize();
	}

	/// <summary>Converts a stored value to the requested platform-settings type.</summary>
	public T ConvertExChangeType<T>(object value) {
		return (T)Convert.ChangeType(value, typeof(T));
	}

	// Include the settings type and optional instance identity in each platform key.
	private string PropertyNameToKey(Type settingObjectType, object settingId, string propertyName) {
		var result = string.Format(
			"{0}.{1}",
			string.Format("{0}{1}", settingObjectType.FullName, settingId != null ? "." + ConvertExChangeType<string>(settingId) : string.Empty),
			propertyName
		);
		return result;
	}

	// Read the platform value and convert it back to the declared member type.
	private object LoadPropertyValue(Type settingObjectType, object settingId, string propertyName, Type expectedValueType) {
		var key = PropertyNameToKey(settingObjectType, settingId, propertyName);
		object storedValue = null;

		// Check if stored value is null
		var nullCheckValue = NSUserDefaults.StandardUserDefaults.ValueForKey(new NSString(key));
		if (nullCheckValue == null || nullCheckValue.ToString() == _nullValueString)
			return null;

		// Match the stored representation to the member declaration.
		switch (GetStorageClass(expectedValueType)) {
			case StorageClass.String:
				storedValue = NSUserDefaults.StandardUserDefaults.StringForKey(key);
				break;
			case StorageClass.Integer:
				storedValue = (int)(NSUserDefaults.StandardUserDefaults.IntForKey(key));
				break;
			case StorageClass.Bool:
				storedValue = NSUserDefaults.StandardUserDefaults.BoolForKey(key);
				break;
			case StorageClass.Float:
				storedValue = NSUserDefaults.StandardUserDefaults.FloatForKey(key);
				break;
			case StorageClass.Double:
				storedValue = NSUserDefaults.StandardUserDefaults.DoubleForKey(key);
				break;
			case StorageClass.DateTime:
				storedValue = Tools.Parser.Parse<DateTime?>(NSUserDefaults.StandardUserDefaults.StringForKey(key));
				break;
			case StorageClass.Enum:
				storedValue = Enum.Parse(expectedValueType, NSUserDefaults.StandardUserDefaults.StringForKey(key));
				break;
			case StorageClass.XMLSerialized:
				storedValue = XmlProvider.ReadFromString(expectedValueType, NSUserDefaults.StandardUserDefaults.StringForKey(key));
				break;
			default:
				throw new Exception("Internal Error F1C3E9A8-BAD2-48AF-9533-E78DE234B353");
		}

		// Preserve the existing conversion into the requested member type.
		return Tools.Object.ChangeType(storedValue, expectedValueType);
	}

	// Store a native value or the existing null sentinel under the member key.
	private void SavePropertyValue(Type settingObjectType, object settingId, string propertyName, object value) {
		var key = PropertyNameToKey(settingObjectType, settingId, propertyName);
		if (value != null) {
			switch (GetStorageClass(value.GetType())) {
				case StorageClass.String:
					NSUserDefaults.StandardUserDefaults.SetString(ConvertExChangeType<string>(value), PropertyNameToKey(settingObjectType, settingId, propertyName));
					break;
				case StorageClass.Integer:
					NSUserDefaults.StandardUserDefaults.SetInt(ConvertExChangeType<int>(value), PropertyNameToKey(settingObjectType, settingId, propertyName));
					break;
				case StorageClass.Bool:
					NSUserDefaults.StandardUserDefaults.SetBool(ConvertExChangeType<bool>(value), PropertyNameToKey(settingObjectType, settingId, propertyName));
					break;
				case StorageClass.Float:
					NSUserDefaults.StandardUserDefaults.SetFloat(ConvertExChangeType<float>(value), PropertyNameToKey(settingObjectType, settingId, propertyName));
					break;
				case StorageClass.Double:
					NSUserDefaults.StandardUserDefaults.SetDouble(ConvertExChangeType<double>(value), PropertyNameToKey(settingObjectType, settingId, propertyName));
					break;
				case StorageClass.DateTime:
					NSUserDefaults.StandardUserDefaults.SetString(
						string.Format("{0:yyyy-MM-dd HH:mm:ss.fff}", ConvertExChangeType<DateTime>(value)),
						PropertyNameToKey(settingObjectType, settingId, propertyName)
					);
					break;
				case StorageClass.Enum:
					NSUserDefaults.StandardUserDefaults.SetString(value.ToString(), PropertyNameToKey(settingObjectType, settingId, propertyName));
					break;
				case StorageClass.XMLSerialized:
					NSUserDefaults.StandardUserDefaults.SetString(XmlProvider.WriteToString(key), PropertyNameToKey(settingObjectType, settingId, propertyName));
					break;
				default:
					throw new Exception("Internal Error 01B66BCD-FDEB-4FCF-B32F-44B2C8A2A81C");
			}
		} else {
			NSUserDefaults.StandardUserDefaults.SetString(_nullValueString, PropertyNameToKey(settingObjectType, settingId, propertyName));
		}
	}

	// Remove the value stored for one settings member.
	private void DeletePropertyValue(Type settingObjectType, object settingId, string propertyName) {
		NSUserDefaults.StandardUserDefaults.RemoveObject(PropertyNameToKey(settingObjectType, settingId, propertyName));
	}

	// Choose the existing native storage representation for a declared member type.
	private StorageClass GetStorageClass(Type type) {
		var result = StorageClass.XMLSerialized;
		TypeSwitch.DoType(type,
			TypeSwitch.Case<string>(() => result = StorageClass.String),
			TypeSwitch.Case<bool>(() => result = StorageClass.Bool),
			TypeSwitch.Case<bool?>(() => result = StorageClass.Bool),
			TypeSwitch.Case<byte>(() => result = StorageClass.Integer),
			TypeSwitch.Case<byte?>(() => result = StorageClass.Integer),
			TypeSwitch.Case<sbyte>(() => result = StorageClass.Integer),
			TypeSwitch.Case<sbyte?>(() => result = StorageClass.Integer),
			TypeSwitch.Case<short>(() => result = StorageClass.Integer),
			TypeSwitch.Case<short?>(() => result = StorageClass.Integer),
			TypeSwitch.Case<ushort>(() => result = StorageClass.Integer),
			TypeSwitch.Case<ushort?>(() => result = StorageClass.Integer),
			TypeSwitch.Case<int>(() => result = StorageClass.Integer),
			TypeSwitch.Case<int?>(() => result = StorageClass.Integer),
			TypeSwitch.Case<uint>(() => result = StorageClass.Integer),
			TypeSwitch.Case<uint?>(() => result = StorageClass.Integer),
			/*TypeSwitch.Case<long>((x) => {throw new NotSupportedException(string.Format("Settings of type {0} not supported", x.GetType().Name)); }),
			TypeSwitch.Case<long?>((x) => {throw new NotSupportedException(string.Format("Settings of type {0} not supported", x.GetType().Name)); }),
			TypeSwitch.Case<ulong>((x) => {throw new NotSupportedException(string.Format("Settings of type {0} not supported", x.GetType().Name)); }),
			TypeSwitch.Case<ulong?>((x) => {throw new NotSupportedException(string.Format("Settings of type {0} not supported", x.GetType().Name)); }),*/
			TypeSwitch.Case<float>(() => result = StorageClass.Float),
			TypeSwitch.Case<float?>(() => result = StorageClass.Float),
			TypeSwitch.Case<double>(() => result = StorageClass.Double),
			TypeSwitch.Case<double?>(() => result = StorageClass.Double),
			TypeSwitch.Case<decimal>(() => result = StorageClass.Double),
			TypeSwitch.Case<decimal?>(() => result = StorageClass.Double),
			TypeSwitch.Case<DateTime>(() => result = StorageClass.DateTime),
			TypeSwitch.Case<DateTime?>(() => result = StorageClass.DateTime),
			TypeSwitch.Case<Enum>(() => result = StorageClass.Enum),
			TypeSwitch.Default(() => result = StorageClass.XMLSerialized)
		);
		return result;
	}

	// Select public fields explicitly declared as persisted settings.
	private IEnumerable<FieldInfo> GetSettingsFields(Type settingsObjectType) {
		return settingsObjectType.GetFields(BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.SetField)
			.Where(field => Attribute.IsDefined(field, typeof(DefaultValueAttribute)));
	}

	// Select non-indexed properties explicitly declared as persisted settings.
	private IEnumerable<PropertyInfo> GetSettingsProperties(Type settingsObjectType) {
		return settingsObjectType.GetProperties(BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty)
			.Where(property => Attribute.IsDefined(property, typeof(DefaultValueAttribute)) && property.GetIndexParameters().Length == 0);
	}

	// Native representations used by the platform settings conversion helpers.
	private enum StorageClass {
		String,
		Integer,
		Float,
		Double,
		Bool,
		DateTime,
		Enum,
		XMLSerialized
	}
}

#endif

