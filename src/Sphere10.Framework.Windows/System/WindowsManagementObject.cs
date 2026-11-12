// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Sphere10.Framework.Windows;

/// <summary>Wraps one native WMI COM object and its copied properties for Native AOT within a synchronous query.</summary>
public sealed class WindowsManagementObject : SyncDisposable {
	private readonly WindowsManagementQuery _query;
	private readonly Dictionary<string, object> _values = new(StringComparer.OrdinalIgnoreCase);

	internal WindowsManagementObject(IntPtr pointer, WindowsManagementQuery query) {
		Pointer = pointer;
		_query = query;
	}

	internal IntPtr Pointer { get; private set; }

	public string ClassName => Convert.ToString(this["__CLASS"], CultureInfo.InvariantCulture);

	public string Path => Convert.ToString(this["__PATH"], CultureInfo.InvariantCulture);

	public object this[string name] {
		get {
			if (_values.TryGetValue(name, out var stored))
				return stored;
			var status = WinAPI.WBEMCLI.Get(Pointer, name, out var value, out var cimType);
			try {
				WindowsManagementQuery.Check(status);
				stored = ConvertCimValue(WindowsVariantMarshaller.ConvertToManaged(value, ReadEmbeddedObject), cimType);
				_values.Add(name, stored);
				return stored;
			} finally {
				WindowsVariantMarshaller.Free(value);
			}
		}
		set {
			var native = Unsafe.BitCast<WinAPI.OLEAUT32.VARIANT, ComVariant>(WindowsVariantMarshaller.ConvertToUnmanaged(value));
			try {
				WindowsManagementQuery.Check(WinAPI.WBEMCLI.Put(Pointer, name, in native));
				if (_values.Remove(name, out var previous))
					ReleaseValue(previous);
			} finally {
				WindowsVariantMarshaller.Free(native);
			}
		}
	}

	public IEnumerable<KeyValuePair<string, object>> Properties {
		get {
			WindowsManagementQuery.Check(WinAPI.WBEMCLI.GetNames(Pointer, out var names));
			using var freeNames = Tools.Scope.ExecuteOnDispose(() => WinAPI.OLEAUT32.SafeArrayDestroy(names));
			foreach (var name in (string[])WindowsVariantMarshaller.ReadArray(names, ReadEmbeddedObject))
				yield return new KeyValuePair<string, object>(name, this[name]);
		}
	}

	public IEnumerable<WindowsManagementObject> GetRelated(string resultClass, string associationClass)
		=> _query.Read("ASSOCIATORS OF {" + Convert.ToString(this["__RELPATH"], CultureInfo.InvariantCulture) + "} WHERE AssocClass=" + associationClass + " ResultClass=" + resultClass);

	public WindowsManagementObject GetMethodParameters(string name) {
		using var definition = _query.GetObject(ClassName);
		var status = WinAPI.WBEMCLI.GetMethod(definition.Pointer, name, out var input, out var output);
		using var releaseInput = Tools.Scope.ExecuteOnDispose(() => { if (input != IntPtr.Zero) Marshal.Release(input); });
		using var releaseOutput = Tools.Scope.ExecuteOnDispose(() => { if (output != IntPtr.Zero) Marshal.Release(output); });
		WindowsManagementQuery.Check(status);
		if (input == IntPtr.Zero)
			return null;
		WindowsManagementQuery.Check(WinAPI.WBEMCLI.SpawnInstance(input, out var instance));
		return new WindowsManagementObject(instance, _query);
	}

	public WindowsManagementObject InvokeMethod(string name, WindowsManagementObject input, TimeSpan timeout)
		=> _query.Invoke(Path, name, input, timeout);

	protected override void DisposeInternal() {
		foreach (var value in _values.Values)
			ReleaseValue(value);
		_values.Clear();
		if (Pointer != IntPtr.Zero) {
			Marshal.Release(Pointer);
			Pointer = IntPtr.Zero;
		}
	}

	private object ReadEmbeddedObject(IntPtr pointer) {
		var item = Tools.Windows.COM.QueryManagementObject(pointer);
		return new WindowsManagementObject(item, _query);
	}

	private static object ConvertCimValue(object value, int cimType) {
		if (value == null)
			return null;
		if ((cimType & 0x2000) != 0) {
			var items = ((Array)value).Cast<object>();
			return (cimType & ~0x2000) switch {
				17 => items.Select(Convert.ToByte).ToArray(),
				18 => items.Select(item => unchecked((ushort)Convert.ToInt32(item, CultureInfo.InvariantCulture))).ToArray(),
				19 => items.Select(item => unchecked((uint)Convert.ToInt32(item, CultureInfo.InvariantCulture))).ToArray(),
				20 => items.Select(item => Convert.ToInt64(item, CultureInfo.InvariantCulture)).ToArray(),
				21 => items.Select(item => Convert.ToUInt64(item, CultureInfo.InvariantCulture)).ToArray(),
				13 => items.Cast<WindowsManagementObject>().ToArray(),
				_ => value
			};
		}
		return cimType switch {
			18 => unchecked((ushort)Convert.ToInt32(value, CultureInfo.InvariantCulture)),
			19 => unchecked((uint)Convert.ToInt32(value, CultureInfo.InvariantCulture)),
			20 => Convert.ToInt64(value, CultureInfo.InvariantCulture),
			21 => Convert.ToUInt64(value, CultureInfo.InvariantCulture),
			_ => value
		};
	}

	private static void ReleaseValue(object value) {
		if (value is WindowsManagementObject item)
			item.Dispose();
		else if (value is WindowsManagementObject[] items)
			foreach (var child in items)
				child?.Dispose();
	}
}
