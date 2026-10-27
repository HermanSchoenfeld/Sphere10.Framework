// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Security;

// ReSharper disable CheckNamespace
namespace Tools;

/// <summary>Windows scope factories. Import the Tools namespace to discover these static extension members.</summary>
public static class WindowsScopeExtensions {
	extension(Scope) {
		public static UnmanagedMemoryScope EnterUnmanagedMemoryScope(int size) => new(size);

		public static UnmanagedMemoryScope EnterUnmanagedUnicodeStringScope(string value) => UnmanagedMemoryScope.FromUnicodeString(value);

		public static AlwaysOnScope EnterAlwaysOnScope(bool system, bool display) => new(system, display);

		public static ThreadDpiAwarenessScope EnterThreadDpiAwarenessScope(DpiAwarenessContext context) => new(context);

		public static AttachToCommandPromptScope EnterAttachToCommandPromptScope() => new();

		public static TokenPrivilegeScope EnterTokenPrivilegeScope(params string[] privileges) => new(privileges);
	}
}
