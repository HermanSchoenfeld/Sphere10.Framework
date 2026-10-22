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

namespace Sphere10.Framework.Windows;

/// <summary>Reusable Windows computer identity, firmware and installed hardware queries.</summary>
public class ComputerUtil {
	public virtual WindowsComputerInfo Query() => WindowsComputer.Query();

	public virtual Task<WindowsComputerInfo> QueryAsync(CancellationToken cancellationToken = default) => WindowsComputer.QueryAsync(cancellationToken);

	public virtual WindowsSystemProductInfo[] GetSystemProducts() => WindowsComputer.GetSystemProducts();

	public virtual Task<WindowsSystemProductInfo[]> GetSystemProductsAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetSystemProductsAsync(cancellationToken);

	public virtual WindowsBiosInfo[] GetBios() => WindowsComputer.GetBios();

	public virtual Task<WindowsBiosInfo[]> GetBiosAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetBiosAsync(cancellationToken);

	public virtual WindowsBaseboardInfo[] GetBaseboards() => WindowsComputer.GetBaseboards();

	public virtual Task<WindowsBaseboardInfo[]> GetBaseboardsAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetBaseboardsAsync(cancellationToken);

	public virtual WindowsProcessorInfo[] GetProcessors() => WindowsComputer.GetProcessors();

	public virtual Task<WindowsProcessorInfo[]> GetProcessorsAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetProcessorsAsync(cancellationToken);

	public virtual WindowsOperatingSystemInfo[] GetOperatingSystems() => WindowsComputer.GetOperatingSystems();

	public virtual Task<WindowsOperatingSystemInfo[]> GetOperatingSystemsAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetOperatingSystemsAsync(cancellationToken);

	public virtual WindowsMemoryModuleInfo[] GetMemoryModules() => WindowsComputer.GetMemoryModules();

	public virtual Task<WindowsMemoryModuleInfo[]> GetMemoryModulesAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetMemoryModulesAsync(cancellationToken);

	public virtual WindowsMemoryArrayInfo[] GetMemoryArrays() => WindowsComputer.GetMemoryArrays();

	public virtual Task<WindowsMemoryArrayInfo[]> GetMemoryArraysAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetMemoryArraysAsync(cancellationToken);

	public virtual WindowsEnclosureInfo[] GetEnclosures() => WindowsComputer.GetEnclosures();

	public virtual Task<WindowsEnclosureInfo[]> GetEnclosuresAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetEnclosuresAsync(cancellationToken);

	public virtual WindowsVideoControllerInfo[] GetVideoControllers() => WindowsComputer.GetVideoControllers();

	public virtual Task<WindowsVideoControllerInfo[]> GetVideoControllersAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetVideoControllersAsync(cancellationToken);

	public virtual WindowsPlugAndPlayDeviceInfo[] GetDevices() => WindowsComputer.GetDevices();

	public virtual Task<WindowsPlugAndPlayDeviceInfo[]> GetDevicesAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetDevicesAsync(cancellationToken);

	public virtual WindowsMonitorInfo[] GetMonitors() => WindowsComputer.GetMonitors();

	public virtual Task<WindowsMonitorInfo[]> GetMonitorsAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetMonitorsAsync(cancellationToken);

	public virtual WindowsPrinterInfo[] GetPrinters() => WindowsComputer.GetPrinters();

	public virtual Task<WindowsPrinterInfo[]> GetPrintersAsync(CancellationToken cancellationToken = default) => WindowsComputer.GetPrintersAsync(cancellationToken);

	public virtual void Rename(string expectedCurrentName, string newName) => WindowsComputer.Rename(expectedCurrentName, newName);

	public virtual Task RenameAsync(string expectedCurrentName, string newName, CancellationToken cancellationToken = default) => WindowsComputer.RenameAsync(expectedCurrentName, newName, cancellationToken);

	public virtual WindowsPowerCapabilities GetPowerCapabilities() => new(WinAPI.POWRPROF.IsPwrSuspendAllowed(), WinAPI.POWRPROF.IsPwrHibernateAllowed(), WinAPI.POWRPROF.IsPwrShutdownAllowed());

	public virtual Task<WindowsPowerCapabilities> GetPowerCapabilitiesAsync(CancellationToken cancellationToken = default) => Task.Run(GetPowerCapabilities, cancellationToken);

	public virtual WindowsPowerPlanInfo[] GetPowerPlans(CancellationToken cancellationToken = default) => WindowsPowerInventory.GetPlans(cancellationToken);

	public virtual Task<WindowsPowerPlanInfo[]> GetPowerPlansAsync(CancellationToken cancellationToken = default) => Task.Run(() => GetPowerPlans(cancellationToken), cancellationToken);

	public virtual WindowsPowerSettingInfo[] GetPowerSettings(Guid planId, CancellationToken cancellationToken = default) => WindowsPowerInventory.GetSettings(planId, cancellationToken);

	public virtual Task<WindowsPowerSettingInfo[]> GetPowerSettingsAsync(Guid planId, CancellationToken cancellationToken = default) => Task.Run(() => GetPowerSettings(planId, cancellationToken), cancellationToken);

	public virtual WindowsPowerDiagnosticInfo[] GetPowerDiagnostics(CancellationToken cancellationToken = default) => WindowsPowerInventory.GetDiagnostics(cancellationToken);

	public virtual Task<WindowsPowerDiagnosticInfo[]> GetPowerDiagnosticsAsync(CancellationToken cancellationToken = default) => Task.Run(() => GetPowerDiagnostics(cancellationToken), cancellationToken);

	/// <summary>Requests power-off. Acceptance does not guarantee completion; the interactive user or another application may veto shutdown.</summary>
	public virtual void Shutdown(uint reason, bool forceIfHung = false) => RequestShutdown(8, reason, forceIfHung);

	public virtual Task ShutdownAsync(uint reason, bool forceIfHung = false, CancellationToken cancellationToken = default) => Task.Run(() => Shutdown(reason, forceIfHung), cancellationToken);

	public virtual void Restart(uint reason, bool forceIfHung = false) => RequestShutdown(2, reason, forceIfHung);

	public virtual Task RestartAsync(uint reason, bool forceIfHung = false, CancellationToken cancellationToken = default) => Task.Run(() => Restart(reason, forceIfHung), cancellationToken);

	public virtual void Logoff(bool forceIfHung = false) {
		if (!WinAPI.USER32.ExitWindowsEx(forceIfHung ? 0x10U : 0, 0))
			throw new Win32Exception(Marshal.GetLastWin32Error());
	}

	public virtual Task LogoffAsync(bool forceIfHung = false, CancellationToken cancellationToken = default) => Task.Run(() => Logoff(forceIfHung), cancellationToken);

	public virtual void Lock() {
		if (!WinAPI.USER32.LockWorkStation())
			throw new Win32Exception(Marshal.GetLastWin32Error());
	}

	public virtual Task LockAsync(CancellationToken cancellationToken = default) => Task.Run(Lock, cancellationToken);

	public virtual void Sleep(bool disableWakeEvents = false) => Suspend(false, disableWakeEvents);

	public virtual Task SleepAsync(bool disableWakeEvents = false, CancellationToken cancellationToken = default) => Task.Run(() => Sleep(disableWakeEvents), cancellationToken);

	public virtual void Hibernate(bool disableWakeEvents = false) => Suspend(true, disableWakeEvents);

	public virtual Task HibernateAsync(bool disableWakeEvents = false, CancellationToken cancellationToken = default) => Task.Run(() => Hibernate(disableWakeEvents), cancellationToken);

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
