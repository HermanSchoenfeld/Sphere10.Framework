// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Startup;

[Flags]
public enum WindowsStartupSources { None = 0, LogonRegistry = 1, ScheduledTasks = 2, WmiSubscriptions = 4, All = 7 }

/// <summary>An explicit target interpretation, independent of the unchanged raw registration.</summary>
public sealed record WindowsStartupTarget(string Path, string Arguments, string WorkingDirectory, string Availability);
