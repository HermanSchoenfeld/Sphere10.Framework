// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Files;
using Sphere10.Framework.Windows.EventLogs;
using Sphere10.Framework.Windows.Networking;
using Sphere10.Framework.Windows.Security;
using Sphere10.Framework.Windows.Startup;
using Sphere10.Framework.Windows.Storage;

namespace Tools;

/// <summary>
/// Static facade providing access to Windows utility instances. Each property returns a
/// thread-static singleton of the corresponding utility class from <see cref="Sphere10.Framework.Windows"/>.
/// </summary>
public static class Windows {

	[field: ThreadStatic]
	public static FileUtil Files => field ??= new();

	[field: ThreadStatic]
	public static ComputerUtil Computer => field ??= new();

	[field: ThreadStatic]
	public static WindowsDisplayUtil Display => field ??= new();

	[field: ThreadStatic]
	public static PerformanceCounterUtil PerformanceCounters => field ??= new();

	[field: ThreadStatic]
	public static RegistryUtil Registry => field ??= new();

	[field: ThreadStatic]
	public static ServiceUtil Services => field ??= new();

	[field: ThreadStatic]
	public static PrivilegeUtil Security => field ??= new();

	[field: ThreadStatic]
	public static ProcessSignaler Processes => field ??= new();

	[field: ThreadStatic]
	public static ThreadUtil Threads => field ??= new();

	[field: ThreadStatic]
	public static Win32Util Win32 => field ??= new();

	[field: ThreadStatic]
	public static ShellUtility Shell => field ??= new();

	[field: ThreadStatic]
	public static WindowsNetworkUtil Network => field ??= new();

	[field: ThreadStatic]
	public static WindowsDriveUtil Drives => field ??= new();

	[field: ThreadStatic]
	public static WindowsStartupUtil Startup => field ??= new();

	[field: ThreadStatic]
	public static WindowsEventLogUtil EventLogs => field ??= new();

	[field: ThreadStatic]
	public static EnvironmentUtil Environment => field ??= new();

	[field: ThreadStatic]
	public static SecurityDescriptorUtil SecurityDescriptors => field ??= new();

	[field: ThreadStatic]
	public static NamedPipeUtil Pipes => field ??= new();

	[field: ThreadStatic]
	public static EtwTraceUtil TraceSessions => field ??= new();

	[field: ThreadStatic]
	public static SessionUtil Sessions => field ??= new();

}

