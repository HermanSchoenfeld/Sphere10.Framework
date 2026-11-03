// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Data;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.Crud;
using Sphere10.Framework.Windows.Forms.DataVisualization;
using Sphere10.Framework.Windows.Forms.Firebird;
using Sphere10.Framework.Windows.Forms.MSSQL;
using Sphere10.Framework.Windows.Forms.MySQL;
using Sphere10.Framework.Windows.Forms.Oracle;
using Sphere10.Framework.Windows.Forms.PostgreSQL;
using Sphere10.Framework.Windows.Forms.Sqlite;
using Sphere10.Framework.Windows.Forms.TextBoxes;
using Book = Sphere10.Framework.Windows.Forms.AppointmentBook;
using SourceGrid = Sphere10.Framework.Windows.Forms.SourceGrid;
using FormsUI = global::Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Tester-owned examples. This list deliberately makes additions to the public control surface reviewable.</summary>
public static class ControlCatalogSamples {
	public static ControlCatalogEntry[] CreateEntries() {
		return new[] {
			Sample<ApplicationBanner>("Application", () => new ApplicationBanner { Title = "Control catalog", CompanyName = "Sphere10", Version = "Interactive example" }),
			Sample<ApplicationBannerHost>("Application", () => new ApplicationBannerHost()),
			Sample<ApplicationBar>("Application", CreateApplicationBar, "Select a section using the real application bar buttons."),
			Sample<ApplicationControl>("Application", () => Composed(new ApplicationControl())),
			Sample<ApplicationSettingsEditor>("Application", CreateSettingsEditor, "Edit private inspector preferences using the shared settings editor. These sample values are not saved."),
			Window<ApplicationForm>(() => new ApplicationForm()),
			Sample<BarChart>("Charts", () => new ChartTestScreen(), "Use the columns and bars tabs; add samples, change scenarios and inspect the axes."),
			Sample<BasicContactDetailsControl>("Application", () => new BasicContactDetailsControl { ContactIsAnonymous = false, ContactEmail = "sample@example.invalid" }),
			Sample<BitmapButton>("Buttons", CreateBitmapButton, "The bitmap contains five horizontal state frames. Hover, focus, press and disable the actual button to inspect each frame."),
			Sample<BlockConfigurationScreen>("Application", () => Composed(new BlockConfigurationScreen())),
			Window<BlockMainForm>(() => CreateMainForm(new BlockMainForm()), "An independent native shell containing an editable screen. Closing it closes this example."),
			Sample<ButtonTextBox>("Text", CreateButtonTextBox),
			Sample<CheckedGroupBox>("Groups", () => Composed(new CheckedGroupBox { Text = "Enable this group" })),
			Sample<CheckedListBoxEx>("Choices", () => Items(new CheckedListBoxEx())),
			Sample<ColorPicker>("Editors", () => new ColorPicker()),
			Sample<ComboBoxEx>("Choices", () => Items(new ComboBoxEx())),
			Sample<ConnectionBarBase>("Database", () => Composed(new ConnectionBarBase()), "Concrete designer base; provider-specific behavior is available under DatabaseConnectionBar."),
			Sample<ConnectionPanelBase>("Database", () => Composed(new ConnectionPanelBase()), "Concrete designer base; provider-specific behavior is available under DatabaseConnectionPanel."),
			Window<CrudDialog>(CreateCrudDialog, "Edit an isolated in-memory employee list; no application data is used."),
			Window<CrudEntityEditorDialog>(CreateCrudEditorDialog, "Inspect an isolated employee using the real native CRUD entity dialog."),
			Sample<CrudReferencePicker>("Data", () => new CrudReferencePicker(CreateReferenceBinding(), null), "Search and select an employee from the in-memory list."),
			Sample<CustomComboBox>("Choices", () => new CustomComboBoxScreen(), "Open both custom dropdowns, edit their borrowed controls, then reopen them."),
			Sample<DatabaseConnectionBar>("Database", () => new ConnectionBarTestScreen(), "Configure a connection without connecting automatically; testing a connection is an explicit action."),
			Sample<DatabaseConnectionPanel>("Database", () => new ConnectionPanelTestScreen(), "Choose a database provider and edit its parameters. No connection opens automatically."),
			Sample<MSSQLConnectionBar>("Database providers", () => new MSSQLConnectionBar { Server = "localhost", Database = "sample", Username = "sample" }, "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<MSSQLConnectionPanel>("Database providers", () => new MSSQLConnectionPanel { Server = "localhost", Database = "sample", Username = "sample" }, "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<MySQLConnectionBar>("Database providers", () => new MySQLConnectionBar { Server = "localhost", Database = "sample", Username = "sample" }, "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<MySQLConnectionPanel>("Database providers", () => new MySQLConnectionPanel { Server = "localhost", Database = "sample", Username = "sample" }, "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<OracleConnectionBar>("Database providers", () => new OracleConnectionBar { Server = "localhost", Database = "sample", Username = "sample" }, "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<OracleConnectionPanel>("Database providers", () => new OracleConnectionPanel { Server = "localhost", Database = "sample", Username = "sample" }, "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<PostgreSQLConnectionBar>("Database providers", () => new PostgreSQLConnectionBar { Server = "localhost", Database = "sample", Username = "sample" }, "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<PostgreSQLConnectionPanel>("Database providers", () => new PostgreSQLConnectionPanel { Server = "localhost", Database = "sample", Username = "sample" }, "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<SqliteConnectionBar>("Database providers", () => new SqliteConnectionBar { ConnectionString = "Data Source=:memory:;" }, "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<SqliteConnectionPanel>("Database providers", () => new SqliteConnectionPanel { ConnectionString = "Data Source=:memory:;" }, "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<FirebirdConnectionBar>("Database providers", () => new FirebirdConnectionBar { Server = "localhost", Database = "sample.fdb", Username = "sample" }, "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<FirebirdConnectionPanel>("Database providers", () => new FirebirdConnectionPanel { Server = "localhost", Database = "sample.fdb", Username = "sample" }, "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<FirebirdEmbeddedConnectionBar>("Database providers", () => new FirebirdEmbeddedConnectionBar(), "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<FirebirdEmbeddedConnectionPanel>("Database providers", () => new FirebirdEmbeddedConnectionPanel(), "Edit local sample connection parameters. Selecting or editing this control does not connect to a database."),
			Sample<DecimalBox>("Numbers", () => new DecimalBox { Text = "123.45" }),
			Sample<DefaultCrudEntityEditor>("Data", CreateEntityEditor, "Edit the sample employee's properties; the sample is private to this view."),
			Sample<DefaultWinFormsWizardScreen>("Wizards", () => Composed(new DefaultWinFormsWizardScreen()), "Concrete dictionary-model wizard screen, shown as a composed editable surface."),
			Window<DialogEx>(() => new DialogEx()),
			Sample<DropDownButton>("Buttons", () => new DropDownButton { Height = 40 }),
			new ControlCatalogEntry(typeof(DropDownForm), "Dropdowns", "Open the positioned dropdown editor. Press Escape or click outside it to close.", null, true, ShowDropDownAsync),
			Sample<EditableControl>("Editors", () => Composed(new EditableControl())),
			Window<EnterTextDialog>(() => new EnterTextDialog("Edit this sample text")),
			Sample<EnumComboBox>("Choices", () => new EnumComboScreen()),
			new ControlCatalogEntry(typeof(ExceptionDialog), "Forms", "Inspect a sample exception and its details without causing a real failure.", null, true, owner => ExceptionDialog.ShowAsync(owner, new InvalidOperationException("A sample exception for inspecting the error display."))),
			Window<ExecuteScriptForm>(CreateScriptDialog, "Review and execute SELECT 1 against a private SQLite memory database. No existing database is used."),
			new ControlCatalogEntry(typeof(ExpandingCircle), "Feedback", "Play the native expanding-circle effect. It closes itself when the expansion completes.", null, true, ShowExpandingCircleAsync),
			Sample<Expando>("Layout", () => new ExpandoTesterScreen(), "Expand, collapse and rearrange the real task-pane groups."),
			Sample<FlagsCheckedListBox>("Choices", () => new FlagsCheckedBoxListScreen()),
			Window<FormEx>(() => new FormEx()),
			Window<GenericEditorForm>(() => new GenericEditorForm(CreateEmployee(), false)),
			Sample<GradientPanel>("Layout", () => Composed(new GradientPanel(Color.AliceBlue, Color.SteelBlue, 90))),
			Sample<GraphChart>("Charts", () => new ChartTestScreen(), "Select line or area, change the scenario and add values using the chart's actual actions."),
			Sample<HeaderGroupBox>("Groups", () => Composed(new HeaderGroupBox { Text = "HeaderGroupBox" })),
			Sample<HexEditorControl>("Editors", CreateHexEditor, "Edit the sample bytes using the actual hex and text views."),
			Window<HexEditorDialog>(() => new HexEditorDialog { Bytes = new byte[] { 0x53, 0x70, 0x68, 0x65, 0x72, 0x65, 0x31, 0x30 } }),
			Sample<ImageAttachmentSelectorControl>("Images", () => new ImageAttachmentSelectorControl(), "Choose None, a local file or the clipboard. Images are read only after your explicit selection."),
			Sample<ImageLinkLabel>("Images", CreateImageLink),
			Sample<ImageNavigator>("Images", CreateImageNavigator, "Use Previous and Next to navigate two owned sample images."),
			Sample<IntBox>("Numbers", () => new IntBox { Text = "42" }),
			Sample<ListMerger>("Choices", () => new MiscTestScreen(), "Move entries between the two populated lists with the merge buttons."),
			Window<LiteMainForm>(() => Composed(new LiteMainForm()), "Independent native shell; its close operation closes only this example."),
			Sample<LoadingCircle>("Feedback", () => new LoadingCircleTestScreen()),
			Sample<LoadingCircleToolStripMenuItem>("Tool strip items", CreateLoadingItem, "A real hosted LoadingCircle. Toggle animation using the adjacent button."),
			Window<LogonDialog>(() => new LogonDialog("Sample sign in", "Enter any nonempty username and password. This authenticator runs locally.", (user, password) => Task.FromResult(new AuthenticationResult { ResultCode = !string.IsNullOrWhiteSpace(user) && !string.IsNullOrEmpty(password) ? AuthenticationErrorCode.Success : AuthenticationErrorCode.InvalidCredentials }))),
			new ControlCatalogEntry(typeof(LogOutputConfigurationDialog), "Logging", "Inspect a locally owned logging selector's real configuration dialog.", null, true, ShowLoggingConfigurationAsync),
			WizardStep<LogOutputDestinationWizardScreen>(() => new LogOutputDestinationWizardScreen()),
			WizardStep<LogOutputFileWizardScreen>(() => new LogOutputFileWizardScreen()),
			WizardStep<LogOutputFormattingWizardScreen>(() => new LogOutputFormattingWizardScreen()),
			WizardStep<LogOutputReviewWizardScreen>(() => new LogOutputReviewWizardScreen()),
			WizardStep<LogOutputRollingWizardScreen>(() => new LogOutputRollingWizardScreen()),
			Sample<LogOutputSelectorControl>("Logging", () => new LoggingTestScreen(), "Select an output and send sample messages. File output requires an explicit destination selection."),
			WizardStep<LogOutputWizardScreen>(() => Composed(new LogOutputWizardScreen())),
			Sample<LogScreen>("Logging", CreateLogScreen, "This real host-owned log view receives a sample message when loaded."),
			Window<MainForm>(() => CreateMainForm(new MainForm()), "Independent native application frame with its native menus, toolbar and editable screen."),
			Sample<MaskedTextBoxEx>("Text", () => new MaskedTextBoxEx { Mask = "000-000", Text = "123456" }),
			Window<MergeableMenuStrip>(() => new VisualInheritanceFixerSubForm(), "The hidden MergeableMenuStrip contributes real items to the visible inherited menu. Inspect and use the merged menus in this original example."),
			Window<MergeableStatusStrip>(() => new VisualInheritanceFixerSubForm(), "The hidden MergeableStatusStrip contributes items to the visible inherited status strip in this original example."),
			Window<MergeableToolStrip>(() => new VisualInheritanceFixerSubForm(), "The hidden MergeableToolStrip contributes items to the visible inherited toolbar in this original example."),
			Sample<MoneyBox>("Numbers", () => new MoneyBox { Text = "123.45" }),
			Sample<MultiSelectionButton>("Choices", CreateMultiSelectionButton),
			new ControlCatalogEntry(typeof(NumericBoxBase<>), "Numbers / abstract", "NumericBoxBase<T> is abstract. IntBox demonstrates the closed NumericBoxBase<int> implementation.", () => new IntBox { Text = "42" }),
			Sample<NumericUpDownEx>("Numbers", () => new NumericUpDownEx { Minimum = -100, Maximum = 100, Value = 42 }),
			Sample<PadLockButton>("Buttons", () => new PadLockTestScreen()),
			Sample<PanelEx>("Layout", () => Composed(new PanelEx())),
			Window<PasswordDialog>(() => new PasswordDialog("Sample password", "Enter and confirm a sample password. The secret is not printed.")),
			Sample<PathSelectorControl>("Editors", () => new PathSelectorTestScreen()),
			new ControlCatalogEntry(typeof(PerPixelAlphaForm), "Forms", "A per-pixel-alpha sample. Click the image to close it; it also closes automatically after five seconds.", null, true, ShowAlphaFormAsync),
			Sample<PlaceHolderTextExtender>("Components", CreatePlaceholder, "The component supplies native placeholder text. Type into the editor, then clear it to see the cue return."),
			Sample<PictureBoxEx>("Images", CreatePictureBox),
			new ControlCatalogEntry(typeof(PopupDropDown), "Dropdowns", "Opens a real ToolStrip popup containing a sample text editor. Click outside it or press Escape to close.", CreatePopup, true),
			Window<ProductAboutBox>(() => new ProductAboutBox()),
			Window<ProductProductReportBugDialog>(() => new ProductProductReportBugDialog(), "Inspect the product feedback form. Sending feedback is a separate explicit action."),
			Window<ProductProductRequestFeatureDialog>(() => new ProductProductRequestFeatureDialog(), "Inspect the feature request form. Sending feedback is a separate explicit action."),
			Window<ProductProductSendCommentsDialog>(() => new ProductProductSendCommentsDialog(), "Inspect the comments form. Sending feedback is a separate explicit action."),
			Sample<ProgressBarEx>("Feedback", () => new ProgressBarEx { Value = 65, Height = 28 }),
			Sample<PropertyGridEx>("Editors", () => new PropertyGridEx { SelectedObject = CreateEmployee() }),
			Window<QuestionDialog>(() => new QuestionDialog(SystemIconType.Question, "Sample question", "Choose one of the sample responses.", "First", "Second", "Cancel")),
			Sample<RadioButtonPanel>("Groups", CreateRadioGroups, "Select either group header or the standalone radio option. RadioButtonPanel coordinates their mutually exclusive selection."),
			Sample<RadioGroupBox>("Groups", CreateRadioGroups, "Select a group header to enable that group's editor and disable the other group."),
			Window<ReviewTextDialog>(() => new ReviewTextDialog { Instructions = "Review, copy or explicitly save this sample text.", PreviewText = "Sphere10 native control catalog\r\nA complete sample text preview.", DefaultFileName = "control-catalog.txt" }),
			Sample<RichTextBoxEx>("Text", () => new RichTextBoxEx { Text = "Edit this rich text sample." }),
			Sample<RoundedPanel>("Layout", () => Composed(new RoundedPanel())),
			Sample<SearchTextBox>("Text", () => new SearchTextBox { Text = "Search sample" }),
			Sample<SeparatorLine>("Layout", () => new SeparatorLine { Height = 24 }),
			Sample<SidebarToggleButton>("Tool strip items", CreateSidebarToggle, "Toggle the sidebar button to show or hide its sample content panel."),
			Window<ServerPostBackForm>(() => new ServerPostBackForm()),
			Sample<ServiceStatusControl>("Windows", () => new ServiceStatusControl { ServiceName = "Sphere10.ControlCatalog.NotInstalled", Status = ServiceStatus.NotInstalled }, "Shows a deliberately absent sample service. No service is started or stopped automatically."),
			Window<SplashScreen>(CreateSplashScreen),
			Sample<SplitButton>("Buttons", CreateSplitButton),
			Sample<SquareButton>("Buttons", () => new SquareButton { Text = "Press this square button", Height = 60 }),
			Sample<TaskItem>("Layout", () => new TaskItem { Text = "TaskItem — click this task", Height = 48 }),
			Sample<TaskPane>("Layout", () => new ExpandoTesterScreen()),
			Sample<TelemetryChart>("Charts", () => new TelemetryChartTestScreen()),
			Sample<TextBoxEx>("Text", () => new TextBoxEx { Text = "Editable TextBoxEx sample" }),
			Sample<TextBoxUITypeEditor>("Editors", CreateTypeEditor),
			Sample<ToolStripChoiceSelector>("Tool strip items", CreateChoiceSelector, "Select an item from the native checked menu; the selected value is displayed beside it."),
			Window<ToolStripVisualInheritanceFixer>(() => new VisualInheritanceFixerSubForm(), "The component merges the inherited and derived tool strips. Use the original example's commands."),
			Window<TextEditorForm>(() => new TextEditorForm("Edit this sample document.")),
			Sample<UserControlEx>("Layout", () => Composed(new UserControlEx())),
			Sample<ValidatedTextBoxButton>("Editors", CreateValidatedEditor),
			Sample<ValidationIndicator>("Feedback", () => new ValidationIndicatorTestScreen()),
			Sample<VisualGrid>("Data", () => new VisualGridTestScreen()),
			Sample<VisualGridConfigurationContextMenu>("Data", CreateGridConfigurationMenu, "The registered Visual Grid Laboratory demonstrates this header menu on both grids. Commands require a hosted settings role; use that main-shell screen to exercise Save configuration and Restore defaults."),
			Sample<WhoAreYouControl>("Application", () => new WhoAreYouControl()),
			Sample<ApplicationScreen>("Application", () => Composed(new ApplicationScreen { Title = "Standalone screen example" })),
			new ControlCatalogEntry(typeof(ApplicationScreenForm), "Application", "Opens an isolated screen through its native host, then detaches it using the native hosting API.", null, true, ShowDetachedScreenAsync),
			Sample<FormsUI.ApplicationScreenHost>("Application", CreateScreenHost, "A private host containing a native sample screen; its state is independent of the tester's host."),
			new ControlCatalogEntry(typeof(ApplicationScreenHostBase), "Application / abstract", "Abstract host base, demonstrated by the concrete ApplicationScreenHost.", CreateScreenHost),
			Sample<ApplicationScreenTabControl>("Application", CreateScreenTabs),
			new ControlCatalogEntry(typeof(ApplicationSettingsDialog), "Application", "Edits the running application's actual owner settings.", null, true, ShowOptionsAsync),
			new ControlCatalogEntry(typeof(WinFormsWizardDialog<>), "Wizards / generic", "Runs the closed WinFormsWizardDialog<LogOutputConfiguration>. Finish validates sample settings without creating an output or writing a file.", null, true, owner => ShowLogWizardAsync(owner, new LogOutputDestinationWizardScreen(), new LogOutputFormattingWizardScreen(), new LogOutputReviewWizardScreen())),
			new ControlCatalogEntry(typeof(WinFormsWizardScreen<>), "Wizards / generic", "Closed WinFormsWizardScreen<LogOutputConfiguration> with a locally owned model.", null, true, owner => ShowLogWizardAsync(owner, Composed(new WinFormsWizardScreen<LogOutputConfiguration>()))),
			Sample<Book.AppointmentBook>("Appointments", () => new AppointmentBookScreen()),
			Sample<Book.BaseAppointmentBook>("Appointments", () => new AppointmentBookScreen()),
			Sample<Book.UnallocatedBook>("Appointments", () => new AppointmentBookScreen()),
			Sample<CrudComboBox>("Data", () => new VisualGridTestScreen()),
			Sample<Chart>("Charts", () => new ChartGalleryScreen()),
			Sample<SourceGrid.ArrayGrid>("SourceGrid", () => new SourceGridGalleryScreen()),
			new ControlCatalogEntry(typeof(SourceGrid.CustomScrollControl), "SourceGrid / abstract", "Abstract scroll base, demonstrated by a populated, editable SourceGrid.Grid.", () => new SourceGridGalleryScreen()),
			Sample<SourceGrid.DataGrid>("SourceGrid", () => new SourceGridGalleryScreen()),
			Sample<SourceGrid.Grid>("SourceGrid", () => new SourceGridGalleryScreen()),
			new ControlCatalogEntry(typeof(SourceGrid.GridVirtual), "SourceGrid / abstract", "Abstract grid engine, demonstrated by the concrete cell, bound and virtual grid examples.", () => new SourceGridGalleryScreen()),
			Sample<SourceGrid.ListEditor>("SourceGrid", () => new SourceGridGalleryScreen()),
			Sample<SourceGrid.Extensions.PingGrids.PingGrid>("SourceGrid", () => new SourceGridGalleryScreen()),
			Sample<SourceGrid.Planning.PlanningGrid>("SourceGrid", () => new SourceGridGalleryScreen()),
			Sample<TextLog>("Logging", CreateTextLog, "Edit and select the native text. Text and selection properties reflect the visible editor; AppendText appends through that same editor."),
			Sample<Sphere10.Framework.Windows.Forms.WinForms.TransparentPanel>("Layout", () => Composed(new Sphere10.Framework.Windows.Forms.WinForms.TransparentPanel()))
		};
	}

	private static ControlCatalogEntry Sample<T>(string category, Func<Control> create, string instructions = "Interact with the actual control. The property panel edits this live sample only.") {
		return new ControlCatalogEntry(typeof(T), category, instructions, create);
	}

	private static ControlCatalogEntry Window<T>(Func<Control> create, string instructions = "Open this real form in an owned window. Close it to return to the catalog.") {
		return new ControlCatalogEntry(typeof(T), "Forms", instructions, () => {
			var example = create();
			if (example is Form { Controls.Count: 0 })
				Composed(example);
			return example;
		}, true);
	}

	private static ControlCatalogEntry WizardStep<T>(Func<WinFormsWizardScreen<LogOutputConfiguration>> create) {
		return new ControlCatalogEntry(typeof(T), "Logging / wizards", "Open this step with its actual wizard model and native navigation. Finishing this sample does not open output or create a file.", null, true, owner => ShowLogWizardAsync(owner, create()));
	}

	private static T Composed<T>(T control)
		where T : Control {
		if (control is GroupBox)
			control.Padding = new Padding(12, 28, 12, 12);
		control.Controls.Add(new TextBox { Dock = DockStyle.Top, Text = "Edit this content inside " + control.GetType().Name });
		if (control is RadioButtonPanel) {
			control.Controls.Add(new RadioButton { Text = "Second option", Dock = DockStyle.Top });
			control.Controls.Add(new RadioButton { Text = "First option", Checked = true, Dock = DockStyle.Top });
		}
		return control;
	}

	private static ComboBoxEx Items(ComboBoxEx control) {
		control.Items.AddRange(new object[] { "First choice", "Second choice", "Third choice" });
		control.SelectedIndex = 0;
		return control;
	}

	private static CheckedListBoxEx Items(CheckedListBoxEx control) {
		control.Items.AddRange(new object[] { "Read", "Write", "Execute" });
		control.SetItemChecked(0, true);
		return control;
	}

	private static ApplicationBar CreateApplicationBar() {
		var bar = new ApplicationBar();
		bar.AddItem(new ApplicationBar.Item(new TextBox { Multiline = true, Text = "First section content" }, null, "First section"));
		bar.AddItem(new ApplicationBar.Item(new TextBox { Multiline = true, Text = "Second section content" }, null, "Second section"));
		bar.Disposed += (_, _) => {
			foreach (var item in bar.Items)
				item.MenuControl.Dispose();
		};
		return bar;
	}

	private static Control CreateEntityEditor() {
		var editor = new DefaultCrudEntityEditor();
		editor.SetEntity(DataSourceCapabilities.CanRead | DataSourceCapabilities.CanUpdate, CreateEmployee(), false);
		return editor;
	}

	// Keep this settings sample detached from any provider or operational screen.
	private static Control CreateSettingsEditor() {
		var editor = new ApplicationSettingsEditor();
		editor.SetEntity(DataSourceCapabilities.CanRead | DataSourceCapabilities.CanUpdate, new Screens.ScreenHostingDesignTestScreen.ScreenHostingPreferences(), false);
		return editor;
	}

	private static Employee CreateEmployee() {
		return new Employee { ID = 1, FirstName = "Ada", LastName = "Lovelace" };
	}

	private static TestCrudDataSource CreateEmployees() {
		var source = new TestCrudDataSource();
		source.AddEmployee(CreateEmployee());
		source.AddEmployee(new Employee { ID = 2, FirstName = "Grace", LastName = "Hopper" });
		return source;
	}

	private static CrudReferenceBinding CreateReferenceBinding() {
		return new CrudReferenceBinding<Employee>(CreateEmployees(), new[] {
			EmployeeColumn(nameof(Employee.FirstName), "First name", employee => employee.FirstName),
			EmployeeColumn(nameof(Employee.LastName), "Last name", employee => employee.LastName)
		}, employee => employee.FirstName + " " + employee.LastName);
	}

	private static Form CreateCrudDialog() {
		var dialog = new CrudDialog();
		_ = dialog.SetCrudParameters(new[] { EmployeeColumn(nameof(Employee.FirstName), "First name", employee => employee.FirstName), EmployeeColumn(nameof(Employee.LastName), "Last name", employee => employee.LastName) }, typeof(DefaultCrudEntityEditor), DataSourceCapabilities.Default, CreateEmployees());
		return dialog;
	}

	private static Form CreateCrudEditorDialog() {
		var dialog = new CrudEntityEditorDialog();
		dialog.SetEntityEditor(null, new DefaultCrudEntityEditor(), DataSourceCapabilities.CanRead, CreateEmployee(), false);
		return dialog;
	}

	private static Form CreateScriptDialog() {
		var database = Tools.Sqlite.Open(":memory:");
		var script = database.CreateSQLBuilder();
		script.Emit("SELECT 1;");
		return new ExecuteScriptForm(database, script);
	}

	private static Control CreateHexEditor() {
		var editor = new HexEditorControl();
		editor.Bytes = new byte[] { 0x53, 0x70, 0x68, 0x65, 0x72, 0x65, 0x31, 0x30 };
		return editor;
	}

	private static Control CreateImageNavigator() {
		var images = new[] { CreateImage(Color.SteelBlue), CreateImage(Color.SeaGreen) };
		var navigator = new ImageNavigator { Images = images, ImageAreaSizeMode = PictureBoxSizeMode.Zoom };
		navigator.Disposed += (_, _) => {
			foreach (var image in images)
				image.Dispose();
		};
		return navigator;
	}

	private static Bitmap CreateImage(Color color) {
		var bitmap = new Bitmap(240, 160);
		using var graphics = Graphics.FromImage(bitmap);
		graphics.Clear(color);
		graphics.DrawEllipse(Pens.White, 30, 25, 180, 110);
		return bitmap;
	}

	private static Control CreatePictureBox() {
		var image = CreateImage(Color.SteelBlue);
		var picture = new PictureBoxEx { Image = image, SizeMode = PictureBoxSizeMode.Zoom };
		picture.Disposed += (_, _) => image.Dispose();
		return picture;
	}

	private static Control CreateImageLink() {
		var image = CreateImage(Color.SteelBlue);
		var link = new ImageLinkLabel { Text = "Click the image link", Image = image, EnableMouseEffect = true, BorderWidth = 2, BorderRound = 12 };
		link.Click += (_, _) => link.Text = "ImageLinkLabel clicked";
		link.Disposed += (_, _) => image.Dispose();
		return link;
	}

	private static Control CreatePopup() {
		var popup = new PopupDropDown(true);
		popup.Items.Add(new ToolStripControlHost(new TextBox { Multiline = true, Text = "Edit this popup content", Size = new Size(300, 120) }));
		return popup;
	}

	private static Control CreateSplitButton() {
		var button = new SplitButton { Text = "Action and menu", Height = 36, ContextMenuStrip = new ContextMenuStrip() };
		button.ContextMenuStrip.Items.Add("First action");
		button.ContextMenuStrip.Items.Add("Second action");
		button.Disposed += (_, _) => button.ContextMenuStrip?.Dispose();
		return button;
	}

	private static Control CreateLogScreen() {
		var screen = new LogScreen();
		screen.Load += (_, _) => screen.BeginInvoke((Action)(() => screen.Logger.Info("A sample message in the real LogScreen.")));
		return screen;
	}

	private static WinFormsActionWizard<LogOutputConfiguration> CreateLogWizard(params WinFormsWizardScreen<LogOutputConfiguration>[] screens) {
		var builder = new WinFormsWizardBuilder<LogOutputConfiguration>()
			.WithTitle("Sample logging configuration")
			.WithModel(new LogOutputConfiguration { Destination = LogOutputDestination.File, FilePath = "sample.log" })
			.OnFinished(_ => Task.FromResult(Result.Success))
			.OnCancelled(_ => Result.Success);
		foreach (var screen in screens)
			builder.AddScreen(screen);
		return builder.Build();
	}

	private static async Task ShowLogWizardAsync(Control owner, params WinFormsWizardScreen<LogOutputConfiguration>[] screens) {
		using var wizard = CreateLogWizard(screens);
		await wizard.Start(owner.FindForm());
	}

	private static Control CreateScreenHost() {
		var block = new ApplicationBlockBuilder()
			.WithName("Catalog example")
			.AddScreen<ControlCatalogHostedScreen>("Example screen")
			.Build();
		var host = new FormsUI.ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		host.Disposed += (_, _) => block.Dispose();
		host.InitializeScreens(new[] { block });
		host.ActivateScreen(block, typeof(ControlCatalogHostedScreen));
		return host;
	}

	private static Control CreateMainForm(MainForm form) {
		var block = new ApplicationBlockBuilder()
			.WithName("Catalog shell example")
			.AddScreen<ControlCatalogHostedScreen>("Editable native screen")
			.Build();
		form.Disposed += (_, _) => block.Dispose();
		form.ScreenHost.InitializeScreens(new[] { block });
		form.ScreenHost.ActivateScreen(block, typeof(ControlCatalogHostedScreen));
		return form;
	}

	private static Control CreateScreenTabs() {
		var tabs = new ApplicationScreenTabControl();
		tabs.TabPages.Add(new TabPage("First native tab"));
		tabs.TabPages.Add(new TabPage("Second native tab"));
		tabs.TabPages[0].Controls.Add(new TextBox { Dock = DockStyle.Top, Text = "Edit the first tab" });
		tabs.TabPages[1].Controls.Add(new TextBox { Dock = DockStyle.Top, Text = "Edit the second tab" });
		return tabs;
	}

	private static async Task ShowDetachedScreenAsync(Control owner) {
		using var block = new ApplicationBlockBuilder()
			.WithName("Detached catalog example")
			.AddScreen<ControlCatalogHostedScreen>("Native detached screen")
			.Build();
		using var host = new FormsUI.ApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		host.InitializeScreens(new[] { block });
		var screen = await host.OpenScreenAsync(block, block.ScreenDefinitions.Single());
		await host.ShowScreenUndockedAsync(screen);
		var form = screen.FindForm();
		var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		form.Disposed += (_, _) => closed.TrySetResult();
		form.Owner = owner.FindForm();
		await closed.Task;
	}

	// Expose the designer-owned nonvisual menu to the catalog while retaining its normal screen ownership.
	private static Control CreateGridConfigurationMenu() {
		var screen = new VisualGridTestScreen();
		screen.Tag = screen.Controls.Find("_crudGrid", true).OfType<VisualGrid>().Single().HeaderContextMenuStrip;
		return screen;
	}

	private static VisualGridColumn<Employee> EmployeeColumn(string property, string caption, Func<Employee, object> value) {
		return new VisualGridColumn<Employee> { PropertyName = property, ColumnName = caption, PropertyValue = value, DisplayType = CrudCellDisplayType.Text, DataType = typeof(string) };
	}

	private static Control CreateMultiSelectionButton() {
		var button = new MultiSelectionButton { Text = "Choose an action", Height = 36 };
		button.ButtonsItems.Add(new SubButtonItem("First action", (_, _) => button.Text = "First action selected"));
		button.ButtonsItems.Add(new SubButtonItem("Second action", (_, _) => button.Text = "Second action selected"));
		return button;
	}

	private static async Task ShowOptionsAsync(Control owner) {
		var application = ((ApplicationScreen)owner).Block.Parent;
		using var dialog = new ApplicationSettingsDialog(application, Sphere10Framework.Instance.ServiceProvider) { Text = "ApplicationSettingsDialog — Options" };
		await dialog.ShowDialogAsync(owner);
	}

	private static async Task ShowLoggingConfigurationAsync(Control owner) {
		using var selector = new LogOutputSelectorControl();
		await selector.ShowConfigurationAsync(owner);
		await selector.DisposeAsync();
	}

	private static Task ShowDropDownAsync(Control owner) {
		using var content = new TextBox { Multiline = true, Text = "Edit this dropdown's content", Size = new Size(320, 180) };
		using var dropdown = new DropDownForm(content, owner, owner.FindForm());
		dropdown.ShowDropDown();
		return Task.CompletedTask;
	}

	private static async Task ShowExpandingCircleAsync(Control owner) {
		using var circle = new ExpandingCircle { Enabled = true };
		var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		circle.FormClosed += (_, _) => closed.TrySetResult();
		circle.Location = owner.PointToScreen(new Point(owner.Width / 2 - circle.Width / 2, owner.Height / 2 - circle.Height / 2));
		circle.Show(owner.FindForm());
		circle.StartExpanding();
		await closed.Task;
	}

	private static async Task ShowAlphaFormAsync(Control owner) {
		using var image = CreateImage(Color.SteelBlue);
		using var form = new PerPixelAlphaForm();
		using var timer = new System.Windows.Forms.Timer { Interval = 5000 };
		form.SetBitmap(image);
		form.Click += (_, _) => form.Close();
		timer.Tick += (_, _) => form.Close();
		timer.Start();
		await form.ShowDialogAsync(owner);
	}

	private static Control CreateSplashScreen() {
		var splash = new SplashScreen();
		splash.UpdateTitle("Sphere10 control catalog");
		splash.UpdateSubTitle("Sample progress display — close to return to the catalog");
		splash.Controls.Add(new Button { Text = "Close sample", Dock = DockStyle.Bottom, DialogResult = DialogResult.OK });
		return splash;
	}

	private static Control CreateChoiceSelector() {
		var strip = new ToolStrip();
		var selector = new ToolStripChoiceSelector();
		var result = new ToolStripLabel();
		selector.SetItems(new object[] { "First choice", "Second choice", "Third choice" });
		selector.SelectedIndexChanged += (_, _) => result.Text = "Selected: " + selector.SelectedItem;
		selector.SelectedIndex = 0;
		strip.Items.Add(selector);
		strip.Items.Add(result);
		return strip;
	}

	private static Control CreateSidebarToggle() {
		var panel = new Panel();
		var strip = new ToolStrip { Dock = DockStyle.Top };
		var toggle = new SidebarToggleButton();
		var sidebar = new TextBox { Multiline = true, Text = "Sidebar content", Dock = DockStyle.Left, Width = 160 };
		toggle.CheckedChanged += (_, _) => sidebar.Visible = toggle.Checked;
		strip.Items.Add(toggle);
		panel.Controls.Add(sidebar);
		panel.Controls.Add(strip);
		return panel;
	}

	private static Control CreateLoadingItem() {
		var strip = new ToolStrip();
		var loading = new LoadingCircleToolStripMenuItem();
		var toggle = new ToolStripButton("Animate") { CheckOnClick = true };
		toggle.CheckedChanged += (_, _) => loading.LoadingCircleControl.Active = toggle.Checked;
		strip.Items.Add(loading);
		strip.Items.Add(toggle);
		return strip;
	}

	private static Control CreatePlaceholder() {
		var extender = new PlaceHolderTextExtender();
		var editor = new TextBox();
		var panel = new UserControlEx { Tag = extender };
		editor.Dock = DockStyle.Top;
		panel.Controls.Add(editor);
		panel.Load += (_, _) => extender.SetPlaceHolderText(editor, "PlaceHolderTextExtender — type here");
		panel.Disposed += (_, _) => extender.Dispose();
		return panel;
	}

	private static Control CreateBitmapButton() {
		var colors = new[] { Color.SteelBlue, Color.DarkOrange, Color.SeaGreen, Color.MediumPurple, Color.Gray };
		var states = new[] { "Normal", "Pressed", "Focused", "Hover", "Disabled" };
		var image = new Bitmap(180 * states.Length, 54);
		using (var graphics = Graphics.FromImage(image)) {
			for (var index = 0; index < states.Length; index++) {
				using var fill = new SolidBrush(colors[index]);
				graphics.FillRectangle(fill, index * 180, 0, 180, 54);
				graphics.DrawString(states[index], SystemFonts.MessageBoxFont, Brushes.White, index * 180 + 16, 18);
			}
		}
		var panel = new UserControlEx();
		var button = new BitmapButton { Image = image, Size = new Size(180, 54), Location = new Point(12, 12) };
		var disabled = new CheckBox { Text = "Disable button", AutoSize = true, Location = new Point(12, 82) };
		disabled.CheckedChanged += (_, _) => button.Enabled = !disabled.Checked;
		panel.Controls.Add(button);
		panel.Controls.Add(disabled);
		panel.Disposed += (_, _) => image.Dispose();
		return panel;
	}

	private static Control CreateButtonTextBox() {
		var editor = new ButtonTextBox { Text = "Editable text with an embedded button" };
		editor.Button.Text = "Clear";
		editor.ButtonClick += (_, _) => editor.Clear();
		return editor;
	}

	private static Control CreateValidatedEditor() {
		var editor = new ValidatedTextBoxButton { Value = "Edit a value" };
		editor.Button.Text = "Clear";
		editor.Button.Click += (_, _) => editor.Value = string.Empty;
		return editor;
	}

	private static Control CreateTypeEditor() {
		var validator = new Sphere10.Framework.ComponentModel.TypeConverterValidator(typeof(Color));
		var editor = new TextBoxUITypeEditor { Validator = validator, Value = Color.SteelBlue };
		editor.Disposed += (_, _) => validator.Dispose();
		return editor;
	}

	private static Control CreateTextLog() {
		var log = new TextLog { Text = "TextLog's actual editable text surface.\r\nTry typing and selecting text here." };
		return log;
	}

	private static Control CreateRadioGroups() {
		var panel = new RadioButtonPanel();
		var first = new RadioGroupBox { Text = "First profile", DisableChildrenIfUnchecked = true, Size = new Size(300, 110), Location = new Point(12, 12), Padding = new Padding(12, 28, 12, 12) };
		var second = new RadioGroupBox { Text = "Second profile", DisableChildrenIfUnchecked = true, Size = new Size(300, 110), Location = new Point(12, 136), Padding = new Padding(12, 28, 12, 12) };
		var neither = new RadioButton { Text = "Neither profile", AutoSize = true, Location = new Point(12, 264) };
		first.Controls.Add(new TextBox { Dock = DockStyle.Top, Text = "First profile draft" });
		second.Controls.Add(new TextBox { Dock = DockStyle.Top, Text = "Second profile draft" });
		panel.Controls.Add(first);
		panel.Controls.Add(second);
		panel.Controls.Add(neither);
		first.Checked = true;
		return panel;
	}
}

