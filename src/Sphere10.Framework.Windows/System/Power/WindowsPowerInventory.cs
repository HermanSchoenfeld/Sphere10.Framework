// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace Sphere10.Framework.Windows;

internal static class WindowsPowerInventory {
	private static readonly Guid NoSubgroup = new("fea3413e-7e05-4911-9a71-700331f1c294");
	private delegate uint TextQuery(byte[] buffer, ref uint size);

	internal static WindowsPowerPlanInfo[] GetPlans(CancellationToken cancellationToken) {
		Debug.AssertNotMainThread();
		cancellationToken.ThrowIfCancellationRequested();
		Check(WinAPI.POWRPROF.PowerGetActiveScheme(IntPtr.Zero, out var pointer));
		using var release = Tools.Scope.ExecuteOnDispose(() => WinAPI.KERNEL32.LocalFree(pointer));
		var active = Marshal.PtrToStructure<Guid>(pointer);
		return Enumerate(null, null, 16, cancellationToken).Select(id => new WindowsPowerPlanInfo(id,
			Friendly(id, null, null, id.ToString()), Description(id, null, null), id == active))
			.OrderByDescending(plan => plan.IsActive).ThenBy(plan => plan.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
	}

	internal static WindowsPowerSettingInfo[] GetSettings(Guid planId, CancellationToken cancellationToken) {
		Debug.AssertNotMainThread();
		var rows = new List<WindowsPowerSettingInfo>();
		foreach (var subgroup in Enumerate(planId, null, 17, cancellationToken).Append(NoSubgroup).Distinct()) {
			var groupName = Friendly(planId, subgroup, null, subgroup == NoSubgroup ? "Plan Settings" : subgroup.ToString());
			foreach (var setting in Enumerate(planId, subgroup, 18, cancellationToken)) {
				cancellationToken.ThrowIfCancellationRequested();
				var group = new[] { subgroup };
				var key = new[] { setting };
				var scheme = new[] { planId };
				var errors = new List<string>();
				var acStatus = WinAPI.POWRPROF.PowerReadACValueIndex(IntPtr.Zero, scheme, group, key, out var ac);
				var dcStatus = WinAPI.POWRPROF.PowerReadDCValueIndex(IntPtr.Zero, scheme, group, key, out var dc);
				if (acStatus != 0) errors.Add("AC: " + new Win32Exception((int)acStatus).Message);
				if (dcStatus != 0) errors.Add("DC: " + new Win32Exception((int)dcStatus).Message);
				var units = ReadText((byte[] buffer, ref uint size) => WinAPI.POWRPROF.PowerReadValueUnitsSpecifier(IntPtr.Zero, group, key, buffer, ref size));
				var minStatus = WinAPI.POWRPROF.PowerReadValueMin(IntPtr.Zero, group, key, out var minimum);
				var maxStatus = WinAPI.POWRPROF.PowerReadValueMax(IntPtr.Zero, group, key, out var maximum);
				var stepStatus = WinAPI.POWRPROF.PowerReadValueIncrement(IntPtr.Zero, group, key, out var increment);
				var choices = ReadChoices(group, key, cancellationToken);
				var acPolicy = new List<string>();
				var dcPolicy = new List<string>();
				var provenance = new List<string>();
				foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser }) {
					var path = @"SOFTWARE\Policies\Microsoft\Power\PowerSettings\" + setting.ToString("D");
					var address = new RegistryAddress(hive, RegistryView.Registry64, path);
					Tools.Exceptions.Execute(() => {
						var policyAc = Tools.Windows.Registry.ReadValue(address, "ACSettingIndex")?.Data;
						var policyDc = Tools.Windows.Registry.ReadValue(address, "DCSettingIndex")?.Data;
						var label = hive == RegistryHive.LocalMachine ? "Machine" : "User";
						if (policyAc != null) acPolicy.Add(label + ": " + PolicyValue(policyAc));
						if (policyDc != null) dcPolicy.Add(label + ": " + PolicyValue(policyDc));
						if (policyAc != null || policyDc != null) provenance.Add((hive == RegistryHive.LocalMachine ? "HKLM" : "HKCU") + @"\" + path);
					}, error => errors.Add("Policy: " + error.Message));
				}
				rows.Add(new WindowsPowerSettingInfo {
					PlanId = planId, SubgroupId = subgroup, Id = setting, Subgroup = groupName,
					Name = Friendly(planId, subgroup, setting, setting.ToString()), Description = Description(planId, subgroup, setting),
					AcValue = acStatus == 0 ? ac : null, DcValue = dcStatus == 0 ? dc : null,
					AcText = acStatus == 0 ? ValueText(ac, choices) : "Unavailable", DcText = dcStatus == 0 ? ValueText(dc, choices) : "Unavailable",
					Units = units, Minimum = minStatus == 0 ? minimum : null, Maximum = maxStatus == 0 ? maximum : null,
					Increment = stepStatus == 0 ? increment : null, Hidden = (WinAPI.POWRPROF.PowerReadSettingAttributes(group, key) & 1) != 0,
					AcPolicy = string.Join("; ", acPolicy), DcPolicy = string.Join("; ", dcPolicy), PolicySource = string.Join("; ", provenance),
					ReadError = string.Join("; ", errors)
				});
			}
		}
		return rows.OrderBy(row => row.Subgroup, StringComparer.CurrentCultureIgnoreCase).ThenBy(row => row.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
	}

	internal static WindowsPowerDiagnosticInfo[] GetDiagnostics(CancellationToken cancellationToken) {
		Debug.AssertNotMainThread();
		cancellationToken.ThrowIfCancellationRequested();
		var rows = new List<WindowsPowerDiagnosticInfo>();
		void Capability(string name, byte value) => rows.Add(new("Sleep Capabilities", name, value == 0 ? "No" : "Yes", "Windows Power Capabilities"));
		if (WinAPI.POWRPROF.GetPwrCapabilities(out var caps)) {
			Capability("S0 Low Power Idle (Modern Standby)", caps.AoAc);
			Capability("S1 Sleep", caps.SystemS1);
			Capability("S2 Sleep", caps.SystemS2);
			Capability("S3 Sleep", caps.SystemS3);
			Capability("S4 Hibernation", caps.SystemS4);
			Capability("S5 Soft Off", caps.SystemS5);
			Capability("Hibernation File Present", caps.HiberFilePresent);
			Capability("Fast Startup", caps.Hiberboot);
			Capability("Fast Hibernation", caps.FastSystemS4);
			Capability("Wake Alarm", caps.WakeAlarmPresent);
			Capability("Modern Standby Connectivity", caps.AoAcConnectivitySupported);
			Capability("Lid Switch", caps.LidPresent);
			Capability("Sleep Button", caps.SleepButtonPresent);
			Capability("Power Button", caps.PowerButtonPresent);
			Capability("Thermal Control", caps.ThermalControl);
			Capability("Processor Throttling", caps.ProcessorThrottle);
			Capability("Disk Spin Down", caps.DiskSpinDown);
			Capability("Battery Present", caps.SystemBatteriesPresent);
			rows.Add(new("Wake Capabilities", "AC Online Wake State", PowerState(caps.AcOnLineWake), "Windows Power Capabilities"));
			rows.Add(new("Wake Capabilities", "Lid Wake State", PowerState(caps.SoftLidWake), "Windows Power Capabilities"));
			rows.Add(new("Wake Capabilities", "RTC Wake State", PowerState(caps.RtcWake), "Windows Power Capabilities"));
			rows.Add(new("Wake Capabilities", "Minimum Device Wake State", PowerState(caps.MinDeviceWakeState), "Windows Power Capabilities"));
		} else
			rows.Add(new("Sleep Capabilities", "Availability", "Unavailable", "Windows Power Capabilities", "Windows did not return power capabilities."));
		var execution = new byte[4];
		var status = WinAPI.POWRPROF.CallNtPowerInformation(16, IntPtr.Zero, 0, execution, 4);
		if (status == 0) {
			var flags = EndianBitConverter.Little.ToUInt32(execution);
			rows.Add(new("Current Requests", "System Required", (flags & 1) != 0 ? "Yes" : "No", "Windows System Execution State"));
			rows.Add(new("Current Requests", "Display Required", (flags & 2) != 0 ? "Yes" : "No", "Windows System Execution State"));
			rows.Add(new("Current Requests", "User Present", (flags & 4) != 0 ? "Yes" : "No", "Windows System Execution State"));
		} else
			rows.Add(new("Current Requests", "Execution State", "Unavailable", "Windows System Execution State", "NTSTATUS 0x" + status.ToString("X8")));
		foreach (var (level, name) in new[] { (14, "Last Wake Interrupt Time"), (15, "Last Sleep Interrupt Time") }) {
			cancellationToken.ThrowIfCancellationRequested();
			var data = new byte[8];
			status = WinAPI.POWRPROF.CallNtPowerInformation(level, IntPtr.Zero, 0, data, 8);
			var ticks = status == 0 ? EndianBitConverter.Little.ToUInt64(data) : 0;
			rows.Add(new("Sleep / Wake", name, ticks == 0 ? "Not Reported" : ticks.ToString(CultureInfo.InvariantCulture) + " × 100 ns",
				"Windows Interrupt Time (Not Wall Clock)", status == 0 ? string.Empty : "NTSTATUS 0x" + status.ToString("X8")));
		}
		return rows.ToArray();
	}

	private static IEnumerable<Guid> Enumerate(Guid? scheme, Guid? subgroup, uint access, CancellationToken cancellationToken) {
		for (uint index = 0; ; index++) {
			cancellationToken.ThrowIfCancellationRequested();
			var bytes = new byte[16];
			uint size = 16;
			var status = WinAPI.POWRPROF.PowerEnumerate(IntPtr.Zero, Array(scheme), Array(subgroup), access, index, bytes, ref size);
			if (status == 259) yield break;
			Check(status);
			Guard.Ensure(size == 16, "Windows returned an invalid power identifier.");
			yield return new Guid(bytes);
		}
	}

	private static Dictionary<uint, string> ReadChoices(Guid[] subgroup, Guid[] setting, CancellationToken cancellationToken) {
		var result = new Dictionary<uint, string>();
		for (uint index = 0; index < 256; index++) {
			cancellationToken.ThrowIfCancellationRequested();
			var bytes = new byte[4];
			uint size = 4;
			if (WinAPI.POWRPROF.PowerReadPossibleValue(IntPtr.Zero, subgroup, setting, out var type, index, bytes, ref size) != 0 || type != 4 || size != 4)
				break;
			var label = ReadText((byte[] buffer, ref uint length) => WinAPI.POWRPROF.PowerReadPossibleFriendlyName(IntPtr.Zero, subgroup, setting, index, buffer, ref length));
			if (!string.IsNullOrWhiteSpace(label)) result[EndianBitConverter.Little.ToUInt32(bytes)] = label;
		}
		return result;
	}

	private static Guid[] Array(Guid? guid) => guid.HasValue ? new[] { guid.Value } : null;
	private static string Friendly(Guid scheme, Guid? subgroup, Guid? setting, string fallback) {
		var name = ReadText((byte[] buffer, ref uint size) => WinAPI.POWRPROF.PowerReadFriendlyName(IntPtr.Zero, Array(scheme), Array(subgroup), Array(setting), buffer, ref size));
		return string.IsNullOrWhiteSpace(name) ? fallback : name;
	}
	private static string Description(Guid scheme, Guid? subgroup, Guid? setting)
		=> ReadText((byte[] buffer, ref uint size) => WinAPI.POWRPROF.PowerReadDescription(IntPtr.Zero, Array(scheme), Array(subgroup), Array(setting), buffer, ref size));
	private static string ReadText(TextQuery query) {
		uint size = 0;
		var status = query(null, ref size);
		if (status is not (0 or 234) || size is 0 or > 65536) return string.Empty;
		for (var attempt = 0; attempt < 3; attempt++) {
			var buffer = new byte[size];
			status = query(buffer, ref size);
			if (status == 0) return Encoding.Unicode.GetString(buffer, 0, (int)Math.Min(size, buffer.Length)).TrimEnd('\0');
			if (status != 234 || size > 65536) break;
		}
		return string.Empty;
	}
	private static string ValueText(uint value, Dictionary<uint, string> choices)
		=> choices.TryGetValue(value, out var label) ? label + " (" + value.ToString(CultureInfo.InvariantCulture) + ")" : value.ToString(CultureInfo.InvariantCulture);
	private static string PolicyValue(object value) => value is int number ? unchecked((uint)number).ToString(CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture);
	private static string PowerState(uint state) => state switch { 0 => "Unspecified", 1 => "S0", 2 => "S1", 3 => "S2", 4 => "S3", 5 => "S4", 6 => "S5", _ => state.ToString(CultureInfo.InvariantCulture) };
	private static void Check(uint status) { if (status != 0) throw new Win32Exception((int)status); }
}
