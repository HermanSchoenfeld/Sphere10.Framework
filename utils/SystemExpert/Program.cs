using Sphere10.Framework.Application.UI;
using System;
using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Application;

namespace SystemExpert;

static class Program {

	[STAThread]
	static void Main(string[] args) {
		// Preserve the designer layouts and let Windows bitmap-scale each complete window.
		System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.DpiUnaware);
		System.Windows.Forms.Application.EnableVisualStyles();
		System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
		AppDomain.CurrentDomain.UnhandledException += async (Sender, Args) => {
			try {
				await ExceptionDialog.ShowAsync(null, "Error", (Exception)Args.ExceptionObject);
			} catch {
				// Avoid throwing again from the last-chance exception handler.
			}
		};
		System.Windows.Forms.Application.ThreadException += async (Sender, Args) => {
			try {
				await ExceptionDialog.ShowAsync(null, "Error", Args.Exception);
			} catch {
				// Avoid re-entering the UI exception handler if displaying the error fails.
			}
		};
		SystemLog.RegisterLogger(new ConsoleLogger());

		Sphere10Framework.Instance
			.BuildWinFormsApplication()
			.UseMainForm<BlockMainForm>(Form => Form.ScreenMode = ScreenMode.MultiView)
			.UseModule<ModuleConfiguration>()
			.UseModule<Sphere10.Framework.Application.ModuleConfiguration>()
			.UseModule<Sphere10.Framework.Windows.Forms.ModuleConfiguration>()
			.StartWinFormsApplication();

		
	}
}
