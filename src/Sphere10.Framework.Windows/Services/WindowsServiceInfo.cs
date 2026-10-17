// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ServiceProcess;

namespace Sphere10.Framework.Windows.Services;

/// <summary>Native service configuration and current status; error codes distinguish unavailable fields from empty values.</summary>
public sealed record WindowsServiceInfo {
	public string Name { get; init; }
	public string DisplayName { get; init; }
	public string Description { get; init; }
	public string BinaryPath { get; init; }
	public string Account { get; init; }
	public ServiceType ServiceType { get; init; }
	public ServiceStartMode? StartMode { get; init; }
	public ServiceControllerStatus? Status { get; init; }
	public uint? ProcessId { get; init; }
	public uint? Win32ExitCode { get; init; }
	public bool? CanStop { get; init; }
	public bool? CanPause { get; init; }
	public string[] Dependencies { get; init; } = Array.Empty<string>();
	public string[] DependentServices { get; init; } = Array.Empty<string>();
	public int ConfigurationErrorCode { get; init; }
	public int StatusErrorCode { get; init; }
	public int DescriptionErrorCode { get; init; }
	public int DependencyErrorCode { get; init; }
}
