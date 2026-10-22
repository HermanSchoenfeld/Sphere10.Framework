// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Tests.Windows.Forms;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class LogOutputDestinationWizardScreenTests {
	[TestCase(1F, 9F)]
	[TestCase(1.5F, 12F)]
	[TestCase(2F, 14F)]
	public void DestinationExplanationsRemainReadableWhenScaled(float scale, float fontSize) => RunWithMessageLoop(owner => {
		// Exercise wrapping and row growth under both larger native bounds and a larger inherited font.
		using var screen = new LogOutputDestinationWizardScreen();
		using var font = new Font("Segoe UI", fontSize);
		screen.Scale(new SizeF(scale, scale));
		screen.Font = font;
		screen.Size = new Size((int)(650 * scale), (int)(362 * scale));
		owner.ClientSize = screen.Size;
		owner.Controls.Add(screen);
		screen.PerformLayout();
		var layout = FindControl<TableLayoutPanel>(screen, "_layoutPanel");
		layout.PerformLayout();

		// Each caption and its explanation must have distinct, fully measured space in the same row.
		var options = layout.Controls.OfType<RadioButton>().OrderBy(option => option.Top).ToArray();
		Assert.That(options, Has.Length.EqualTo(5));
		var previousBottom = 0;
		foreach (var option in options) {
			var description = (Label)layout.GetControlFromPosition(1, layout.GetRow(option));
			Assert.That(option.Font.Bold, Is.True, option.Text);
			Assert.That(description.Font.Bold, Is.False, option.Text);
			Assert.That(option.Font.SizeInPoints, Is.EqualTo(description.Font.SizeInPoints).Within(0.1F), option.Text);
			Assert.That(description.Left, Is.GreaterThanOrEqualTo(option.Right), option.Text);
			Assert.That(Math.Min(option.Top, description.Top), Is.GreaterThanOrEqualTo(previousBottom), option.Text);
			Assert.That(description.Height, Is.GreaterThanOrEqualTo(description.GetPreferredSize(new Size(description.Width, 0)).Height), option.Text);
			Assert.That(description.Right, Is.LessThanOrEqualTo(layout.ClientSize.Width), option.Text);
			Assert.That(description.Bottom, Is.LessThanOrEqualTo(layout.ClientSize.Height), option.Text);
			Assert.That(option.AccessibleDescription, Is.EqualTo(description.Text), option.Text);
			previousBottom = Math.Max(option.Bottom, description.Bottom);
		}
		Assert.That(FindControl<RadioButton>(screen, "_applicationRadioButton").Text, Is.EqualTo("Windows Application Log"));
		SaveScreenshot(screen, $"log-output-destinations-{scale:0.0}.png");
		return Task.CompletedTask;
	});

	[TestCase("file", LogOutputDestination.File)]
	[TestCase("application", LogOutputDestination.Application)]
	[TestCase("standardOutput", LogOutputDestination.StandardOutput)]
	[TestCase("standardError", LogOutputDestination.StandardError)]
	[TestCase("screen", LogOutputDestination.Screen)]
	public void ExplanationClickSelectsAndAcceptsDestination(string optionName, LogOutputDestination destination) => RunWithMessageLoop(async owner => {
		// Use the real native wizard to verify that clickable descriptions preserve destination branching.
		using var screen = new LogOutputDestinationWizardScreen { ApplicationAvailable = true };
		var model = new LogOutputConfiguration();
		var wizard = new WinFormsWizardBuilder<LogOutputConfiguration>()
			.WithTitle("Log Output")
			.WithModel(model)
			.AddScreen(screen)
			.AddScreen(new LogOutputFormattingWizardScreen())
			.AddScreen(new LogOutputReviewWizardScreen())
			.OnFinished(_ => Task.FromResult(Result.Success))
			.OnCancelled(_ => Result.Success)
			.Build();
		var pending = wizard.Start(owner);
		var dialog = await WaitForDialog(owner);
		var description = FindControl<Label>(screen, $"_{optionName}DescriptionLabel");
		typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(description, new object[] { EventArgs.Empty });
		Assert.That(FindControl<RadioButton>(screen, $"_{optionName}RadioButton").Checked, Is.True);
		Assert.That(FindControl<TableLayoutPanel>(screen, "_layoutPanel").Controls.OfType<RadioButton>().Count(option => option.Checked), Is.EqualTo(1));
		Assert.That((await dialog.WizardManager.MoveNextAsync()).IsSuccess, Is.True);
		Assert.That(model.Destination, Is.EqualTo(destination));
		Assert.That(dialog.WizardManager.CurrentStep.GetType(), Is.EqualTo(destination == LogOutputDestination.File
			? typeof(LogOutputFileWizardScreen)
			: typeof(LogOutputFormattingWizardScreen)));
		((Form)dialog).Close();
		await pending;
	});

	[Test]
	public void UnavailableApplicationExplanationCannotSelectDisabledDestination() => RunWithMessageLoop(async owner => {
		using var selector = new LogOutputSelectorControl();
		owner.Controls.Add(selector);
		var pending = selector.ConfigureAsync(owner);
		var dialog = await WaitForDialog(owner);
		var application = FindControl<RadioButton>(dialog, "_applicationRadioButton");
		var description = FindControl<Label>(dialog, "_applicationDescriptionLabel");
		Assert.That(application.Enabled, Is.False);
		Assert.That(description.Enabled, Is.False);
		typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(description, new object[] { EventArgs.Empty });
		Assert.That(application.Checked, Is.False);
		Assert.That(FindControl<RadioButton>(dialog, "_fileRadioButton").Checked, Is.True);
		((Form)dialog).Close();
		Assert.That(await pending, Is.False);
	});

	private static T FindControl<T>(Control parent, string name)
		where T : Control => (T)parent.Controls.Find(name, true).Single();

	private static async Task<WinFormsWizardDialog<LogOutputConfiguration>> WaitForDialog(Form owner) {
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (DateTime.UtcNow < deadline) {
			var dialog = owner.OwnedForms.OfType<WinFormsWizardDialog<LogOutputConfiguration>>().FirstOrDefault(form => form.Visible);
			if (dialog != null)
				return dialog;
			await Task.Delay(10);
		}
		throw new AssertionException("The logging destination wizard was not displayed.");
	}

	private static void SaveScreenshot(Control control, string filename) {
		var directory = Environment.GetEnvironmentVariable("SPHERE10_WINFORMS_SCREENSHOTS");
		if (string.IsNullOrWhiteSpace(directory))
			return;
		Directory.CreateDirectory(directory);
		using var bitmap = new Bitmap(control.Width, control.Height);
		control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
		bitmap.Save(Path.Combine(directory, filename));
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previousContext = SynchronizationContext.Current;
		using var uiContext = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(uiContext);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The logging destination test timed out.");
			foreach (var dialog in owner.OwnedForms) dialog.Dispose();
			FormsApplication.ExitThread();
		};
		owner.Shown += async (_, _) => {
			using var closeDialogs = Tools.Scope.ExecuteOnDispose(() => {
				foreach (var dialog in owner.OwnedForms) dialog.Dispose();
				owner.Close();
			});
			try { await test(owner); completed = true; } catch (Exception error) { failure = error; }
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}
}
