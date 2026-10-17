// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsComputerInfo(string Name, string Domain, bool PartOfDomain, string Manufacturer, string Model) {
	public ulong? TotalPhysicalMemory { get; init; }
	public string SystemType { get; init; }
	public bool? HypervisorPresent { get; init; }
}

