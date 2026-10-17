// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>Job membership is available for other processes; null Accounting means no job handle is available, not zero accounting.</summary>
public sealed record WindowsProcessJobInfo(bool IsInJob, WindowsJobAccountingInfo Accounting);

