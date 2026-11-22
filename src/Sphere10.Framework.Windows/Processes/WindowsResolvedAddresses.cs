// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Detached address resolution, with explicit missing/mismatched image issues. PE identity matching is not a cryptographic authenticity check.</summary>
public sealed record WindowsResolvedAddresses(DateTime ResolvedAtUtc, WindowsStackFrame[] Frames, string[] Issues);
