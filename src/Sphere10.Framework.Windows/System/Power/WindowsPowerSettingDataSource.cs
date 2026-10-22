// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

public sealed class WindowsPowerSettingDataSource : ReloadableSnapshotDataSource<WindowsPowerSettingInfo> {
	public WindowsPowerSettingDataSource(Guid planId) : this(token => Tools.Windows.Computer.GetPowerSettingsAsync(planId, token)) { }
	public WindowsPowerSettingDataSource(Func<CancellationToken, Task<WindowsPowerSettingInfo[]>> capture) : base(capture) { }
}
