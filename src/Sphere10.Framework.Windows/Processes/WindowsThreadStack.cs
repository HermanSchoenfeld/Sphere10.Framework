// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>A detached, bounded user-mode stack captured from one verified native thread instance.</summary>
public sealed record WindowsThreadStack(ThreadIdentity Identity, DateTime CapturedAtUtc, WindowsStackFrame[] Frames, bool Truncated, string[] Issues);
