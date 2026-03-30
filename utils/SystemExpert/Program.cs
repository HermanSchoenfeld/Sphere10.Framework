using System;
using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Application;

namespace SystemExpert;

static class Program {

	[STAThread]
	static void Main(string[] args) {
		System.Windows.Forms.Application.EnableVisualStyles();
		System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
		AppDomain.CurrentDomain.UnhandledException += (s, e) => Tools.Lambda.ActionIgnoringExceptions(() => ExceptionDialog.Show("Error", (Exception)e.ExceptionObject)).Invoke();
		System.Windows.Forms.Application.ThreadException += (xs, xe) => Tools.Lambda.ActionIgnoringExceptions(() => ExceptionDialog.Show("Error", xe.Exception)).Invoke();
		SystemLog.RegisterLogger(new ConsoleLogger());

		Sphere10Framework.Instance
			.BuildWinFormsApplication()
			.UseMainForm<BlockMainForm>()
			.UseModule<ModuleConfiguration>()
			.UseModule<Sphere10.Framework.Application.ModuleConfiguration>()
			.UseModule<Sphere10.Framework.Windows.Forms.ModuleConfiguration>()
			.StartWinFormsApplication();

		
	}
}
