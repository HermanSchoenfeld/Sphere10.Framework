// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

using Sphere10.Framework.Windows.Services;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>
	/// Provides utility methods for querying Windows services.
	/// </summary>
	public static class Services {
		/// <summary>Creates a fresh lazy service or driver inventory; this utility does not cache its published rows.</summary>
		public static WindowsServiceDataSource CreateDataSource(bool drivers = false) => new WindowsServiceDataSource(drivers);


		public static bool IsServiceInstalled(string serviceName) {
			if (!TryGetServiceController(serviceName, out var controller))
				return false;
			using (controller)
				return true;
		}

		/// <summary>The caller owns the returned service controller.</summary>
		public static bool TryGetServiceController(string serviceName, out ServiceController serviceController) {
			Guard.ArgumentNotNullOrWhitespace(serviceName, nameof(serviceName));
			serviceController = null;
			var controllers = ServiceController.GetServices();
			ServiceController retained = null;
			using (Tools.Scope.ExecuteOnDispose(() => {
				foreach (var controller in controllers)
					if (!ReferenceEquals(controller, retained))
						controller.Dispose();
			})) {
				retained = controllers.FirstOrDefault(controller => string.Equals(controller.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));
				serviceController = retained;
				return retained != null;
			}
		}

		public static WindowsServiceInfo[] GetServices() => ReadControllers(ServiceController.GetServices());
		public static WindowsServiceInfo[] GetDrivers() => ReadControllers(ServiceController.GetDevices());
		public static Task<WindowsServiceInfo[]> GetServicesAsync(CancellationToken cancellationToken = default) => Task.Run(() => ReadControllers(ServiceController.GetServices(), () => cancellationToken.ThrowIfCancellationRequested()), cancellationToken);
		public static Task<WindowsServiceInfo[]> GetDriversAsync(CancellationToken cancellationToken = default) => Task.Run(() => ReadControllers(ServiceController.GetDevices(), () => cancellationToken.ThrowIfCancellationRequested()), cancellationToken);
		public static Task<WindowsServiceInfo> ReadServiceAsync(string name, CancellationToken cancellationToken = default) => Task.Run(() => ReadService(name, () => cancellationToken.ThrowIfCancellationRequested()), cancellationToken);
		public static Task<WindowsServiceConfiguration> ReadConfigurationAsync(string name, CancellationToken cancellationToken = default) => Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();
			return ReadConfiguration(name);
		}, cancellationToken);

		public static WindowsServiceInfo ReadService(string name) => ReadService(name, null);

		/// <summary>Reads optional service configuration without changing the service or enabling privileges.</summary>
		public static Task<WindowsServiceAdvancedConfiguration> ReadAdvancedConfigurationAsync(string name, CancellationToken cancellationToken = default)
			=> Task.Run(() => ReadAdvancedConfigurationCore(name, () => cancellationToken.ThrowIfCancellationRequested()), cancellationToken);

		public static WindowsServiceAdvancedConfiguration ReadAdvancedConfiguration(string name) => ReadAdvancedConfigurationCore(name, null);

		public static WindowsServiceConfiguration ReadConfiguration(string name) {
			Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
			using var manager = OpenManager();
			using var service = OpenService(manager, name, WinAPI.ADVAPI32.SERVICE_QUERY_CONFIG);
			WinAPI.ADVAPI32.QueryServiceConfigW(service.DangerousGetHandle(), IntPtr.Zero, 0, out var needed);
			var error = Marshal.GetLastWin32Error();
			if (error != 122)
				throw new Win32Exception(error);
			Guard.Ensure(needed > 0 && needed <= 65536, "The native service configuration size is invalid.");
			using var allocation = new UnmanagedMemoryScope((int)needed);
			var pointer = allocation.Item;
			if (!WinAPI.ADVAPI32.QueryServiceConfigW(service.DangerousGetHandle(), pointer, needed, out _))
				throw new Win32Exception(Marshal.GetLastWin32Error());
			var data = Marshal.PtrToStructure<WinAPI.ADVAPI32.QUERY_SERVICE_CONFIG>(pointer);
			return new WindowsServiceConfiguration {
				Name = name, DisplayName = Marshal.PtrToStringUni(data.DisplayName), BinaryPath = Marshal.PtrToStringUni(data.BinaryPath),
				Account = Marshal.PtrToStringUni(data.StartName), ServiceType = (ServiceType)data.ServiceType, StartMode = (ServiceStartMode)data.StartType,
				ErrorControl = data.ErrorControl, LoadOrderGroup = Marshal.PtrToStringUni(data.LoadOrderGroup), TagId = data.TagId,
				Dependencies = ReadMultiString(data.Dependencies, pointer, needed)
			};
		}

		/// <summary>Extracts an explicit absolute executable path from a service command line without running it or searching PATH.</summary>
		public static string ResolveExecutablePath(string binaryPath) {
			Guard.ArgumentNotNullOrWhitespace(binaryPath, nameof(binaryPath));
			var command = System.Environment.ExpandEnvironmentVariables(binaryPath.Trim());
			var parsed = Tools.Runtime.ParseCommandLine(command);
			Guard.Argument(Path.IsPathFullyQualified(parsed.executable), nameof(binaryPath), "The service registration does not contain an explicit absolute executable path.");
			return Path.GetFullPath(parsed.executable);
		}

		public static void SetStartMode(string name, ServiceStartMode mode) {
			Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
			Guard.Argument(Enum.IsDefined(mode), nameof(mode), "The service startup mode is invalid.");
			using var manager = OpenManager();
			using var service = OpenService(manager, name, WinAPI.ADVAPI32.SERVICE_CHANGE_CONFIG);
			if (!WinAPI.ADVAPI32.ChangeServiceConfigW(service.DangerousGetHandle(), WinAPI.ADVAPI32.SERVICE_NO_CHANGE, (uint)mode,
				WinAPI.ADVAPI32.SERVICE_NO_CHANGE, null, null, IntPtr.Zero, null, null, null, null))
				throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		public static Task SetStartModeAsync(string name, ServiceStartMode mode, CancellationToken cancellationToken = default) => Task.Run(() => {
			cancellationToken.ThrowIfCancellationRequested();
			SetStartMode(name, mode);
		}, cancellationToken);

		public static void StartService(string name, TimeSpan timeout) => ControlService(name, timeout, ServiceControllerStatus.Running, controller => controller.Start());

		public static void StopService(string name, TimeSpan timeout) => ControlService(name, timeout, ServiceControllerStatus.Stopped, controller => controller.Stop(false));

		public static void PauseService(string name, TimeSpan timeout) => ControlService(name, timeout, ServiceControllerStatus.Paused, controller => controller.Pause());

		public static void ResumeService(string name, TimeSpan timeout) => ControlService(name, timeout, ServiceControllerStatus.Running, controller => controller.Continue());

		public static Task StartServiceAsync(string name, TimeSpan timeout, CancellationToken cancellationToken = default)
			=> ControlServiceAsync(name, timeout, ServiceControllerStatus.Running, controller => controller.Start(), cancellationToken);

		public static Task StopServiceAsync(string name, TimeSpan timeout, CancellationToken cancellationToken = default)
			=> ControlServiceAsync(name, timeout, ServiceControllerStatus.Stopped, controller => controller.Stop(false), cancellationToken);

		public static Task PauseServiceAsync(string name, TimeSpan timeout, CancellationToken cancellationToken = default)
			=> ControlServiceAsync(name, timeout, ServiceControllerStatus.Paused, controller => controller.Pause(), cancellationToken);
		public static Task ResumeServiceAsync(string name, TimeSpan timeout, CancellationToken cancellationToken = default)
			=> ControlServiceAsync(name, timeout, ServiceControllerStatus.Running, controller => controller.Continue(), cancellationToken);
		public static void WaitForStatus(string name, ServiceControllerStatus desired, TimeSpan timeout) {
			Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
			Guard.Argument(timeout > TimeSpan.Zero, nameof(timeout), "The status-wait timeout must be positive.");
			using var controller = new ServiceController(name);
			controller.WaitForStatus(desired, timeout);
		}
		/// <summary>Observes a service state without sending a service-control request.</summary>
		public static async Task WaitForStatusAsync(string name, ServiceControllerStatus desired, TimeSpan timeout, CancellationToken cancellationToken = default) {
			Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
			Guard.Argument(timeout > TimeSpan.Zero, nameof(timeout), "The status-wait timeout must be positive.");
			cancellationToken.ThrowIfCancellationRequested();
			using var controller = new ServiceController(name);
			await WaitForStatusAsync(controller, desired, timeout, cancellationToken).ConfigureAwait(false);
		}
		private static WindowsServiceAdvancedConfiguration ReadAdvancedConfigurationCore(string name, Action checkpoint) {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
			checkpoint?.Invoke();
			using var manager = OpenManager();
			using var service = OpenService(manager, name, WinAPI.ADVAPI32.SERVICE_QUERY_CONFIG);
			var issues = new List<WindowsServiceConfigurationIssue>();
			var result = new WindowsServiceAdvancedConfiguration { Name = name };
			ReadSection(WinAPI.ADVAPI32.SERVICE_CONFIG_FAILURE_ACTIONS, pointer => {
				EnsureConfigurationRange(pointer.Pointer, Marshal.SizeOf<WinAPI.ADVAPI32.SERVICE_FAILURE_ACTIONS>(), pointer);
				var data = Marshal.PtrToStructure<WinAPI.ADVAPI32.SERVICE_FAILURE_ACTIONS>(pointer.Pointer);
				var actions = ReadArray<WinAPI.ADVAPI32.SC_ACTION>(data.Actions, data.ActionCount, pointer);
				result = result with {
					FailureResetPeriodSeconds = data.ResetPeriod,
					FailureRebootMessage = ReadConfigurationString(data.RebootMessage, pointer),
					FailureCommand = ReadConfigurationString(data.Command, pointer),
					FailureActions = actions.Select(action => new WindowsServiceFailureAction(action.Type, action.DelayMilliseconds)).ToArray()
				};
			});
			ReadSection(WinAPI.ADVAPI32.SERVICE_CONFIG_DELAYED_AUTO_START_INFO, pointer => result = result with { DelayedAutoStart = ReadDword(pointer) != 0 });
			ReadSection(WinAPI.ADVAPI32.SERVICE_CONFIG_FAILURE_ACTIONS_FLAG, pointer => result = result with { FailureActionsOnNonCrashFailures = ReadDword(pointer) != 0 });
			ReadSection(WinAPI.ADVAPI32.SERVICE_CONFIG_SERVICE_SID_INFO, pointer => result = result with { ServiceSidType = ReadDword(pointer) });
			ReadSection(WinAPI.ADVAPI32.SERVICE_CONFIG_REQUIRED_PRIVILEGES_INFO, pointer => {
				EnsureConfigurationRange(pointer.Pointer, IntPtr.Size, pointer);
				result = result with { RequiredPrivileges = ReadMultiString(Marshal.ReadIntPtr(pointer.Pointer), pointer.Pointer, pointer.Length) };
			});
			ReadSection(WinAPI.ADVAPI32.SERVICE_CONFIG_PRESHUTDOWN_INFO, pointer => result = result with { PreshutdownTimeoutMilliseconds = ReadDword(pointer) });
			ReadSection(WinAPI.ADVAPI32.SERVICE_CONFIG_LAUNCH_PROTECTED, pointer => result = result with { LaunchProtection = ReadDword(pointer) });
			ReadSection(WinAPI.ADVAPI32.SERVICE_CONFIG_TRIGGER_INFO, pointer => {
				EnsureConfigurationRange(pointer.Pointer, Marshal.SizeOf<WinAPI.ADVAPI32.SERVICE_TRIGGER_INFO>(), pointer);
				var data = Marshal.PtrToStructure<WinAPI.ADVAPI32.SERVICE_TRIGGER_INFO>(pointer.Pointer);
				var triggers = new List<WindowsServiceTrigger>();
				foreach (var trigger in ReadArray<WinAPI.ADVAPI32.SERVICE_TRIGGER>(data.Triggers, data.TriggerCount, pointer)) {
					Guid? subtype = null;
					if (trigger.Subtype != IntPtr.Zero) {
						EnsureConfigurationRange(trigger.Subtype, Marshal.SizeOf<Guid>(), pointer);
						subtype = Marshal.PtrToStructure<Guid>(trigger.Subtype);
					}
					var values = new List<WindowsServiceTriggerData>();
					foreach (var item in ReadArray<WinAPI.ADVAPI32.SERVICE_TRIGGER_SPECIFIC_DATA_ITEM>(trigger.DataItems, trigger.DataItemCount, pointer)) {
						Guard.Ensure(item.Size <= 1024, "The native trigger data exceeds the Windows contract.");
						var bytes = new byte[checked((int)item.Size)];
						if (bytes.Length != 0) {
							EnsureConfigurationRange(item.Data, bytes.Length, pointer);
							Marshal.Copy(item.Data, bytes, 0, bytes.Length);
						}
						values.Add(new WindowsServiceTriggerData(item.Type, bytes));
					}
					triggers.Add(new WindowsServiceTrigger(trigger.Type, trigger.Action, subtype, values.ToArray()));
				}
				result = result with { Triggers = triggers.ToArray() };
			});
			checkpoint?.Invoke();
			return result with { Issues = issues.ToArray() };

			void ReadSection(uint level, Action<ConfigurationBuffer> read) {
				checkpoint?.Invoke();
				Tools.Exceptions.ExecuteCapturingException<Win32Exception>(() => {
					WinAPI.ADVAPI32.QueryServiceConfig2W(service.DangerousGetHandle(), level, IntPtr.Zero, 0, out var needed);
					var error = Marshal.GetLastWin32Error();
					if (error != 122)
						throw new Win32Exception(error);
					Guard.Ensure(needed > 0 && needed <= 8192, "The native optional service configuration size is invalid.");
					using var allocation = new UnmanagedMemoryScope((int)needed);
					if (!WinAPI.ADVAPI32.QueryServiceConfig2W(service.DangerousGetHandle(), level, allocation.Item, needed, out _))
						throw new Win32Exception(Marshal.GetLastWin32Error());
					read(new ConfigurationBuffer(allocation.Item, needed));
				}, out var failure);
				if (failure != null)
					issues.Add(new WindowsServiceConfigurationIssue(level, failure.NativeErrorCode));
			}
		}
		private static uint ReadDword(ConfigurationBuffer buffer) {
			EnsureConfigurationRange(buffer.Pointer, sizeof(uint), buffer);
			return unchecked((uint)Marshal.ReadInt32(buffer.Pointer));
		}
		private static T[] ReadArray<T>(IntPtr value, uint count, ConfigurationBuffer buffer)
			where T : struct {
			var width = Marshal.SizeOf<T>();
			var length = checked((int)count);
			if (length == 0)
				return System.Array.Empty<T>();
			EnsureConfigurationRange(value, checked(width * length), buffer);
			var result = new T[length];
			for (var index = 0; index < length; index++)
				result[index] = Marshal.PtrToStructure<T>(IntPtr.Add(value, index * width));
			return result;
		}
		private static string ReadConfigurationString(IntPtr value, ConfigurationBuffer buffer) {
			if (value == IntPtr.Zero)
				return string.Empty;
			EnsureConfigurationRange(value, sizeof(char), buffer);
			var remaining = checked((int)((buffer.Length - (value.ToInt64() - buffer.Pointer.ToInt64())) / 2));
			for (var index = 0; index < remaining; index++)
				if (Marshal.ReadInt16(value, index * 2) == 0)
					return Marshal.PtrToStringUni(value, index);
			throw new InvalidOperationException("The native optional service string is not terminated.");
		}

		private static void EnsureConfigurationRange(IntPtr value, int length, ConfigurationBuffer buffer) {
			var offset = value.ToInt64() - buffer.Pointer.ToInt64();
			Guard.Ensure(offset >= 0 && length >= 0 && offset <= buffer.Length && length <= buffer.Length - offset,
				"The native optional service data is outside its owning buffer.");
		}

		private static WindowsServiceInfo ReadService(string name, Action checkpoint) {
			Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
			checkpoint?.Invoke();
			WindowsServiceConfiguration configuration = null;
			Tools.Exceptions.ExecuteCapturingException<Win32Exception>(() => configuration = ReadConfiguration(name), out var configurationError);
			WinAPI.ADVAPI32.SERVICE_STATUS_PROCESS? status = null;
			Tools.Exceptions.ExecuteCapturingException<Win32Exception>(() => {
				using var manager = OpenManager();
				using var service = OpenService(manager, name, WinAPI.ADVAPI32.SERVICE_QUERY_STATUS);
				if (!WinAPI.ADVAPI32.QueryServiceStatusEx(service.DangerousGetHandle(), 0, out var value,
					(uint)Marshal.SizeOf<WinAPI.ADVAPI32.SERVICE_STATUS_PROCESS>(), out _))
					throw new Win32Exception(Marshal.GetLastWin32Error());
				status = value;
			}, out var statusError);
			checkpoint?.Invoke();
			var description = string.Empty;
			Tools.Exceptions.ExecuteCapturingException<Win32Exception>(() => description = ReadDescription(name), out var descriptionError);
			var dependents = System.Array.Empty<string>();
			Tools.Exceptions.ExecuteCapturingException(() => {
				using var controller = new ServiceController(name);
				var children = controller.DependentServices;
				using var cleanup = Tools.Scope.ExecuteOnDispose(() => { foreach (var child in children) child.Dispose(); });
				dependents = children.Select(child => child.ServiceName).ToArray();
			}, out var dependencyError, error => error is Win32Exception or InvalidOperationException { InnerException: Win32Exception });
			checkpoint?.Invoke();
			return new WindowsServiceInfo {
				Name = name, DisplayName = configuration?.DisplayName ?? name, Description = description,
				BinaryPath = configuration?.BinaryPath, Account = configuration?.Account, ServiceType = configuration?.ServiceType ?? 0,
				StartMode = configuration?.StartMode, Status = status.HasValue ? (ServiceControllerStatus)status.Value.CurrentState : null,
				ProcessId = status?.ProcessId, Win32ExitCode = status?.Win32ExitCode,
				CanStop = status.HasValue ? (status.Value.ControlsAccepted & 1) != 0 : null,
				CanPause = status.HasValue ? (status.Value.ControlsAccepted & 2) != 0 : null,
				Dependencies = configuration?.Dependencies ?? System.Array.Empty<string>(), DependentServices = dependents,
				ConfigurationErrorCode = configurationError?.NativeErrorCode ?? 0, StatusErrorCode = statusError?.NativeErrorCode ?? 0,
				DescriptionErrorCode = descriptionError?.NativeErrorCode ?? 0,
				DependencyErrorCode = dependencyError is Win32Exception nativeError ? nativeError.NativeErrorCode : (dependencyError?.InnerException as Win32Exception)?.NativeErrorCode ?? 0
			};
		}

		private static async Task ControlServiceAsync(string name, TimeSpan timeout, ServiceControllerStatus desired, Action<ServiceController> action, CancellationToken cancellationToken) {
			Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
			Guard.Argument(timeout > TimeSpan.Zero, nameof(timeout), "The status-wait timeout must be positive.");
			using var controller = await Task.Run(() => {
				cancellationToken.ThrowIfCancellationRequested();
				var service = new ServiceController(name);
				try { cancellationToken.ThrowIfCancellationRequested(); action(service); return service; }
				catch { service.Dispose(); throw; }
			}, cancellationToken).ConfigureAwait(false);
			await WaitForStatusAsync(controller, desired, timeout, cancellationToken).ConfigureAwait(false);
		}

		private static async Task WaitForStatusAsync(ServiceController controller, ServiceControllerStatus desired, TimeSpan timeout, CancellationToken cancellationToken) {
			var started = Stopwatch.GetTimestamp();
			while (true) {
				cancellationToken.ThrowIfCancellationRequested();
				var status = await Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); controller.Refresh(); return controller.Status; }, cancellationToken).ConfigureAwait(false);
				if (status == desired)
					return;
				var remaining = timeout - Stopwatch.GetElapsedTime(started);
				if (remaining <= TimeSpan.Zero)
					throw new System.ServiceProcess.TimeoutException("The service did not reach the requested state within the timeout.");
				await Task.Delay(remaining < TimeSpan.FromMilliseconds(250) ? remaining : TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
			}
		}

		private static void ControlService(string name, TimeSpan timeout, ServiceControllerStatus status, Action<ServiceController> action) {
			Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
			Guard.Argument(timeout > TimeSpan.Zero, nameof(timeout), "The status-wait timeout must be positive.");
			using var controller = new ServiceController(name);
			action(controller);
			controller.WaitForStatus(status, timeout);
		}

		private static WindowsServiceInfo[] ReadControllers(ServiceController[] controllers, Action checkpoint = null) {
			using var cleanup = Tools.Scope.ExecuteOnDispose(() => { foreach (var controller in controllers) controller.Dispose(); });
			var result = new List<WindowsServiceInfo>();
			foreach (var controller in controllers) {
				checkpoint?.Invoke();
				result.Add(ReadService(controller.ServiceName, checkpoint));
			}
			return result.ToArray();
		}

		private static string ReadDescription(string name) {
			using var manager = OpenManager();
			using var service = OpenService(manager, name, WinAPI.ADVAPI32.SERVICE_QUERY_CONFIG);
			WinAPI.ADVAPI32.QueryServiceConfig2W(service.DangerousGetHandle(), WinAPI.ADVAPI32.SERVICE_CONFIG_DESCRIPTION, IntPtr.Zero, 0, out var needed);
			var error = Marshal.GetLastWin32Error();
			if (error != 122)
				throw new Win32Exception(error);
			Guard.Ensure(needed >= IntPtr.Size && needed <= 65536, "The native service description size is invalid.");
			using var allocation = new UnmanagedMemoryScope((int)needed);
			var pointer = allocation.Item;
			if (!WinAPI.ADVAPI32.QueryServiceConfig2W(service.DangerousGetHandle(), WinAPI.ADVAPI32.SERVICE_CONFIG_DESCRIPTION, pointer, needed, out _))
				throw new Win32Exception(Marshal.GetLastWin32Error());
			return Marshal.PtrToStringUni(Marshal.ReadIntPtr(pointer)) ?? string.Empty;
		}

		private static string[] ReadMultiString(IntPtr value, IntPtr allocation, uint size) {
			if (value == IntPtr.Zero)
				return System.Array.Empty<string>();
			var offset = value.ToInt64() - allocation.ToInt64();
			Guard.Ensure(offset >= 0 && offset < size, "The native dependency string is outside its configuration buffer.");
			var maximum = (int)((size - offset) / 2);
			var result = new List<string>();
			var start = 0;
			for (var index = 0; index < maximum; index++) {
				if (Marshal.ReadInt16(value, index * 2) != 0)
					continue;
				if (start == index)
					return result.ToArray();
				result.Add(Marshal.PtrToStringUni(IntPtr.Add(value, start * 2), index - start));
				start = index + 1;
			}
			throw new InvalidOperationException("The native dependency string is not terminated.");
		}

		private static ServiceHandle OpenManager() => ServiceHandle.From(WinAPI.ADVAPI32.OpenSCManagerW(null, null, WinAPI.ADVAPI32.SC_MANAGER_CONNECT));
		private static ServiceHandle OpenService(ServiceHandle manager, string name, uint access) => ServiceHandle.From(WinAPI.ADVAPI32.OpenServiceW(manager.DangerousGetHandle(), name, access));

		#region Inner Types

		private readonly record struct ConfigurationBuffer(IntPtr Pointer, uint Length);

		private sealed class ServiceHandle : SafeHandleZeroOrMinusOneIsInvalid {
			private ServiceHandle(IntPtr value) : base(true) => SetHandle(value);
			public static ServiceHandle From(IntPtr value) => value == IntPtr.Zero ? throw new Win32Exception(Marshal.GetLastWin32Error()) : new ServiceHandle(value);
			protected override bool ReleaseHandle() => WinAPI.ADVAPI32.CloseServiceHandle(handle);
		}

		#endregion
	}
}
