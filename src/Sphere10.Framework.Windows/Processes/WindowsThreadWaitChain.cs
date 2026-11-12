// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>A point-in-time WCT sample. Unsupported synchronization objects are not evidence of no wait or deadlock.</summary>
public sealed record WindowsThreadWaitChain(ThreadIdentity Identity, DateTime CapturedAtUtc, WindowsThreadWaitChainNode[] Nodes, bool CycleDetected, bool Truncated);

