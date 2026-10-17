// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

public static class WindowsComputer {
	private const string MemoryModulesQuery = "SELECT DeviceLocator,BankLabel,Manufacturer,PartNumber,SerialNumber,Capacity,ConfiguredClockSpeed," +
		"SMBIOSMemoryType,FormFactor,DataWidth,TotalWidth FROM Win32_PhysicalMemory";
	private const string MemoryArraysQuery = "SELECT Tag,Location,Use,MemoryDevices,MaxCapacityEx,MaxCapacity FROM Win32_PhysicalMemoryArray";
	private const string EnclosuresQuery = "SELECT Manufacturer,Model,Version,SerialNumber,SMBIOSAssetTag,ChassisTypes FROM Win32_SystemEnclosure";
	private const string VideoControllersQuery = "SELECT DeviceID,Name,AdapterCompatibility,VideoProcessor,PNPDeviceID,DriverVersion,DriverDate,Status,AdapterRAM," +
		"CurrentHorizontalResolution,CurrentVerticalResolution,CurrentRefreshRate FROM Win32_VideoController";
	private const string DevicesQuery = "SELECT DeviceID,Name,Description,Manufacturer,PNPClass,ClassGuid,Service,Status,Present," +
		"ConfigManagerErrorCode,HardwareID,CompatibleID FROM Win32_PnPEntity WHERE Present=TRUE";
	private const string MonitorsQuery = "SELECT InstanceName,ManufacturerName,ProductCodeID,SerialNumberID,UserFriendlyName,Active," +
		"WeekOfManufacture,YearOfManufacture FROM WmiMonitorID";
	private const string PrintersQuery = "SELECT DeviceID,Name,DriverName,PortName,Status,Default,Network,Local,Shared,WorkOffline,PrinterStatus FROM Win32_Printer";

	public static Task<WindowsComputerInfo> QueryAsync(CancellationToken cancellationToken = default) => Task.Run(() => QueryCore(cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public static WindowsComputerInfo Query() => QueryCore(null);

	private static WindowsComputerInfo QueryCore(Action checkpoint) {
		checkpoint?.Invoke();
		using var search = new ManagementObjectSearcher("SELECT Name,Domain,PartOfDomain,Manufacturer,Model,TotalPhysicalMemory,SystemType,HypervisorPresent FROM Win32_ComputerSystem");
		search.Options.Timeout = TimeSpan.FromSeconds(5);
		using var results = search.Get();
		foreach (ManagementObject computer in results) {
			using (computer) {
				checkpoint?.Invoke();
				return new WindowsComputerInfo(Convert.ToString(computer["Name"]), Convert.ToString(computer["Domain"]), Convert.ToBoolean(computer["PartOfDomain"]),
					Convert.ToString(computer["Manufacturer"]), Convert.ToString(computer["Model"])) {
					TotalPhysicalMemory = computer["TotalPhysicalMemory"] == null ? null : Convert.ToUInt64(computer["TotalPhysicalMemory"]),
					SystemType = Convert.ToString(computer["SystemType"]),
					HypervisorPresent = computer["HypervisorPresent"] == null ? null : Convert.ToBoolean(computer["HypervisorPresent"])
				};
			}
		}
		throw new InvalidOperationException("Windows did not provide the computer identity.");
	}

	public static Task RenameAsync(string expectedCurrentName, string newName, CancellationToken cancellationToken = default)
		=> Task.Run(() => RenameCore(expectedCurrentName, newName, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public static void Rename(string expectedCurrentName, string newName) => RenameCore(expectedCurrentName, newName, null);

	private static void RenameCore(string expectedCurrentName, string newName, Action checkpoint) {
		Guard.ArgumentNotNullOrWhitespace(newName, nameof(newName));
		checkpoint?.Invoke();
		using var search = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
		search.Options.Timeout = TimeSpan.FromSeconds(5);
		using var results = search.Get();
		foreach (ManagementObject computer in results) {
			using (computer) {
				Guard.Ensure(expectedCurrentName == null || string.Equals(Convert.ToString(computer["Name"]), expectedCurrentName, StringComparison.OrdinalIgnoreCase), "The computer name changed.");
				using var input = computer.GetMethodParameters("Rename");
				input["Name"] = newName;
				checkpoint?.Invoke();
				using var output = computer.InvokeMethod("Rename", input, new InvokeMethodOptions { Timeout = TimeSpan.FromSeconds(15) });
				var code = Convert.ToUInt32(output?["ReturnValue"] ?? uint.MaxValue);
				if (code != 0) throw new Win32Exception(unchecked((int)code), $"Windows could not rename the computer (code {code}).");
				return;
			}
		}
		throw new InvalidOperationException("Windows did not provide the computer identity.");
	}

	public static WindowsSystemProductInfo[] GetSystemProducts() => ReadResources("SELECT Name,Vendor,Version,IdentifyingNumber,UUID FROM Win32_ComputerSystemProduct", item => new WindowsSystemProductInfo(Text(item, "Name"), Text(item, "Vendor"), Text(item, "Version"), Text(item, "IdentifyingNumber"), Text(item, "UUID")), null);

	public static Task<WindowsSystemProductInfo[]> GetSystemProductsAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadResources("SELECT Name,Vendor,Version,IdentifyingNumber,UUID FROM Win32_ComputerSystemProduct", item => new WindowsSystemProductInfo(Text(item, "Name"), Text(item, "Vendor"), Text(item, "Version"), Text(item, "IdentifyingNumber"), Text(item, "UUID")), cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public static WindowsBiosInfo[] GetBios() => ReadResources("SELECT Manufacturer,SMBIOSBIOSVersion,SerialNumber,ReleaseDate FROM Win32_BIOS", item => new WindowsBiosInfo(Text(item, "Manufacturer"), Text(item, "SMBIOSBIOSVersion"), Text(item, "SerialNumber"), Date(item, "ReleaseDate")), null);

	public static Task<WindowsBiosInfo[]> GetBiosAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadResources("SELECT Manufacturer,SMBIOSBIOSVersion,SerialNumber,ReleaseDate FROM Win32_BIOS", item => new WindowsBiosInfo(Text(item, "Manufacturer"), Text(item, "SMBIOSBIOSVersion"), Text(item, "SerialNumber"), Date(item, "ReleaseDate")), cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public static WindowsBaseboardInfo[] GetBaseboards() => ReadResources("SELECT Manufacturer,Product,Version,SerialNumber FROM Win32_BaseBoard", item => new WindowsBaseboardInfo(Text(item, "Manufacturer"), Text(item, "Product"), Text(item, "Version"), Text(item, "SerialNumber")), null);

	public static Task<WindowsBaseboardInfo[]> GetBaseboardsAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadResources("SELECT Manufacturer,Product,Version,SerialNumber FROM Win32_BaseBoard", item => new WindowsBaseboardInfo(Text(item, "Manufacturer"), Text(item, "Product"), Text(item, "Version"), Text(item, "SerialNumber")), cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public static WindowsProcessorInfo[] GetProcessors() => ReadResources("SELECT DeviceID,Name,Manufacturer,ProcessorId,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed FROM Win32_Processor", item => new WindowsProcessorInfo(Text(item, "DeviceID"), Text(item, "Name"), Text(item, "Manufacturer"), Text(item, "ProcessorId"), Number(item, "NumberOfCores"), Number(item, "NumberOfLogicalProcessors"), Number(item, "MaxClockSpeed")), null);

	public static Task<WindowsProcessorInfo[]> GetProcessorsAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadResources("SELECT DeviceID,Name,Manufacturer,ProcessorId,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed FROM Win32_Processor", item => new WindowsProcessorInfo(Text(item, "DeviceID"), Text(item, "Name"), Text(item, "Manufacturer"), Text(item, "ProcessorId"), Number(item, "NumberOfCores"), Number(item, "NumberOfLogicalProcessors"), Number(item, "MaxClockSpeed")), cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public static WindowsOperatingSystemInfo[] GetOperatingSystems() => ReadResources("SELECT Caption,Version,BuildNumber,OSArchitecture,SerialNumber,InstallDate,LastBootUpTime,WindowsDirectory,SystemDirectory FROM Win32_OperatingSystem", item => new WindowsOperatingSystemInfo(Text(item, "Caption"), Text(item, "Version"), Text(item, "BuildNumber"), Text(item, "OSArchitecture"), Text(item, "SerialNumber"), Date(item, "InstallDate"), Date(item, "LastBootUpTime"), Text(item, "WindowsDirectory"), Text(item, "SystemDirectory")), null);

	public static Task<WindowsOperatingSystemInfo[]> GetOperatingSystemsAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadResources("SELECT Caption,Version,BuildNumber,OSArchitecture,SerialNumber,InstallDate,LastBootUpTime,WindowsDirectory,SystemDirectory FROM Win32_OperatingSystem", item => new WindowsOperatingSystemInfo(Text(item, "Caption"), Text(item, "Version"), Text(item, "BuildNumber"), Text(item, "OSArchitecture"), Text(item, "SerialNumber"), Date(item, "InstallDate"), Date(item, "LastBootUpTime"), Text(item, "WindowsDirectory"), Text(item, "SystemDirectory")), cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public static WindowsMemoryModuleInfo[] GetMemoryModules() => ReadResources(MemoryModulesQuery, ReadMemoryModule, null);

	public static Task<WindowsMemoryModuleInfo[]> GetMemoryModulesAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadResources(MemoryModulesQuery, ReadMemoryModule, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public static WindowsMemoryArrayInfo[] GetMemoryArrays() => ReadResources(MemoryArraysQuery, ReadMemoryArray, null);

	public static Task<WindowsMemoryArrayInfo[]> GetMemoryArraysAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadResources(MemoryArraysQuery, ReadMemoryArray, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public static WindowsEnclosureInfo[] GetEnclosures() => ReadResources(EnclosuresQuery, ReadEnclosure, null);

	public static Task<WindowsEnclosureInfo[]> GetEnclosuresAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadResources(EnclosuresQuery, ReadEnclosure, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public static WindowsVideoControllerInfo[] GetVideoControllers() => ReadResources(VideoControllersQuery, ReadVideoController, null);

	public static Task<WindowsVideoControllerInfo[]> GetVideoControllersAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadResources(VideoControllersQuery, ReadVideoController, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	/// <summary>Lists present PnP entities, including physical, virtual and software devices. This does not open or change the devices.</summary>
	public static WindowsPlugAndPlayDeviceInfo[] GetDevices() => ReadResources(DevicesQuery, ReadDevice, null);

	public static Task<WindowsPlugAndPlayDeviceInfo[]> GetDevicesAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadResources(DevicesQuery, ReadDevice, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	public static WindowsMonitorInfo[] GetMonitors() => ReadResources(MonitorsQuery, ReadMonitor, null, @"\\.\root\wmi");

	public static Task<WindowsMonitorInfo[]> GetMonitorsAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadResources(MonitorsQuery, ReadMonitor, cancellationToken.ThrowIfCancellationRequested, @"\\.\root\wmi"), cancellationToken);

	public static WindowsPrinterInfo[] GetPrinters() => ReadResources(PrintersQuery, ReadPrinter, null);

	public static Task<WindowsPrinterInfo[]> GetPrintersAsync(CancellationToken cancellationToken = default)
		=> Task.Run(() => ReadResources(PrintersQuery, ReadPrinter, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	private static WindowsMemoryModuleInfo ReadMemoryModule(ManagementObject resource) => new() {
		DeviceLocator = Text(resource, "DeviceLocator"),
		BankLabel = Text(resource, "BankLabel"),
		Manufacturer = Text(resource, "Manufacturer"),
		PartNumber = Text(resource, "PartNumber"),
		SerialNumber = Text(resource, "SerialNumber"),
		CapacityBytes = LargeNumber(resource, "Capacity"),
		ConfiguredClockMHz = Number(resource, "ConfiguredClockSpeed"),
		SMBIOSMemoryType = Number(resource, "SMBIOSMemoryType"),
		FormFactor = Number(resource, "FormFactor"),
		DataWidthBits = Number(resource, "DataWidth"),
		TotalWidthBits = Number(resource, "TotalWidth")
	};

	private static WindowsMemoryArrayInfo ReadMemoryArray(ManagementObject resource) => new() {
		Tag = Text(resource, "Tag"),
		Location = Number(resource, "Location"),
		Use = Number(resource, "Use"),
		MemoryDevices = Number(resource, "MemoryDevices"),
		MaximumCapacityBytes = ConvertMemoryCapacity(LargeNumber(resource, "MaxCapacityEx"), Number(resource, "MaxCapacity"))
	};

	private static WindowsEnclosureInfo ReadEnclosure(ManagementObject resource) => new() {
		Manufacturer = Text(resource, "Manufacturer"),
		Model = Text(resource, "Model"),
		Version = Text(resource, "Version"),
		SerialNumber = Text(resource, "SerialNumber"),
		AssetTag = Text(resource, "SMBIOSAssetTag"),
		ChassisTypes = resource["ChassisTypes"] as ushort[] ?? Array.Empty<ushort>()
	};

	private static WindowsVideoControllerInfo ReadVideoController(ManagementObject resource) => new() {
		DeviceId = Text(resource, "DeviceID"),
		Name = Text(resource, "Name"),
		Manufacturer = Text(resource, "AdapterCompatibility"),
		VideoProcessor = Text(resource, "VideoProcessor"),
		PnpDeviceId = Text(resource, "PNPDeviceID"),
		DriverVersion = Text(resource, "DriverVersion"),
		DriverDate = Date(resource, "DriverDate"),
		Status = Text(resource, "Status"),
		ReportedMemoryBytes = Number(resource, "AdapterRAM"),
		HorizontalResolution = Number(resource, "CurrentHorizontalResolution"),
		VerticalResolution = Number(resource, "CurrentVerticalResolution"),
		RefreshRateHz = Number(resource, "CurrentRefreshRate")
	};

	private static WindowsPlugAndPlayDeviceInfo ReadDevice(ManagementObject resource) => new() {
		DeviceId = Text(resource, "DeviceID"),
		Name = Text(resource, "Name"),
		Description = Text(resource, "Description"),
		Manufacturer = Text(resource, "Manufacturer"),
		DeviceClass = Text(resource, "PNPClass"),
		ClassGuid = Text(resource, "ClassGuid"),
		Service = Text(resource, "Service"),
		Status = Text(resource, "Status"),
		Present = Boolean(resource, "Present"),
		ProblemCode = Number(resource, "ConfigManagerErrorCode"),
		HardwareIds = resource["HardwareID"] as string[] ?? Array.Empty<string>(),
		CompatibleIds = resource["CompatibleID"] as string[] ?? Array.Empty<string>()
	};

	private static WindowsMonitorInfo ReadMonitor(ManagementObject resource) => new() {
		InstanceName = Text(resource, "InstanceName"),
		Manufacturer = DecodeMonitorText(resource["ManufacturerName"] as ushort[]),
		ProductCode = DecodeMonitorText(resource["ProductCodeID"] as ushort[]),
		SerialNumber = DecodeMonitorText(resource["SerialNumberID"] as ushort[]),
		Name = DecodeMonitorText(resource["UserFriendlyName"] as ushort[]),
		Active = Boolean(resource, "Active"),
		ManufactureWeek = SmallNumber(resource, "WeekOfManufacture"),
		ManufactureYear = SmallNumber(resource, "YearOfManufacture")
	};

	private static WindowsPrinterInfo ReadPrinter(ManagementObject resource) => new() {
		DeviceId = Text(resource, "DeviceID"),
		Name = Text(resource, "Name"),
		DriverName = Text(resource, "DriverName"),
		PortName = Text(resource, "PortName"),
		Status = Text(resource, "Status"),
		IsDefault = Boolean(resource, "Default"),
		IsNetwork = Boolean(resource, "Network"),
		IsLocal = Boolean(resource, "Local"),
		IsShared = Boolean(resource, "Shared"),
		IsOffline = Boolean(resource, "WorkOffline"),
		PrinterStatus = Number(resource, "PrinterStatus")
	};

	private static T[] ReadResources<T>(string query, Func<ManagementObject, T> convert, Action checkpoint, string scope = @"\\.\root\cimv2") {
		checkpoint?.Invoke();
		var timeout = TimeSpan.FromSeconds(5);
		using var searcher = new ManagementObjectSearcher(new ManagementScope(scope, new ConnectionOptions { Timeout = timeout }),
			new ObjectQuery(query), new EnumerationOptions { Timeout = timeout, Rewindable = false, ReturnImmediately = true, BlockSize = 32 });
		using var resources = searcher.Get();
		var result = new List<T>();
		foreach (ManagementObject resource in resources) {
			using (resource) {
				checkpoint?.Invoke();
				result.Add(convert(resource));
			}
		}
		checkpoint?.Invoke();
		return result.ToArray();
	}

	private static string Text(ManagementObject resource, string property) => Convert.ToString(resource[property], CultureInfo.InvariantCulture);

	private static uint? Number(ManagementObject resource, string property) => resource[property] == null ? null : Convert.ToUInt32(resource[property], CultureInfo.InvariantCulture);

	private static ulong? LargeNumber(ManagementObject resource, string property) => resource[property] == null ? null : Convert.ToUInt64(resource[property], CultureInfo.InvariantCulture);

	private static ushort? SmallNumber(ManagementObject resource, string property) => resource[property] == null ? null : Convert.ToUInt16(resource[property], CultureInfo.InvariantCulture);

	private static bool? Boolean(ManagementObject resource, string property) => resource[property] == null ? null : Convert.ToBoolean(resource[property], CultureInfo.InvariantCulture);

	private static DateTime? Date(ManagementObject resource, string property) => ParseDmtfDate(Text(resource, property));

	internal static ulong? ConvertMemoryCapacity(ulong? extendedKilobytes, uint? legacyKilobytes) {
		// Both provider properties use kilobytes. Zero means unknown; avoid overflowing bytes for a malformed provider value.
		var kilobytes = extendedKilobytes.GetValueOrDefault() > 0 ? extendedKilobytes : legacyKilobytes;
		return kilobytes.GetValueOrDefault() > 0 && kilobytes <= ulong.MaxValue / 1024 ? kilobytes * 1024 : null;
	}

	internal static string DecodeMonitorText(ushort[] characters) {
		if (characters == null || characters.Length == 0)
			return string.Empty;
		// EDID-backed monitor identifiers are ASCII code units, terminated by zero. Bound even a malformed provider array.
		var buffer = new char[Math.Min(characters.Length, 256)];
		var length = 0;
		while (length < buffer.Length && characters[length] != 0) {
			buffer[length] = characters[length] <= 127 ? (char)characters[length] : '\uFFFD';
			length++;
		}
		return new string(buffer, 0, length);
	}

	internal static DateTime? ParseDmtfDate(string text) {
		if (string.IsNullOrEmpty(text) || text.Contains('*'))
			return null;
		try {
			return ManagementDateTimeConverter.ToDateTime(text).ToUniversalTime();
		} catch (ArgumentException) {
			return null;
		} catch (FormatException) {
			return null;
		}
	}
}
