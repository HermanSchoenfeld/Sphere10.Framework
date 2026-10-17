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

	[ThreadStatic] private static FileUtil _files;
	public static FileUtil Files => _files ??= new();

	[ThreadStatic] private static ComputerUtil _computer;
	public static ComputerUtil Computer => _computer ??= new();

	[ThreadStatic] private static PerformanceCounterUtil _performanceCounters;
	public static PerformanceCounterUtil PerformanceCounters => _performanceCounters ??= new();

	[ThreadStatic] private static RegistryUtil _registry;
	public static RegistryUtil Registry => _registry ??= new();

	[ThreadStatic] private static ServiceUtil _services;
	public static ServiceUtil Services => _services ??= new();

	[ThreadStatic] private static PrivilegeUtil _security;
	public static PrivilegeUtil Security => _security ??= new();

	[ThreadStatic] private static ProcessSignaler _processes;
	public static ProcessSignaler Processes => _processes ??= new();

	[ThreadStatic] private static ThreadUtil _threads;
	public static ThreadUtil Threads => _threads ??= new();

	[ThreadStatic] private static Win32Util _win32;
	public static Win32Util Win32 => _win32 ??= new();

	[ThreadStatic] private static ShellUtility _shell;
	public static ShellUtility Shell => _shell ??= new();

	[ThreadStatic] private static WindowsNetworkUtil _network;
	public static WindowsNetworkUtil Network => _network ??= new();

	[ThreadStatic] private static WindowsDriveUtil _drives;
	public static WindowsDriveUtil Drives => _drives ??= new();

	[ThreadStatic] private static WindowsStartupUtil _startup;
	public static WindowsStartupUtil Startup => _startup ??= new();

	[ThreadStatic] private static WindowsEventLogUtil _eventLogs;
	public static WindowsEventLogUtil EventLogs => _eventLogs ??= new();

	[ThreadStatic] private static EnvironmentUtil _environment;
	public static EnvironmentUtil Environment => _environment ??= new();

	[ThreadStatic] private static SecurityDescriptorUtil _securityDescriptors;
	public static SecurityDescriptorUtil SecurityDescriptors => _securityDescriptors ??= new();

}

