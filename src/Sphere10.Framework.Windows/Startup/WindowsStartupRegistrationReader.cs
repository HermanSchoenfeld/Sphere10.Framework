// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;

namespace Sphere10.Framework.Windows.Startup;

/// <summary>One bounded, read-only native traversal. Each family retains independent availability; no registration is activated or edited.</summary>
internal sealed class WindowsStartupRegistrationReader {
	private readonly List<WindowsStartupRegistration> _rows = new();
	private readonly int _maximumRows;
	private readonly TimeSpan _timeout;
	private readonly Action _checkpoint;
	private bool _truncated;

	public WindowsStartupRegistrationReader(int maximumRows, int queryTimeoutSeconds, Action checkpoint) {
		Guard.ArgumentInRange(maximumRows, 1, 10000, nameof(maximumRows));
		Guard.ArgumentInRange(queryTimeoutSeconds, 1, 30, nameof(queryTimeoutSeconds));
		_maximumRows = maximumRows;
		_timeout = TimeSpan.FromSeconds(queryTimeoutSeconds);
		_checkpoint = checkpoint;
	}

	public WindowsStartupRegistration[] Read(WindowsStartupSources sources) {
		Debug.AssertNotMainThread();
		Guard.Argument((sources & ~WindowsStartupSources.All) == 0, nameof(sources), "Select supported launch-source families.");
		_checkpoint?.Invoke();
		if (sources.HasFlag(WindowsStartupSources.LogonRegistry))
			ReadLogonRegistry();
		if (sources.HasFlag(WindowsStartupSources.ScheduledTasks) && !_truncated)
			ReadTasks();
		if (sources.HasFlag(WindowsStartupSources.WmiSubscriptions) && !_truncated)
			ReadSubscriptions();
		if (_truncated)
			_rows.Add(new WindowsStartupRegistration { Identity = "limit", Name = "Capture Limit", Availability = $"The {_maximumRows:N0}-row limit was reached; later registrations/families were not captured." });
		_checkpoint?.Invoke();
		return _rows.OrderBy(row => row.Family).ThenBy(row => row.Location, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase).ToArray();
	}

	private bool Add(WindowsStartupRegistration row) {
		_checkpoint?.Invoke();
		if (_rows.Count >= _maximumRows) {
			_truncated = true;
			return false;
		}
		_rows.Add(row);
		return true;
	}

	private void ReadLogonRegistry() {
		var views = Environment.Is64BitOperatingSystem ? new[] { RegistryView.Registry64, RegistryView.Registry32 } : new[] { RegistryView.Registry32 };
		foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
			foreach (var view in views) {
				var winlogon = new RegistryAddress(hive, view, @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon");
				ReadRegistryValues(winlogon, new[] { "Shell", "Userinit", "Taskman" }, "Winlogon");
				if (_truncated)
					return;
				var activeSetup = new RegistryAddress(hive, view, @"Software\Microsoft\Active Setup\Installed Components");
				Capture(WindowsStartupSources.LogonRegistry, Display(activeSetup), () => {
					// Missing keys are ordinary on some Windows editions. Values and subkeys retain the explicit native view.
					using var parent = Tools.Windows.Registry.OpenKey(activeSetup, throwIfMissing: false);
					if (parent == null)
						return;
					foreach (var key in Tools.Windows.Registry.GetSubKeys(activeSetup)) {
						if (_truncated)
							break;
						ReadRegistryValues(key, new[] { "StubPath" }, "Active Setup");
					}
				});
				if (_truncated)
					return;
			}
	}

	private void ReadRegistryValues(RegistryAddress address, string[] names, string family) {
		Capture(WindowsStartupSources.LogonRegistry, Display(address), () => {
			var values = Tools.Windows.Registry.ReadValues(address, false);
			var componentState = family == "Active Setup" ? " / " + string.Join("; ", values.Where(value => value.Name is "Version" or "IsInstalled" or "Locale").Select(value => value.Name + "=" + Clip(value.Data?.ToString()))) : string.Empty;
			foreach (var value in values.Where(value => names.Contains(value.Name, StringComparer.OrdinalIgnoreCase))) {
				if (!Add(new WindowsStartupRegistration {
					Identity = address + "|" + value.Name, Family = WindowsStartupSources.LogonRegistry,
					Name = value.Name, Scope = address.Hive == RegistryHive.CurrentUser ? "Current User" : "All Users",
					Location = Display(address), RegistryAddress = address, RegistryValueName = value.Name,
					Command = Clip(value.Data as string), Trigger = family + " Logon Configuration" + componentState,
					SourceState = "Registered (Read Only)", Availability = value.ReadError?.Message ?? (family == "Active Setup" ? "Installed-component registration; execution depends on per-user installation/version state." : "Stored configuration; multiple-command semantics are retained verbatim.")
				}))
					break;
			}
		});
	}

	private void ReadTasks() {
		ReadManagement(@"root\Microsoft\Windows\TaskScheduler", "SELECT * FROM MSFT_ScheduledTask", WindowsStartupSources.ScheduledTasks, item => {
			var allTriggers = item["Triggers"] as WindowsManagementObject[] ?? Array.Empty<WindowsManagementObject>();
			using var disposeTriggers = Tools.Scope.ExecuteOnDispose(() => { foreach (var trigger in allTriggers) trigger.Dispose(); });
			var triggers = allTriggers.Where(trigger =>
				trigger.ClassName is "MSFT_TaskBootTrigger" or "MSFT_TaskLogonTrigger").ToArray();
			if (triggers.Length == 0)
				return;
			var path = Text(item, "TaskPath") + Text(item, "TaskName");
			var actions = item["Actions"] as WindowsManagementObject[] ?? Array.Empty<WindowsManagementObject>();
			using var disposeActions = Tools.Scope.ExecuteOnDispose(() => { foreach (var action in actions) action.Dispose(); });
			var state = Convert.ToInt32(item["State"]);
			using var principal = item["Principal"] as WindowsManagementObject;
			for (var index = 0; index < actions.Length && !_truncated; index++) {
				var action = actions[index];
				var executable = action.ClassName == "MSFT_TaskExecAction" ? Text(action, "Execute") : string.Empty;
				Add(new WindowsStartupRegistration {
					Identity = "Task|" + path + "|" + index, Family = WindowsStartupSources.ScheduledTasks,
					Name = Text(item, "TaskName") + " / Action " + (index + 1), Location = path,
					Scope = principal == null ? "Task Principal Unavailable" : Text(principal, "UserId"),
					Command = string.IsNullOrEmpty(executable) ? action.ClassName + " / " + string.Join("; ", action.Properties.Select(property => property.Key + "=" + Clip(property.Value?.ToString()))) : "\"" + executable + "\" " + Text(action, "Arguments"),
					Executable = executable, Arguments = string.IsNullOrEmpty(executable) ? string.Empty : Text(action, "Arguments"),
					WorkingDirectory = string.IsNullOrEmpty(executable) ? string.Empty : Text(action, "WorkingDirectory"),
					Trigger = string.Join("; ", triggers.Select(trigger => trigger.ClassName + " Enabled=" + Text(trigger, "Enabled")
						+ ", Start=" + Text(trigger, "StartBoundary") + ", End=" + Text(trigger, "EndBoundary") + ", Delay=" + Text(trigger, "Delay")
						+ (trigger.ClassName == "MSFT_TaskLogonTrigger" ? ", User=" + Text(trigger, "UserId") : string.Empty))),
					SourceState = state switch { 0 => "Unknown", 1 => "Disabled", 2 => "Queued", 3 => "Ready", 4 => "Running", _ => state.ToString() },
					Availability = string.IsNullOrEmpty(executable) ? "Non-executable action fields retained; no file target inferred." : "Boot/logon trigger presence is not evidence of execution. Task conditions and trigger enablement still apply."
				});
			}
		});
	}

	private void ReadSubscriptions() {
		const string scope = @"root\subscription";
		ReadManagement(scope, "SELECT * FROM __EventFilter", WindowsStartupSources.WmiSubscriptions, item => Add(new WindowsStartupRegistration {
			Identity = item.Path, Family = WindowsStartupSources.WmiSubscriptions, Name = Text(item, "Name"), Scope = scope,
			Location = item.Path, Trigger = Text(item, "EventNamespace") + " / " + Text(item, "QueryLanguage") + " / " + Text(item, "Query"),
			SourceState = "Event Filter (Read Only)", Availability = "A filter alone does not establish an active consumer binding."
		}));
		ReadManagement(scope, "SELECT * FROM __EventConsumer", WindowsStartupSources.WmiSubscriptions, item => {
			var commandLine = item.ClassName == "CommandLineEventConsumer";
			var script = item.ClassName == "ActiveScriptEventConsumer";
			Add(new WindowsStartupRegistration {
				Identity = item.Path, Family = WindowsStartupSources.WmiSubscriptions, Name = Text(item, "Name"), Scope = scope,
				Location = item.Path, Command = commandLine ? Text(item, "CommandLineTemplate") : script ? Text(item, "ScriptFileName") + " / " + Text(item, "ScriptText") : item.ClassName,
				Executable = commandLine ? Text(item, "ExecutablePath") : string.Empty,
				WorkingDirectory = commandLine ? Text(item, "WorkingDirectory") : string.Empty,
				Trigger = item.ClassName + (script ? " / Engine=" + Text(item, "ScriptingEngine") : string.Empty), SourceState = "Event Consumer (Read Only)",
				Availability = "Consumer configuration is not proof of execution. WMI templates/scripts are retained verbatim; no expanded runtime command is inferred."
			});
		});
		ReadManagement(scope, "SELECT * FROM __FilterToConsumerBinding", WindowsStartupSources.WmiSubscriptions, item => Add(new WindowsStartupRegistration {
			Identity = item.Path, Family = WindowsStartupSources.WmiSubscriptions, Name = "Filter To Consumer Binding", Scope = scope,
			Location = item.Path, Command = Text(item, "Consumer"), Trigger = Text(item, "Filter"), SourceState = "Subscription Binding (Read Only)",
			Availability = "The captured association links the raw filter and consumer paths; no operation is invoked."
		}));
	}

	private void ReadManagement(string scope, string query, WindowsStartupSources family, Action<WindowsManagementObject> project) {
		if (_truncated)
			return;
		Capture(family, scope + " / " + query, () => {
			var elapsed = Stopwatch.StartNew();
			using var searcher = Tools.Windows.COM.CreateManagementQuery(_timeout, scope);
			var objects = searcher.Read(query);
			foreach (WindowsManagementObject item in objects) {
				using (item) {
					_checkpoint?.Invoke();
					if (_truncated)
						break;
					if (elapsed.Elapsed >= _timeout) {
						Add(new WindowsStartupRegistration { Identity = "Timeout|" + scope + "|" + query, Family = family, Name = "Provider Time Limit", Scope = scope,
							Location = query, Availability = "The provider enumeration time limit was reached; completed rows were retained. Native provider waits may finish after this cooperative deadline." });
						break;
					}
					Capture(family, item.Path, () => project(item));
					if (_truncated)
						break;
				}
			}
		});
	}

	private void Capture(WindowsStartupSources family, string location, Action read) {
		_checkpoint?.Invoke();
		Tools.Exceptions.ExecuteCapturingException(read, out var error, captured => captured is not OperationCanceledException);
		if (error != null)
			Add(new WindowsStartupRegistration { Identity = "Unavailable|" + family + "|" + location, Family = family, Name = "Source Unavailable", Location = location, Availability = error.Message });
	}

	private static string Text(WindowsManagementObject item, string name) => Clip(item[name]?.ToString());

	private static string Clip(string value) {
		value ??= string.Empty;
		return value.Length > 16384 ? value[..16384] + " [Field Truncated At 16,384 Characters]" : value;
	}

	private static string Display(RegistryAddress address)
		=> address.Hive switch { RegistryHive.CurrentUser => "HKEY_CURRENT_USER", RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE", _ => address.Hive.ToString() } + "\\" + address.SubKeyPath;
}
