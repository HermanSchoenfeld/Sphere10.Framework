// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

public static class Sphere10FrameworkExtensions {

	public static Sphere10FrameworkBuilder BuildWinFormsApplication(this Sphere10Framework framework) {
		return framework.Build();
	}

	public static Sphere10FrameworkBuilder UseMainForm<TMainForm>(this Sphere10FrameworkBuilder builder)
		where TMainForm : class, IMainForm {
		builder.ConfigureServices(services => services.AddMainForm<TMainForm>());
		return builder;
	}

	public static void StartWinFormsApplication(this Sphere10FrameworkBuilder builder, Size? size = null) {
		builder.Start();
		var Framework = Sphere10Framework.Instance;
		var MainForm = Framework.ServiceProvider.GetService<IMainForm>();
		if (MainForm is not Form)
			throw new SoftwareException("Registered IMainForm is not a WinForms Form");
		if (MainForm is IBlockManager BlockManager) {
			var Blocks = Framework.ServiceProvider.GetServices<IApplicationBlock>().OrderBy(b => b.Position);
			Blocks.ForEach(BlockManager.RegisterBlock);
		}
		if (size != null)
			((Form)MainForm).Size = size.Value;
		System.Windows.Forms.Application.Run(MainForm as Form);
	}
}

