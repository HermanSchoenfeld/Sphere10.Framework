// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester.Wizard;
using WinFormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WizardDemoTests {
	[TestCase("", true, false)]
	[TestCase("   ", true, false)]
	[TestCase("Ada", false, false)]
	[TestCase("Ada", true, true)]
	public async Task NameRequiresTextAndConfirmation(string Name, bool Confirmed, bool Valid) {
		using var Screen = new EnterNameScreen();
		FindControl<TextBox>(Screen, "textBox1").Text = Name;
		FindControl<CheckBox>(Screen, "checkBox1").Checked = Confirmed;
		Assert.That((await Screen.Validate()).IsSuccess, Is.EqualTo(Valid));
	}

	[TestCase("", false)]
	[TestCase("   ", false)]
	[TestCase("unknown", false)]
	[TestCase("-1", false)]
	[TestCase("12.5", false)]
	[TestCase("2147483648", false)]
	[TestCase("0", true)]
	[TestCase("42", true)]
	[TestCase(" 42 ", true)]
	public async Task AgeRequiresANonNegativeWholeNumber(string Age, bool Valid) {
		using var Screen = new EnterAgeScreen();
		FindControl<TextBox>(Screen, "textBox1").Text = Age;
		Assert.That((await Screen.Validate()).IsSuccess, Is.EqualTo(Valid));
	}

	[Test]
	public void DemoRejectsInvalidNextCollectsValuesAndFinishesWithItsDisplayedModel() => RunWithMessageLoop(async Owner => {
		var Model = DemoWizardModel.Default;
		var NameScreen = new EnterNameScreen();
		var AgeScreen = new EnterAgeScreen();
		var NoBackScreen = new CantGoBackScreen();
		var SummaryScreen = new ConfirmScreen();
		DemoWizardModel? FinishedModel = null;
		using var Wizard = new WizardBuilder<DemoWizardModel>()
			.WithTitle("Demo wizard regression")
			.WithModel(Model)
			.AddScreen(NameScreen)
			.AddScreen(AgeScreen)
			.AddScreen(NoBackScreen)
			.AddScreen(SummaryScreen)
			.OnFinished(Value => {
				FinishedModel = Value;
				return Task.FromResult(Result.Success);
			})
			.Build();
		var Pending = Wizard.Start(Owner);
		var Dialog = await WaitForDialog<WizardDialog<DemoWizardModel>>();
		Assert.That(NameScreen.Visible, Is.True);
		await RejectNext(Wizard, Dialog, "Enter your name.");
		Assert.That(NameScreen.Visible, Is.True);
		Assert.That(Model.Name, Is.Empty);
		Assert.That(Model.Age, Is.Null);
		Assert.That(FinishedModel, Is.Null);

		FindControl<TextBox>(NameScreen, "textBox1").Text = "  Ada Lovelace  ";
		await RejectNext(Wizard, Dialog, "Check the confirmation box to proceed.");
		Assert.That(NameScreen.Visible, Is.True);
		Assert.That(Model.Name, Is.Empty, "Invalid Next must not commit the current form.");
		FindControl<CheckBox>(NameScreen, "checkBox1").Checked = true;
		await Wizard.Next();
		Assert.That(AgeScreen.Visible, Is.True);
		Assert.That(Model.Name, Is.EqualTo("Ada Lovelace"));
		Assert.That(FindControl<TextBox>(AgeScreen, "textBox1").Text, Is.Empty);

		FindControl<TextBox>(AgeScreen, "textBox1").Text = "invalid";
		await RejectNext(Wizard, Dialog, "Enter your age as a non-negative whole number.");
		Assert.That(AgeScreen.Visible, Is.True);
		Assert.That(Model.Age, Is.Null);
		FindControl<TextBox>(AgeScreen, "textBox1").Text = "42";
		Assert.That(Model.Age, Is.Null, "Typing updates the model only after the step validates.");
		await Wizard.Next();
		Assert.That(NoBackScreen.Visible, Is.True);
		Assert.That(Model.Age, Is.EqualTo(42));
		Assert.That(FindControl<Button>(Dialog, "_previousButton").Visible, Is.False);
		await Wizard.Next();
		Assert.That(SummaryScreen.Visible, Is.True);
		Assert.That(FindControl<Label>(SummaryScreen, "label3").Text, Is.EqualTo("Ada Lovelace"));
		Assert.That(FindControl<Label>(SummaryScreen, "label4").Text, Is.EqualTo("42"));
		Assert.That(FindControl<Button>(Dialog, "_previousButton").Visible, Is.True);
		Assert.That(FindControl<Button>(Dialog, "_nextButton").Text, Is.EqualTo("Finish"));
		await Wizard.Next();
		Assert.That(await Pending, Is.EqualTo(WizardResult.Success));
		Assert.That(FinishedModel, Is.SameAs(Model));
		Assert.That(FinishedModel!.Name, Is.EqualTo("Ada Lovelace"));
		Assert.That(FinishedModel.Age, Is.EqualTo(42));
	});

	[Test]
	public void ReturningToAnInputStepRestoresValuesAndRefreshesTheSummaryAfterEdits() => RunWithMessageLoop(async Owner => {
		var Model = new DemoWizardModel { Name = "Original", Age = 20 };
		var NameScreen = new EnterNameScreen();
		var AgeScreen = new EnterAgeScreen();
		var SummaryScreen = new ConfirmScreen();
		using var Wizard = new WizardBuilder<DemoWizardModel>()
			.WithTitle("Demo wizard back navigation")
			.WithModel(Model)
			.AddScreen(NameScreen)
			.AddScreen(AgeScreen)
			.AddScreen(SummaryScreen)
			.OnFinished(Value => Task.FromResult(Result.Success))
			.Build();
		var Pending = Wizard.Start(Owner);
		await WaitForDialog<WizardDialog<DemoWizardModel>>();
		Assert.That(FindControl<TextBox>(NameScreen, "textBox1").Text, Is.EqualTo("Original"));
		FindControl<CheckBox>(NameScreen, "checkBox1").Checked = true;
		await Wizard.Next();
		Assert.That(FindControl<TextBox>(AgeScreen, "textBox1").Text, Is.EqualTo("20"));
		await Wizard.Next();
		await Wizard.Previous();
		await Wizard.Previous();
		Assert.That(FindControl<TextBox>(NameScreen, "textBox1").Text, Is.EqualTo("Original"));
		FindControl<TextBox>(NameScreen, "textBox1").Text = "Revised";
		await Wizard.Next();
		FindControl<TextBox>(AgeScreen, "textBox1").Text = "0";
		await Wizard.Next();
		Assert.That(FindControl<Label>(SummaryScreen, "label3").Text, Is.EqualTo("Revised"));
		Assert.That(FindControl<Label>(SummaryScreen, "label4").Text, Is.EqualTo("0"));
		await Wizard.Previous();
		Assert.That(FindControl<TextBox>(AgeScreen, "textBox1").Text, Is.EqualTo("0"), "A valid zero age must survive revisiting the step.");
		await Wizard.Next();
		await Wizard.Next();
		Assert.That(await Pending, Is.EqualTo(WizardResult.Success));
	});

	[Test]
	public void InitialAndInjectedScreensCanAccessTheirWizardModelDuringInitialize() => RunWithMessageLoop(async Owner => {
		var Model = new DemoWizardModel { Name = "Initialization", Age = 30 };
		var FirstScreen = new InitializeProbeScreen();
		var InjectedScreen = new InitializeProbeScreen();
		using var Wizard = new WizardBuilder<DemoWizardModel>()
			.WithTitle("Wizard initialization")
			.WithModel(Model)
			.AddScreen(FirstScreen)
			.OnFinished(Value => Task.FromResult(Result.Success))
			.Build();
		var Pending = Wizard.Start(Owner);
		await WaitForDialog<WizardDialog<DemoWizardModel>>();
		Assert.That(FirstScreen.InitializedModel, Is.SameAs(Model));
		await Wizard.InjectScreen(InjectedScreen);
		Assert.That(InjectedScreen.InitializedModel, Is.SameAs(Model));
		await Wizard.Next();
		Assert.That(InjectedScreen.Visible, Is.True);
		await Wizard.Next();
		Assert.That(await Pending, Is.EqualTo(WizardResult.Success));
	});

	private static async Task RejectNext(ActionWizard<DemoWizardModel> Wizard, Form Owner, string ExpectedMessage) {
		var Pending = Wizard.Next();
		var Error = await WaitForDialog<DialogEx>();
		Assert.That(Error.Owner, Is.SameAs(Owner));
		Assert.That(Pending.IsCompleted, Is.False);
		Assert.That(FindControl<Label>(Error, "_textLabel").Text, Does.Contain(ExpectedMessage));
		FindControl<Button>(Error, "button1").PerformClick();
		await Pending;
	}

	private static T FindControl<T>(Control Parent, string Name) where T : Control => (T)Parent.Controls.Find(Name, true).Single();

	private static async Task<T> WaitForDialog<T>() where T : Form {
		for (var Attempt = 0; Attempt < 500; Attempt++) {
			var Dialog = WinFormsApplication.OpenForms.Cast<Form>().OfType<T>().FirstOrDefault(Form => Form.Visible);
			if (Dialog != null)
				return Dialog;
			await Task.Delay(10);
		}
		throw new AssertionException($"{typeof(T).Name} did not become visible.");
	}

	private static void RunWithMessageLoop(Func<Form, Task> Test) {
		Exception? Failure = null;
		var Completed = false;
		var PreviousContext = SynchronizationContext.Current;
		using var UiContext = new WindowsFormsSynchronizationContext();
		using var RestoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(PreviousContext));
		SynchronizationContext.SetSynchronizationContext(UiContext);
		using var Owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var Watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		Watchdog.Tick += (_, _) => {
			Failure = new AssertionException("The wizard test timed out.");
			foreach (var Dialog in WinFormsApplication.OpenForms.Cast<Form>().Reverse().ToArray())
				Dialog.Dispose();
			WinFormsApplication.ExitThread();
		};
		Owner.Shown += async (_, _) => {
			using var CloseOwner = Tools.Scope.ExecuteOnDispose(Owner.Close);
			try {
				await Test(Owner);
				Completed = true;
			} catch (Exception Error) {
				Failure = Error;
			}
		};
		Watchdog.Start();
		WinFormsApplication.Run(Owner);
		Assert.That(Failure, Is.Null, Failure?.ToString());
		Assert.That(Completed, Is.True, "The message loop must execute and complete the test body.");
	}

	private sealed class InitializeProbeScreen : WizardScreen<DemoWizardModel> {
		public DemoWizardModel? InitializedModel { get; private set; }

		public override async Task Initialize() {
			await Task.Yield();
			InitializedModel = Model;
		}
	}
}
