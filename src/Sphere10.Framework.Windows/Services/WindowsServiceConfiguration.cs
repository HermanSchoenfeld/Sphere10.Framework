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

/// <summary>The configuration returned by QueryServiceConfig.</summary>
public sealed record WindowsServiceConfiguration {
	public string Name { get; init; }
	public string DisplayName { get; init; }
	public string BinaryPath { get; init; }
	public string Account { get; init; }
	public ServiceType ServiceType { get; init; }
	public ServiceStartMode StartMode { get; init; }
	public uint ErrorControl { get; init; }
	public string LoadOrderGroup { get; init; }
	public uint TagId { get; init; }
	public string[] Dependencies { get; init; } = Array.Empty<string>();
}
