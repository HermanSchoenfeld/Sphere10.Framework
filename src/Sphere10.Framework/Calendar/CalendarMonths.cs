// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;

namespace Sphere10.Framework;

/// <summary>A set of months.</summary>
[Flags]
public enum CalendarMonths {
	None = 0,
	January = 1,
	February = 2,
	March = 4,
	April = 8,
	May = 16,
	June = 32,
	July = 64,
	August = 128,
	September = 256,
	October = 512,
	November = 1024,
	December = 2048,
	EveryMonth = 4095
}
