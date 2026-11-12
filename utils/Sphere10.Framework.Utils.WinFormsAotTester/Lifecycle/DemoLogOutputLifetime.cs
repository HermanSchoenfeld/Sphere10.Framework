// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Lifecycle;

/// <summary>Registers demo output with the shared accepted-write lifetime.</summary>
public sealed class DemoLogOutputLifetime : LogOutputLifetime {
	public new static IDisposable RegisterCurrent(Control owner, Func<Task> closeOutput) =>
		(Sphere10Framework.Instance.IsStarted ? Sphere10Framework.Instance.ServiceProvider.GetService(typeof(DemoLogOutputLifetime)) as DemoLogOutputLifetime : null)
			?.Register(owner, closeOutput);

	public new static IDisposable RegisterCurrent(Control owner, params ChartLogOutputBehavior[] outputs) => RegisterCurrent(owner, () => {
		foreach (var output in outputs)
			output.Dispose();
		return Task.WhenAll(outputs.Select(output => output.Completion));
	});
}
