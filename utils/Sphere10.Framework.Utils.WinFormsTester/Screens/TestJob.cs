// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>The original bounded synchronous job used by scheduler serialization examples.</summary>
public class TestJob : ISchedulerJob {
	public void Execute(IJob job) {
		job.Log.Debug("TestJob BEGIN");
		System.Threading.Thread.Sleep(500);
		job.Log.Debug("TestJob END");
	}
}



