// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;

namespace Sphere10.Framework.Windows;

/// <summary>A detached, read-only desktop target; empty bounds mean its process identity could not be verified.</summary>
public sealed record WindowsWindowTarget(WindowsWindowInfo Window, Rectangle Bounds);

public sealed record WindowsUiAutomationProperty(int Id, string Name, string Value, string Availability);

internal readonly record struct WindowsUiAutomationPropertyDescription(int Id, string Name, bool PatternSupport = false);
