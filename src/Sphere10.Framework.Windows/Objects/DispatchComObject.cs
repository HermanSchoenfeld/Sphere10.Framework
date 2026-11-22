// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Sphere10.Framework.Windows;

/// <summary>
/// Owns one automation (IDispatch) interface and calls its members by name. Explicit IDispatch calls support Native AOT, which has
/// neither runtime COM activation nor a dynamic binder. Derived wrappers expose the named members their callers use, and are
/// used and disposed on the apartment that created them.
/// </summary>
internal abstract class DispatchComObject : SyncDisposable {
	private IntPtr _dispatch;

	protected DispatchComObject(IntPtr dispatch) {
		Guard.Argument(dispatch != IntPtr.Zero, nameof(dispatch), "An automation interface is required.");
		_dispatch = dispatch;
	}

	/// <summary>The owned interface, borrowed by native calls made while this wrapper is alive.</summary>
	internal IntPtr Interface => _dispatch != IntPtr.Zero ? _dispatch : throw new ObjectDisposedException(GetType().Name);

	/// <summary>Reads a property or calls a method and returns its copied scalar value.</summary>
	protected object GetValue(string member, params object[] arguments) {
		var result = InvokeMember(member, arguments);
		using var release = Tools.Scope.ExecuteOnDispose(() => WindowsVariantMarshaller.Free(result));
		return WindowsVariantMarshaller.ConvertToManaged(result, _ => null);
	}

	/// <summary>Calls a member that returns another automation object and transfers its reference to a typed wrapper, or returns null when none is supplied.</summary>
	protected T GetObject<T>(string member, Func<IntPtr, T> wrap, params object[] arguments) where T : DispatchComObject {
		var result = InvokeMember(member, arguments);
		if (result.VarType == VarEnum.VT_DISPATCH && result.GetRawDataRef<IntPtr>() != IntPtr.Zero)
			return wrap(result.GetRawDataRef<IntPtr>());
		WindowsVariantMarshaller.Free(result);
		return null;
	}

	/// <summary>Calls a method whose result is not used.</summary>
	protected void Call(string member, params object[] arguments) => WindowsVariantMarshaller.Free(InvokeMember(member, arguments));

	/// <summary>Assigns a property.</summary>
	protected void SetValue(string member, object value) {
		var dispatch = Interface;
		Marshal.ThrowExceptionForHR(WinAPI.OLEAUT32.GetDispatchId(dispatch, member, out var id));
		var argument = ComVariantMarshaller.ConvertToUnmanaged(value);
		using var release = Tools.Scope.ExecuteOnDispose(() => WindowsVariantMarshaller.Free(argument));
		ThrowForFailure(member, WinAPI.OLEAUT32.InvokeDispatchPut(dispatch, id, argument, out var failure), failure);
	}

	/// <summary>What the object is called in a failure that carries no description of its own.</summary>
	protected virtual string Subject => "The automation object";

	protected override void DisposeInternal() {
		if (_dispatch != IntPtr.Zero)
			Marshal.Release(_dispatch);
		_dispatch = IntPtr.Zero;
	}

	private ComVariant InvokeMember(string member, object[] arguments) {
		var dispatch = Interface;
		Marshal.ThrowExceptionForHR(WinAPI.OLEAUT32.GetDispatchId(dispatch, member, out var id));
		var values = new ComVariant[arguments.Length];
		using var release = Tools.Scope.ExecuteOnDispose(() => {
			foreach (var value in values)
				WindowsVariantMarshaller.Free(value);
		});
		for (var index = 0; index < arguments.Length; index++)
			values[index] = ComVariantMarshaller.ConvertToUnmanaged(arguments[index]);
		var status = WinAPI.OLEAUT32.InvokeDispatch(dispatch, id, (ushort)(WinAPI.OLEAUT32.DISPATCH_METHOD | WinAPI.OLEAUT32.DISPATCH_PROPERTYGET), values, out var result, out var failure);
		ThrowForFailure(member, status, failure);
		return result;
	}

	// Frees the failure's strings whether or not the call failed, then reports the object's own description when it gave one.
	private void ThrowForFailure(string member, int status, WinAPI.OLEAUT32.EXCEPINFO failure) {
		var description = failure.Description == IntPtr.Zero ? null : Marshal.PtrToStringBSTR(failure.Description);
		foreach (var text in new[] { failure.Source, failure.Description, failure.HelpFile })
			if (text != IntPtr.Zero)
				Marshal.FreeBSTR(text);
		if (status == WinAPI.OLEAUT32.DISP_E_EXCEPTION)
			throw new COMException(description ?? $"{Subject} reported a failure in {member}.", failure.StatusCode != 0 ? failure.StatusCode : status);
		Marshal.ThrowExceptionForHR(status);
	}
}
