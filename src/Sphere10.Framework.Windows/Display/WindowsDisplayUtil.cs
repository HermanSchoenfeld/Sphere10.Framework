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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Native = Sphere10.Framework.Windows.WinAPI.USER32;

namespace Sphere10.Framework.Windows;

/// <summary>Read-only current display paths. Async entry points dispatch all CCD, registry and WMI work to a worker.</summary>
public class WindowsDisplayUtil {
	public virtual Task<WindowsDisplayInfo[]> GetDisplaysAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => GetDisplaysCore(cancellationToken), cancellationToken);

	public virtual WindowsDisplayInfo[] GetDisplays() => GetDisplaysCore(CancellationToken.None);

	private static WindowsDisplayInfo[] GetDisplaysCore(CancellationToken cancellationToken) {
		Debug.AssertNotMainThread();
		cancellationToken.ThrowIfCancellationRequested();
		const uint activePaths = 2;
		Native.DISPLAYCONFIG_PATH_INFO[] paths = null;
		Native.DISPLAYCONFIG_MODE_INFO[] modes = null;
		uint pathCount = 0;
		var captured = false;
		for (var attempt = 0; attempt < 5; attempt++) {
			cancellationToken.ThrowIfCancellationRequested();
			Check(Native.GetDisplayConfigBufferSizes(activePaths, out pathCount, out var modeCount), "Size display paths");
			Guard.Ensure(pathCount <= 4096 && modeCount <= 16384, "Windows returned an excessive display configuration.");
			paths = new Native.DISPLAYCONFIG_PATH_INFO[pathCount];
			modes = new Native.DISPLAYCONFIG_MODE_INFO[modeCount];
			var result = Native.QueryDisplayConfig(activePaths, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
			if (result == 122)
				continue; // Hotplug changed the topology between sizing and querying.
			Check(result, "Read display paths");
			Array.Resize(ref modes, checked((int)modeCount));
			Array.Resize(ref paths, checked((int)pathCount));
			captured = true;
			break;
		}
		Guard.Ensure(captured, "Display topology kept changing; refresh again.");
		var sharedAvailability = new List<string>();
		var identities = Array.Empty<WindowsMonitorInfo>();
		var adapters = Array.Empty<WindowsVideoControllerInfo>();
		CaptureOptional(() => identities = WindowsComputer.GetMonitors(), "Monitor identity", sharedAvailability);
		cancellationToken.ThrowIfCancellationRequested();
		CaptureOptional(() => adapters = WindowsComputer.GetVideoControllers(), "Video driver", sharedAvailability);
		var displays = new List<WindowsDisplayInfo>();
		foreach (var path in paths) {
			cancellationToken.ThrowIfCancellationRequested();
			var availability = new List<string>(sharedAvailability);
			var target = new Native.DISPLAYCONFIG_TARGET_DEVICE_NAME { Header = Header<Native.DISPLAYCONFIG_TARGET_DEVICE_NAME>(2, path.Target.AdapterId, path.Target.Id) };
			Record(Native.DisplayConfigGetDeviceInfo(ref target), "Monitor name/path", availability);
			var source = new Native.DISPLAYCONFIG_SOURCE_DEVICE_NAME { Header = Header<Native.DISPLAYCONFIG_SOURCE_DEVICE_NAME>(1, path.Source.AdapterId, path.Source.Id) };
			Record(Native.DisplayConfigGetDeviceInfo(ref source), "Desktop source", availability);
			var adapter = new Native.DISPLAYCONFIG_ADAPTER_NAME { Header = Header<Native.DISPLAYCONFIG_ADAPTER_NAME>(4, path.Source.AdapterId, 0) };
			Record(Native.DisplayConfigGetDeviceInfo(ref adapter), "Adapter path", availability);
			var preferred = new Native.DISPLAYCONFIG_TARGET_PREFERRED_MODE { Header = Header<Native.DISPLAYCONFIG_TARGET_PREFERRED_MODE>(3, path.Target.AdapterId, path.Target.Id) };
			var hasPreferred = Record(Native.DisplayConfigGetDeviceInfo(ref preferred), "Preferred timing", availability);
			var color = new Native.DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO { Header = Header<Native.DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>(9, path.Target.AdapterId, path.Target.Id) };
			var hasColor = Record(Native.DisplayConfigGetDeviceInfo(ref color), "Advanced color", availability);
			var color2 = new Native.DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2 { Header = Header<Native.DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2>(15, path.Target.AdapterId, path.Target.Id) };
			var hasColor2 = Native.DisplayConfigGetDeviceInfo(ref color2) == 0;
			var white = new Native.DISPLAYCONFIG_SDR_WHITE_LEVEL { Header = Header<Native.DISPLAYCONFIG_SDR_WHITE_LEVEL>(11, path.Target.AdapterId, path.Target.Id) };
			var hasWhite = Native.DisplayConfigGetDeviceInfo(ref white) == 0;
			var monitorId = DeviceInstanceId(target.DevicePath);
			var adapterId = DeviceInstanceId(adapter.DevicePath);
			var identity = identities.FirstOrDefault(item => item.InstanceName.Equals(monitorId, StringComparison.OrdinalIgnoreCase)
				|| item.InstanceName.StartsWith(monitorId + "_", StringComparison.OrdinalIgnoreCase));
			var controller = string.IsNullOrEmpty(adapterId) ? null : adapters.FirstOrDefault(item => string.Equals(item.PnpDeviceId, adapterId, StringComparison.OrdinalIgnoreCase));
			WindowsMonitorEdid edid = null;
			CaptureOptional(() => edid = ReadEdid(monitorId), "Cached EDID", availability);
			var sourceMode = Mode(modes, path.Source.ModeInfoIndex, 1, path.Source.Id, path.Source.AdapterId);
			var targetMode = Mode(modes, path.Target.ModeInfoIndex, 2, path.Target.Id, path.Target.AdapterId);
			var current = NewDevMode();
			var hasCurrent = !string.IsNullOrEmpty(source.Name) && Native.EnumDisplaySettingsExW(source.Name, -1, ref current, 0);
			var supported = new List<WindowsDisplayMode>();
			if (!string.IsNullOrEmpty(source.Name))
				for (var index = 0; index < 8192; index++) {
					cancellationToken.ThrowIfCancellationRequested();
					var mode = NewDevMode();
					if (!Native.EnumDisplaySettingsExW(source.Name, index, ref mode, 0))
						break;
					supported.Add(new WindowsDisplayMode(mode.Width, mode.Height, mode.BitsPerPixel, mode.Frequency, (mode.DisplayFlags & 2) != 0));
				}
			displays.Add(new WindowsDisplayInfo {
				Id = $"{path.Target.AdapterId.HighPart:X8}{path.Target.AdapterId.LowPart:X8}:{path.Target.Id}",
				Name = string.IsNullOrWhiteSpace(target.Name) ? identity?.Name ?? source.Name ?? "Unnamed Display" : target.Name,
				GdiDeviceName = source.Name ?? string.Empty, MonitorDevicePath = target.DevicePath ?? string.Empty, AdapterDevicePath = adapter.DevicePath ?? string.Empty,
				AdapterName = controller?.Name ?? string.Empty, DriverVersion = controller?.DriverVersion ?? string.Empty, DriverDate = controller?.DriverDate,
				Connector = Connector(path.Target.OutputTechnology), ConnectorInstance = target.ConnectorInstance, SourceId = path.Source.Id, TargetId = path.Target.Id,
				PathFlags = path.Flags, SourceStatusFlags = path.Source.StatusFlags, TargetStatusFlags = path.Target.StatusFlags,
				Rotation = path.Target.Rotation switch { 1 => "0°", 2 => "90°", 3 => "180°", 4 => "270°", _ => $"Unknown ({path.Target.Rotation})" },
				Scaling = path.Target.Scaling switch { 1 => "Identity", 2 => "Centered", 3 => "Stretched", 4 => "Aspect Ratio", 5 => "Custom", 128 => "Preferred", _ => $"Unknown ({path.Target.Scaling})" },
				DesktopX = sourceMode?.Source.X, DesktopY = sourceMode?.Source.Y, DesktopWidth = sourceMode?.Source.Width, DesktopHeight = sourceMode?.Source.Height,
				DesktopBitsPerPixel = hasCurrent ? current.BitsPerPixel : null, LogicalDpi = hasCurrent && current.LogPixels > 0 ? current.LogPixels : null,
				ActiveTiming = targetMode.HasValue ? Timing(targetMode.Value.Signal) : null, PreferredTiming = hasPreferred ? Timing(preferred.Signal) : null,
				AdvancedColorSupported = hasColor ? (color.Flags & 1) != 0 : null, AdvancedColorEnabled = hasColor ? (color.Flags & 2) != 0 : null,
				WideColorEnforced = hasColor ? (color.Flags & 4) != 0 : null, AdvancedColorForceDisabled = hasColor ? (color.Flags & 8) != 0 : null,
				HdrSupported = hasColor2 ? (color2.Flags & 16) != 0 : null, HdrUserEnabled = hasColor2 ? (color2.Flags & 32) != 0 : null,
				WideColorSupported = hasColor2 ? (color2.Flags & 64) != 0 : null, AdvancedColorLimitedByPolicy = hasColor2 ? (color2.Flags & 8) != 0 : null,
				ActiveColorMode = hasColor2 ? color2.ActiveColorMode switch { 0 => "SDR", 1 => "Wide Color Gamut", 2 => "HDR", _ => $"Unknown ({color2.ActiveColorMode})" } : string.Empty,
				ColorEncoding = hasColor ? color.ColorEncoding switch { 0 => "RGB", 1 => "YCbCr 4:4:4", 2 => "YCbCr 4:2:2", 3 => "YCbCr 4:2:0", 4 => "Intensity", _ => $"Unknown ({color.ColorEncoding})" } : string.Empty,
				BitsPerColorChannel = hasColor && color.BitsPerColorChannel > 0 ? color.BitsPerColorChannel : null,
				SdrWhiteLevelNits = hasWhite && white.WhiteLevel > 0 ? white.WhiteLevel * 80.0 / 1000.0 : null,
				Identity = identity, Edid = edid, Modes = supported.Distinct().ToArray(), Availability = availability.ToArray()
			});
		}
		return displays.ToArray();
	}

	private static Native.DISPLAYCONFIG_DEVICE_INFO_HEADER Header<T>(uint type, WinAPI.ADVAPI32.LUID adapter, uint id)
		where T : struct => new() { Type = type, Size = (uint)Marshal.SizeOf<T>(), AdapterId = adapter, Id = id };

	private static Native.DISPLAY_DEVMODE NewDevMode() => new() { Size = (ushort)Marshal.SizeOf<Native.DISPLAY_DEVMODE>() };

	private static Native.DISPLAYCONFIG_MODE_INFO? Mode(Native.DISPLAYCONFIG_MODE_INFO[] modes, uint index, uint type, uint id, WinAPI.ADVAPI32.LUID adapter) {
		if (index >= modes.Length)
			return null;
		var mode = modes[index];
		return mode.InfoType == type && mode.Id == id && mode.AdapterId.LowPart == adapter.LowPart && mode.AdapterId.HighPart == adapter.HighPart ? mode : null;
	}

	private static WindowsDisplayTiming Timing(Native.DISPLAYCONFIG_VIDEO_SIGNAL_INFO signal) => new() {
		ActiveWidth = signal.ActiveWidth, ActiveHeight = signal.ActiveHeight, TotalWidth = signal.TotalWidth, TotalHeight = signal.TotalHeight,
		PixelClockHz = signal.PixelRate, HorizontalSyncHz = Rational(signal.HorizontalSync), VerticalSyncHz = Rational(signal.VerticalSync),
		ScanLineOrdering = signal.ScanLineOrdering switch { 1 => "Progressive", 2 => "Interlaced Upper First", 3 => "Interlaced Lower First", _ => "Unspecified" }
	};

	private static double? Rational(Native.DISPLAYCONFIG_RATIONAL value) => value.Denominator == 0 ? null : value.Numerator / (double)value.Denominator;

	private static string Connector(int technology) => technology switch {
		-1 => "Other", 0 => "VGA / HD15", 1 => "S-Video", 2 => "Composite", 3 => "Component", 4 => "DVI", 5 => "HDMI", 6 => "LVDS",
		8 => "D-JPN", 9 => "SDI", 10 => "DisplayPort", 11 => "Embedded DisplayPort", 12 => "UDI", 13 => "Embedded UDI", 14 => "SDTV Dongle",
		15 => "Miracast", 16 => "Indirect Wired", 17 => "Indirect Virtual", 18 => "DisplayPort USB Tunnel", int.MinValue => "Internal", _ => $"Unknown ({technology})"
	};

	private static string DeviceInstanceId(string devicePath) {
		if (string.IsNullOrEmpty(devicePath) || !devicePath.StartsWith(@"\\?\", StringComparison.Ordinal))
			return string.Empty;
		var parts = devicePath[4..].Split('#');
		return parts.Length >= 3 && parts.Take(3).All(part => part.Length > 0 && !part.Contains('\\')) ? string.Join("\\", parts.Take(3)) : string.Empty;
	}

	private static WindowsMonitorEdid ReadEdid(string deviceId) {
		if (string.IsNullOrEmpty(deviceId))
			return null;
		using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\" + deviceId + @"\Device Parameters");
		if (key?.GetValue("EDID") is not byte[] bytes)
			return null;
		using var overrides = key.OpenSubKey("EDID_OVERRIDE");
		return WindowsMonitorEdid.Parse(bytes, overrides != null && overrides.ValueCount > 0);
	}

	private static void CaptureOptional(Action capture, string name, List<string> availability) {
		Tools.Exceptions.ExecuteCapturingException(capture, out var error, exception => exception is not OperationCanceledException);
		if (error != null)
			availability.Add($"{name}: {error.Message}");
	}

	private static bool Record(int code, string name, List<string> availability) {
		if (code == 0)
			return true;
		availability.Add($"{name}: {new Win32Exception(code).Message} ({code})");
		return false;
	}

	private static void Check(int code, string operation) {
		if (code != 0)
			throw new Win32Exception(code, $"{operation}: {new Win32Exception(code).Message}");
	}
}
