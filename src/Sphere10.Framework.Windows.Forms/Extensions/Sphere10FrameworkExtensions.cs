// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

public static class Sphere10FrameworkExtensions {

	public static void StartWinFormsApplication<TMainForm>(this Sphere10Framework framework, Size? size = null, Sphere10FrameworkOptions options = Sphere10FrameworkOptions.Default)
		where TMainForm : class, IMainForm {
		Sphere10Framework.Instance.StartFramework(serviceCollection => serviceCollection.AddMainForm<TMainForm>(), options);
		framework.StartWinFormsApplication(size);
	}

	public static void StartWinFormsApplication(this Sphere10Framework framework, Size? size = null) {


		if (!framework.IsStarted)
			framework.StartFramework();
		var mainForm = Sphere10Framework.Instance.ServiceProvider.GetService<IMainForm>();
		if (!(mainForm is Form)) {
			throw new SoftwareException("Registered IMainForm is not a WinForms Form");
		}
		if (mainForm is IBlockManager) {
			var blockManager = mainForm as IBlockManager;
			var blocks = Sphere10Framework.Instance.ServiceProvider.GetServices<IApplicationBlock>().OrderBy(b => b.Position);
			blocks.ForEach(blockManager.RegisterBlock);
		}

		if (size != null)
			((Form)mainForm).Size = size.Value;

		System.Windows.Forms.Application.Run(mainForm as Form);
	}

}

