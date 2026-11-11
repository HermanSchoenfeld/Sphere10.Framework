// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

internal static partial class AotSmoke {
	private static string _directory;
	private static string _case = "all";
	private static Timer _timer;
	private static Stopwatch _elapsed;
	private static bool _ready;
	private static bool _mainReadyRecorded;
	private static bool _screensVerified;
	private static bool _finalized;

	public static bool IsActive => _directory != null;
	public static string EvidenceDirectory => _directory;

	public static void Configure(string[] args) {
		var argument = Array.IndexOf(args, "--aot-smoke");
		if (argument < 0)
			return;
		var directory = args.ElementAtOrDefault(argument + 1);
		Guard.ArgumentNotNullOrEmpty(directory, nameof(args));
		_directory = Path.GetFullPath(directory);
		Directory.CreateDirectory(_directory);
		File.WriteAllText(Path.Combine(_directory, "smoke.log"), string.Empty);
		_elapsed = Stopwatch.StartNew();
		var caseArgument = Array.IndexOf(args, "--aot-case");
		_case = caseArgument < 0 ? "all" : args.ElementAtOrDefault(caseArgument + 1);
		Guard.Argument(new[] { "all", "navigation", "enums", "appointment", "visualgrid", "options", "sourcegrid", "charting", "objectspace", "crudeditor" }.Contains(_case), nameof(args), "Unknown AOT smoke case.");
		Record("Starting case=" + _case);
		ConfigureObjectSpaceFixtures(args);
		Sphere10Framework.Instance.Finalized += () => { _finalized = true; Record("FrameworkFinalized"); };
		_timer = new Timer { Interval = 100 };
		_timer.Tick += Observe;
		_timer.Start();
	}

	public static void ConfigureServices(IServiceCollection services) {
		if (!IsActive)
			return;
		// The diagnostic run owns temporary settings rather than the tester's interactive preferences.
		services.AddSingleton(new Local<ISettingsProvider>(new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "local")))));
		services.AddSingleton(new Global<ISettingsProvider>(new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "global")))));
	}

	public static void Complete() {
		if (!IsActive)
			return;
		_timer?.Dispose();
		if (!_mainReadyRecorded || !_screensVerified || !_finalized)
			Fail(new InvalidOperationException("Smoke did not verify readiness, selected screen cases and framework finalization."));
		Record("RunnerReturned");
	}

	public static void Fail(Exception error) {
		if (!IsActive)
			throw error;
		Record("Failed: " + error);
		Environment.Exit(1);
	}

	private static async void Observe(object sender, EventArgs args) {
		try {
			var main = System.Windows.Forms.Application.OpenForms.OfType<BlockMainForm>().FirstOrDefault(form => form.Visible && form.IsHandleCreated);
			if (main == null)
				return;
			if (!_ready) {
				_ready = true;
				_timer.Interval = 800;
				return;
			}
			_timer.Stop();
			using var process = Process.GetCurrentProcess();
			process.Refresh();
			_mainReadyRecorded = true;
			Record("MainReady");
			Record("ElapsedMilliseconds=" + _elapsed.ElapsedMilliseconds);
			Record("DynamicCodeSupported=" + RuntimeFeature.IsDynamicCodeSupported);
			Record("DynamicCodeCompiled=" + RuntimeFeature.IsDynamicCodeCompiled);
			Record("PrivateBytes=" + process.PrivateMemorySize64);
			Record("WorkingSetBytes=" + process.WorkingSet64);
			Record("ManagedBytes=" + GC.GetTotalMemory(false));
			Record("MainTitle=" + main.Text);
			Record("MainSize=" + main.Width + "x" + main.Height);
			await VerifyScreens(main);
			_screensVerified = true;
			Record("ScreensVerified=" + _case);
			// Use the real application exit and finalization path; forced exit only skips interactive confirmation.
			var application = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<Sphere10.Framework.Windows.Forms.IApplication>();
			await application.ExitAsync(force: true);
		} catch (Exception error) {
			Fail(error);
		}
	}

	private static async System.Threading.Tasks.Task VerifySourceGrid(BlockMainForm main) {
		var host = main.ScreenHost;
		Record("InitialMode=" + main.ScreenMode);
		Guard.Ensure(main.ScreenMode == ScreenMode.MultiView);
		main.Size = new System.Drawing.Size(1280, 900);
		await System.Threading.Tasks.Task.Yield();
		var block = host.Blocks.Single(item => item.ScreenDefinitions.Any(definition => definition.ScreenType == typeof(SourceGridGalleryScreen)));
		var definition = block.ScreenDefinitions.Single(item => item.ScreenType == typeof(SourceGridGalleryScreen));
		Record("SourceGridActivate");
		var gallery = (SourceGridGalleryScreen)await host.OpenScreenAsync(block, definition);
		await System.Threading.Tasks.Task.Yield();
		Record("SourceGridInitialPaint");
		gallery.Refresh();
		CaptureControl(gallery, "sourcegrid-initial.png");
		Record("SourceGridReset");
		gallery.ResetExamples();
		var examples = gallery.Controls.Find("sourceGridExamples", true).OfType<TabControl>().Single();
		Guard.Ensure(examples.TabCount == 7);
		for (var index = 0; index < examples.TabCount; index++) {
			examples.SelectedIndex = index;
			await System.Threading.Tasks.Task.Yield();
			Record("SourceGridExample=" + examples.SelectedTab.Text);
			var csv = gallery.ExportCsv();
			var html = gallery.ExportHtml();
			Guard.Ensure(!string.IsNullOrWhiteSpace(csv) && !string.IsNullOrWhiteSpace(html));
			File.WriteAllText(Path.Combine(_directory, "sourcegrid-" + index + ".csv"), csv);
			File.WriteAllText(Path.Combine(_directory, "sourcegrid-" + index + ".html"), html);
			Record("SourceGridCsv=" + csv.Length);
			Record("SourceGridHtml=" + html.Length);
			using (var preview = gallery.CreatePreviewImage())
				preview.Save(Path.Combine(_directory, "sourcegrid-preview-" + index + ".png"), System.Drawing.Imaging.ImageFormat.Png);
			gallery.Refresh();
			CaptureControl(gallery, "sourcegrid-live-" + index + ".png");
		}
		Record("SourceGridRendered=7");
		Guard.Ensure(await host.TrySetScreenModeAsync(ScreenMode.SingleView));
		Guard.Ensure(main.ScreenMode == ScreenMode.SingleView && !host.TabControl.Visible);
		Guard.Ensure(await host.TrySetScreenModeAsync(ScreenMode.MultiView));
		Guard.Ensure(main.ScreenMode == ScreenMode.MultiView && host.TabControl.Visible && host.TabControl.TabCount > 0);
		Guard.Ensure(ReferenceEquals(host.ActiveScreen, gallery));
		Guard.Ensure(host.TabControl.TabPages.Cast<TabPage>().Any(page => ReferenceEquals(page.Tag, gallery) && page.Controls.Contains(gallery)));
		CaptureControl(gallery, "sourcegrid-restored-tabs.png");
		Record("MultiViewTabsVerified=" + host.TabControl.TabCount);
	}

	private static void CaptureControl(Control control, string file) {
		using var bitmap = new System.Drawing.Bitmap(control.Width, control.Height);
		control.DrawToBitmap(bitmap, control.ClientRectangle);
		bitmap.Save(Path.Combine(_directory, file), System.Drawing.Imaging.ImageFormat.Png);
	}

	private static void Record(string text) => File.AppendAllText(Path.Combine(_directory, "smoke.log"), text + Environment.NewLine);
}
