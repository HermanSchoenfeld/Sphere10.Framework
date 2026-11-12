// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Windows.Security;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>Reusable Windows computer identity, firmware and installed hardware queries.</summary>
	public static class Computer {
		public static int LogicalProcessorCount => WindowsSystemInformation.LogicalProcessorCount;

		public static WindowsSystemCounters GetSystemCounters() => WindowsSystemInformation.GetCounters();

		public static Task<WindowsSystemCounters> GetSystemCountersAsync(CancellationToken cancellationToken = default) => Task.Run(GetSystemCounters, cancellationToken);

		/// <summary>Creates a fresh lazy power-plan inventory without starting a poller.</summary>
		public static WindowsPowerPlanDataSource CreatePowerPlanDataSource() => new WindowsPowerPlanDataSource();

		public static WindowsPowerSettingDataSource CreatePowerSettingDataSource(Guid planId) => new WindowsPowerSettingDataSource(planId);

		public static WindowsPowerDiagnosticDataSource CreatePowerDiagnosticDataSource() => new WindowsPowerDiagnosticDataSource();

		/// <summary>Creates a caller-owned executable wrapper. No powercfg process or report is started until requested.</summary>
		public static PowerCfgSentry CreatePowerCfgSentry() => new PowerCfgSentry();

		public static WindowsComputerInfo Query() => WindowsComputer.Query();

		public static Task<WindowsComputerInfo> QueryAsync(CancellationToken cancellationToken = default) => WindowsComputer.QueryAsync(cancellationToken);

		public static WindowsSystemProductInfo[] GetSystemProducts() => WindowsComputer.GetSystemProducts();

		public static Task<WindowsSystemProductInfo[]> GetSystemProductsAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetSystemProductsAsync(cancellationToken);

		public static WindowsBiosInfo[] GetBios() => WindowsComputer.GetBios();

		public static Task<WindowsBiosInfo[]> GetBiosAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetBiosAsync(cancellationToken);

		public static WindowsBaseboardInfo[] GetBaseboards() => WindowsComputer.GetBaseboards();

		public static Task<WindowsBaseboardInfo[]> GetBaseboardsAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetBaseboardsAsync(cancellationToken);

		public static WindowsProcessorInfo[] GetProcessors() => WindowsComputer.GetProcessors();

		public static Task<WindowsProcessorInfo[]> GetProcessorsAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetProcessorsAsync(cancellationToken);

		public static WindowsOperatingSystemInfo[] GetOperatingSystems() => WindowsComputer.GetOperatingSystems();

		public static Task<WindowsOperatingSystemInfo[]> GetOperatingSystemsAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetOperatingSystemsAsync(cancellationToken);

		public static WindowsMemoryModuleInfo[] GetMemoryModules() => WindowsComputer.GetMemoryModules();

		public static Task<WindowsMemoryModuleInfo[]> GetMemoryModulesAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetMemoryModulesAsync(cancellationToken);

		public static WindowsMemoryArrayInfo[] GetMemoryArrays() => WindowsComputer.GetMemoryArrays();

		public static Task<WindowsMemoryArrayInfo[]> GetMemoryArraysAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetMemoryArraysAsync(cancellationToken);

		public static WindowsEnclosureInfo[] GetEnclosures() => WindowsComputer.GetEnclosures();

		public static Task<WindowsEnclosureInfo[]> GetEnclosuresAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetEnclosuresAsync(cancellationToken);

		public static WindowsVideoControllerInfo[] GetVideoControllers() => WindowsComputer.GetVideoControllers();

		public static Task<WindowsVideoControllerInfo[]> GetVideoControllersAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetVideoControllersAsync(cancellationToken);

		public static WindowsPlugAndPlayDeviceInfo[] GetDevices() => WindowsComputer.GetDevices();

		public static Task<WindowsPlugAndPlayDeviceInfo[]> GetDevicesAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetDevicesAsync(cancellationToken);

		public static WindowsMonitorInfo[] GetMonitors() => WindowsComputer.GetMonitors();

		public static Task<WindowsMonitorInfo[]> GetMonitorsAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetMonitorsAsync(cancellationToken);

		public static WindowsPrinterInfo[] GetPrinters() => WindowsComputer.GetPrinters();

		public static Task<WindowsPrinterInfo[]> GetPrintersAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetPrintersAsync(cancellationToken);

		public static void Rename(string expectedCurrentName, string newName) => WindowsComputer.Rename(expectedCurrentName, newName);

		public static Task RenameAsync(string expectedCurrentName, string newName, CancellationToken cancellationToken = default) => WindowsComputer.RenameAsync(expectedCurrentName, newName, cancellationToken);

		public static WindowsPowerCapabilities GetPowerCapabilities() => new(WinAPI.POWRPROF.IsPwrSuspendAllowed(), WinAPI.POWRPROF.IsPwrHibernateAllowed(), WinAPI.POWRPROF.IsPwrShutdownAllowed());

		public static Task<WindowsPowerCapabilities> GetPowerCapabilitiesAsync(CancellationToken cancellationToken = default) => Task.Run(GetPowerCapabilities, cancellationToken);

		public static WindowsPowerPlanInfo[] GetPowerPlans(CancellationToken cancellationToken = default) => WindowsPowerInventory.GetPlans(cancellationToken);

		public static Task<WindowsPowerPlanInfo[]> GetPowerPlansAsync(CancellationToken cancellationToken = default) => Task.Run(() => GetPowerPlans(cancellationToken), cancellationToken);

		public static WindowsPowerSettingInfo[] GetPowerSettings(Guid planId, CancellationToken cancellationToken = default) => WindowsPowerInventory.GetSettings(planId, cancellationToken);

		public static Task<WindowsPowerSettingInfo[]> GetPowerSettingsAsync(Guid planId, CancellationToken cancellationToken = default) => Task.Run(() => GetPowerSettings(planId, cancellationToken), cancellationToken);

		public static WindowsPowerDiagnosticInfo[] GetPowerDiagnostics(CancellationToken cancellationToken = default) => WindowsPowerInventory.GetDiagnostics(cancellationToken);

		public static Task<WindowsPowerDiagnosticInfo[]> GetPowerDiagnosticsAsync(CancellationToken cancellationToken = default) => Task.Run(() => GetPowerDiagnostics(cancellationToken), cancellationToken);

		/// <summary>Requests power-off. Acceptance does not guarantee completion; the interactive user or another application may veto shutdown.</summary>
		public static void Shutdown(uint reason, bool forceIfHung = false) => RequestShutdown(8, reason, forceIfHung);

		public static Task ShutdownAsync(uint reason, bool forceIfHung = false, CancellationToken cancellationToken = default) => Task.Run(() => Shutdown(reason, forceIfHung), cancellationToken);

		public static void Restart(uint reason, bool forceIfHung = false) => RequestShutdown(2, reason, forceIfHung);

		public static Task RestartAsync(uint reason, bool forceIfHung = false, CancellationToken cancellationToken = default) => Task.Run(() => Restart(reason, forceIfHung), cancellationToken);

		public static void Logoff(bool forceIfHung = false) {
			if (!WinAPI.USER32.ExitWindowsEx(forceIfHung ? 0x10U : 0, 0))
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		public static Task LogoffAsync(bool forceIfHung = false, CancellationToken cancellationToken = default) => Task.Run(() => Logoff(forceIfHung), cancellationToken);

		public static void Lock() {
			if (!WinAPI.USER32.LockWorkStation())
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		public static Task LockAsync(CancellationToken cancellationToken = default) => Task.Run(Lock, cancellationToken);

		public static void Sleep(bool disableWakeEvents = false) => Suspend(false, disableWakeEvents);

		public static Task SleepAsync(bool disableWakeEvents = false, CancellationToken cancellationToken = default) => Task.Run(() => Sleep(disableWakeEvents), cancellationToken);

		public static void Hibernate(bool disableWakeEvents = false) => Suspend(true, disableWakeEvents);

		public static Task HibernateAsync(bool disableWakeEvents = false, CancellationToken cancellationToken = default) => Task.Run(() => Hibernate(disableWakeEvents), cancellationToken);

		private static void RequestShutdown(uint flags, uint reason, bool forceIfHung) {
			Guard.Argument(reason != 0, nameof(reason), "Supply a Windows shutdown reason code.");
			using var privilege = new TokenPrivilegeScope(new[] { "SeShutdownPrivilege" });
			if (!WinAPI.USER32.ExitWindowsEx(flags | (forceIfHung ? 0x10U : 0), reason))
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		private static void Suspend(bool hibernate, bool disableWakeEvents) {
			using var privilege = new TokenPrivilegeScope(new[] { "SeShutdownPrivilege" });
			if (!WinAPI.POWRPROF.SetSuspendState(hibernate, false, disableWakeEvents))
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}
	}
}
