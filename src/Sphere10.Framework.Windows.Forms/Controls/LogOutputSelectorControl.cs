// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application.UI;

using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Selects optional file, application, screen or standard-stream logging through a wizard.</summary>
[DefaultEvent(nameof(LoggingStateChanged))]
public partial class LogOutputSelectorControl : UserControlEx, IAsyncDisposable {
	/// <summary>Raised on the owner UI thread after configuration validation and before output IO. Throw to reject the proposed configuration.</summary>
	public event EventHandlerEx<LogOutputConfiguration> LoggingStarting;
	public event EventHandlerEx LoggingStateChanged;
	public event EventHandlerEx<Exception> LoggingFailed;

	private readonly NoOpLogger _inactiveLogger = new();
	private readonly int _ownerThreadId;
	private LogOutputConfiguration _configuration = new();
	private LogOutputSession _activeSession;
	private Exception _lastError;
	private Exception _pendingFailure;
	private int _failureNotificationQueued;
	private int _notificationsDisposed;
	private bool _updatingControls;
	private bool _configuring;
	private bool _detailsOpen;
	private bool _hasConfiguration;
	private bool _initialized;
	private bool _lastNotifiedLoggingState;
	private readonly List<LogScreen> _logScreens = new();
	private LogScreen _activeLogScreen;
	private WinFormsApplicationScreenHost _standaloneScreenHost;
	private Task _pendingDrain = Task.CompletedTask;
	private volatile bool _stopping;

	public LogOutputSelectorControl() {
		_ownerThreadId = Environment.CurrentManagedThreadId;
		InitializeComponent();
		_initialized = true;
		UpdateUiState();
	}

	/// <summary>The borrowed application logger offered by the wizard. The control never changes its options or disposes it.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ILogger ApplicationLogger { get; set; }

	/// <summary>An optional borrowed host, useful when the selector is contributed through a ToolStripControlHost.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IWinFormsApplicationScreenHost ScreenHost { get; set; }

	/// <summary>The source name displayed as Log Output - NAME. Empty or generic values use the owning screen or window.</summary>
	[Category("Appearance"), DefaultValue("Log Output")]
	public string ScreenTitle { get; set; } = "Log Output";

	/// <summary>The current selected logger, or an actual NoOpLogger when inactive. Fetch again after a state change to use the new output.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ILogger Logger {
		get {
			var session = Volatile.Read(ref _activeSession);
			return session?.IsLogging == true ? session : _inactiveLogger;
		}
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool IsLogging => Volatile.Read(ref _activeSession)?.IsLogging == true;

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool HasConfiguration => _hasConfiguration;

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Exception LastError => Volatile.Read(ref _lastError);

	/// <summary>Queues an information entry without file IO or UI dispatch waits on the caller. Returns false while off or when the bounded queue is full.</summary>
	public bool EnqueueInfo(string message) => Volatile.Read(ref _activeSession)?.EnqueueInfo(message) == true;

	/// <summary>Queues a record with its own capture timestamp, bypassing the optional processing-time prefix when requested.</summary>
	public bool EnqueueInfo(string message, bool includesTimestamp) => Volatile.Read(ref _activeSession)?.EnqueueInfo(message, includesTimestamp) == true;

	/// <summary>Completes when all entries accepted before this call have been written. It requires no UI continuation.</summary>
	public Task FlushLoggingAsync() {
		var session = Volatile.Read(ref _activeSession);
		var preceding = Volatile.Read(ref _pendingDrain);
		return session == null ? preceding : Task.WhenAll(preceding, session.FlushAsync());
	}

	/// <summary>A defensive copy of the selected options. Setting options does not open output; stop active logging before changing them.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public LogOutputConfiguration Configuration {
		get {
			var configuration = _configuration.Clone();
			var session = Volatile.Read(ref _activeSession);
			if (session?.IsLogging == true)
				configuration.Options = session.Options;
			return configuration;
		}
		set {
			EnsureOwnerThread();
			Guard.Ensure(!IsLogging && !_configuring, "Stop logging before replacing its configuration.");
			Guard.ArgumentNotNull(value, nameof(value));
			_configuration = value.Clone();
			_hasConfiguration = false;
			UpdateUiState();
		}
	}

	/// <summary>Starts an explicitly configured output without showing a wizard. Call on the creating UI thread.</summary>
	public void StartLogging() => StartLogging(_configuration);

	public void StartLogging(LogOutputConfiguration configuration) {
		EnsureOwnerThread();
		Guard.Ensure(!Tools.Runtime.IsDesignMode, "Logging cannot start on the design surface.");
		NotifyPendingFailure();
		Guard.Ensure(!IsLogging, "Stop logging before starting another output.");
		Guard.Ensure(!_stopping, "Wait for the previous logging output to finish closing.");
		Guard.ArgumentNotNull(configuration, nameof(configuration));
		try {
			var normalized = configuration.ValidateAndNormalize();
			LoggingStarting?.Invoke(normalized.Clone());
			var session = CreateSession(normalized);
			session.LoggingFailed += OnLoggerFailed;
			Interlocked.Exchange(ref _activeSession, session)?.Dispose();
			_configuration = normalized;
			_hasConfiguration = true;
			_rememberedEnabled = true;
			Volatile.Write(ref _lastError, null);
			var preceding = Volatile.Read(ref _pendingDrain);
			if (preceding.IsCompleted) {
				_ = preceding.Exception;
				Volatile.Write(ref _pendingDrain, Task.CompletedTask);
			}
		} catch (Exception error) {
			Volatile.Write(ref _lastError, error);
			UpdateUiState();
			NotifyLoggingStateChanged();
			throw;
		}
		AcceptLogOutputSettings();
		UpdateUiState();
		NotifyLoggingStateChanged();
	}

	/// <summary>Joins outstanding writes before closing owned file output. Disables future borrowed-output writes without waiting for external callbacks already dispatched.</summary>
	public void StopLogging() {
		EnsureOwnerThread();
		var session = DetachSession(userStopped: true, out var stateError);
		session?.Dispose();
		if (stateError != null)
			ExceptionDispatchInfo.Capture(stateError).Throw();
	}

	/// <summary>Turns output off, drains accepted queued entries and closes owned file output without blocking the UI.</summary>
	public Task StopLoggingAsync() {
		EnsureOwnerThread();
		if (_stopping)
			return Volatile.Read(ref _pendingDrain);
		_stopping = true;
		DetachSession(userStopped: true, out var stateError);
		var completion = CompleteStopAsync(Volatile.Read(ref _pendingDrain), stateError);
		Volatile.Write(ref _pendingDrain, completion);
		return completion;
	}

	/// <summary>Disposes the control on its UI thread, preserving the remembered enabled choice. The returned drain needs no later UI continuation.
	/// Hosted log views remain available through their host; a standalone fallback host is owned by this control.</summary>
	public ValueTask DisposeAsync() {
		if (IsDisposed)
			return new ValueTask(Volatile.Read(ref _pendingDrain));
		EnsureOwnerThread();
		DetachSession(userStopped: false, out var stateError);
		var completion = Task.WhenAll(Volatile.Read(ref _pendingDrain),
			stateError == null ? Task.CompletedTask : Task.FromException(stateError));
		Volatile.Write(ref _pendingDrain, completion);
		Dispose();
		return new ValueTask(Volatile.Read(ref _pendingDrain));
	}

	private LogOutputSession DetachSession(bool userStopped, out Exception stateError) {
		NotifyPendingFailure();
		_configuration = Configuration;
		var enabledChoiceChanged = userStopped && _rememberedEnabled;
		if (userStopped)
			_rememberedEnabled = false;
		var session = CloseActiveSession();
		if (enabledChoiceChanged)
			AcceptLogOutputSettings();
		UpdateUiState();
		Tools.Exceptions.ExecuteCapturingException(() => NotifyLoggingStateChanged(), out stateError);
		return session;
	}

	private LogOutputSession CloseActiveSession() {
		var session = Volatile.Read(ref _activeSession);
		if (session == null)
			return null;
		// Publish the actual drain before removing the session or notifying consumers. A worker
		// flushing during this transition must see either the old session or its retained drain.
		var completion = Task.WhenAll(Volatile.Read(ref _pendingDrain), session.DisposeAsync());
		Volatile.Write(ref _pendingDrain, completion);
		Interlocked.Exchange(ref _activeSession, null);
		_activeLogScreen = null;
		session.LoggingFailed -= OnLoggerFailed;
		_ = completion.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None,
			TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		return session;
	}

	private async Task CompleteStopAsync(Task drain, Exception stateError) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await drain.ConfigureAwait(false);
		}).ConfigureAwait(false);
		error ??= stateError;
		_stopping = false;
		if (error != null)
			Volatile.Write(ref _lastError, error);
		if (Volatile.Read(ref _notificationsDisposed) == 0 && IsHandleCreated) {
			Tools.Exceptions.ExecuteCapturingException<InvalidOperationException>(() => BeginInvoke((Action)(() => {
				if (IsDisposed || Disposing)
					return;
				UpdateUiState();
				if (error != null)
					LoggingFailed?.Invoke(error);
			})), out var dispatchError);
		}
		if (error != null)
			ExceptionDispatchInfo.Capture(error).Throw();
	}

	/// <summary>Shows output, file/rolling, formatting and review steps. Cancellation never opens output.</summary>
	public async Task<bool> ConfigureAsync(IWin32Window owner = null) {
		EnsureOwnerThread();
		Guard.Ensure(!Tools.Runtime.IsDesignMode, "Logging cannot be configured on the design surface.");
		Guard.Ensure(!_configuring, "The logging wizard is already open.");
		_configuring = true;
		UpdateUiState();
		using var configurationScope = Tools.Scope.ExecuteOnDispose(() => {
			_configuring = false;
			if (!IsDisposed && !Disposing)
				UpdateUiState();
		});
		using var wizard = CreateConfigurationWizard(Configuration);
		var parent = owner as Form ?? (owner as Control)?.FindForm() ?? FindForm();
		return await wizard.Start(parent) == WizardResult.Success;
	}

	/// <summary>Shows current details and a Change button. Opening or cancelling this dialog does not interrupt active output.</summary>
	public async Task ShowConfigurationAsync(IWin32Window owner = null) {
		EnsureOwnerThread();
		Guard.Ensure(!Tools.Runtime.IsDesignMode, "Logging cannot be configured on the design surface.");
		Guard.Ensure(!_configuring && !_detailsOpen, "Logging configuration is already open.");
		_detailsOpen = true;
		UpdateUiState();
		using var dialogScope = Tools.Scope.ExecuteOnDispose(() => {
			_detailsOpen = false;
			if (!IsDisposed && !Disposing)
				UpdateUiState();
		});
		using var dialog = new LogOutputConfigurationDialog();
		dialog.Bind(this);
		var parent = owner as Form ?? (owner as Control)?.FindForm() ?? FindForm();
		await dialog.ShowDialogAsync(parent);
	}

	public string GetDetailsText() {
		var state = IsLogging ? "Logging Is On." : "Logging Is Off.";
		if (_hasConfiguration)
			state += Environment.NewLine + Configuration.Describe();
		else
			state += Environment.NewLine + "No logging output has been selected.";
		if (LastError != null)
			state += Environment.NewLine + "Last Output Error: " + LastError.Message;
		if (LastSettingsError != null)
			state += Environment.NewLine + "Preferences Could Not Be Saved Or Restored: " + LastSettingsError.Message;
		return state;
	}

	/// <summary>Creates the shared native wizard; override only to extend the existing workflow.</summary>
	protected virtual WinFormsActionWizard<LogOutputConfiguration> CreateConfigurationWizard(LogOutputConfiguration configuration) =>
		new WinFormsWizardBuilder<LogOutputConfiguration>()
			.WithTitle("Log Output")
			.WithModel(configuration)
			.AddScreen(new LogOutputDestinationWizardScreen { ApplicationAvailable = ApplicationLogger != null })
			.AddScreen(new LogOutputFormattingWizardScreen())
			.AddScreen(new LogOutputReviewWizardScreen())
			.OnFinished(async selected => {
				var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(() => ApplyConfigurationAsync(selected));
				if (error != null)
					LoggingFailed?.Invoke(error);
				return error == null ? Result.Success : Result.Error(error.Message);
			})
			.OnCancelled(_ => Result.Success)
			.Build();

	protected override void OnHandleCreated(EventArgs e) {
		base.OnHandleCreated(e);
		if (_initialized)
			NotifyPendingFailure();
	}

	protected override void OnHandleDestroyed(EventArgs e) {
		Interlocked.Exchange(ref _failureNotificationQueued, 0);
		base.OnHandleDestroyed(e);
	}

	private void EnsureOwnerThread() {
		Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThreadId, "Configure and start/stop logging on the control's creating UI thread.");
		ObjectDisposedException.ThrowIf(IsDisposed || Disposing, this);
	}

	private void UpdateUiState() {
		_updatingControls = true;
		using var controlsScope = Tools.Scope.ExecuteOnDispose(() => _updatingControls = false);
		_loggingCheckBox.Checked = IsLogging;
		_loggingCheckBox.Enabled = !_configuring && !_detailsOpen && !_stopping;
		_configureButton.Enabled = !_configuring && !_detailsOpen && !_stopping;
		_toolTip.SetToolTip(_loggingCheckBox, GetDetailsText());
	}

	private async void LoggingCheckedChanged(object sender, EventArgs e) {
		if (_updatingControls || Tools.Runtime.IsDesignMode)
			return;
		var requested = _loggingCheckBox.Checked;
		UpdateUiState();
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (requested) {
				if (_hasConfiguration)
					StartLogging();
				else
					await ConfigureAsync();
			} else
				await StopLoggingAsync();
		});
		if (error != null) {
			Volatile.Write(ref _lastError, error);
			UpdateUiState();
			LoggingFailed?.Invoke(error);
			await DialogEx.ShowAsync(FindForm(), SystemIconType.Error, "Log Output", error.Message, "OK");
		}
	}

	private async void ConfigureButtonClick(object sender, EventArgs e) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await ShowConfigurationAsync();
		});
		if (error != null) {
			Volatile.Write(ref _lastError, error);
			UpdateUiState();
			LoggingFailed?.Invoke(error);
			await DialogEx.ShowAsync(FindForm(), SystemIconType.Error, "Log Output", error.Message, "OK");
		}
	}

	private void OnLoggerFailed(Exception error) {
		if (Volatile.Read(ref _notificationsDisposed) != 0)
			return;
		Volatile.Write(ref _lastError, error);
		Interlocked.Exchange(ref _pendingFailure, error);
		if (Environment.CurrentManagedThreadId == _ownerThreadId)
			NotifyPendingFailure();
		else if (IsHandleCreated && Interlocked.CompareExchange(ref _failureNotificationQueued, 1, 0) == 0) {
			Tools.Exceptions.ExecuteCapturingException<InvalidOperationException>(() => BeginInvoke((Action)NotifyPendingFailure), out var dispatchError);
			if (dispatchError != null)
				Interlocked.Exchange(ref _failureNotificationQueued, 0);
		}
	}

	private void NotifyPendingFailure() {
		Interlocked.Exchange(ref _failureNotificationQueued, 0);
		if (Volatile.Read(ref _notificationsDisposed) != 0 || IsDisposed || Disposing)
			return;
		var error = Interlocked.Exchange(ref _pendingFailure, null);
		if (error == null)
			return;
		UpdateUiState();
		NotifyLoggingStateChanged();
		LoggingFailed?.Invoke(error);
	}

	private void NotifyLoggingStateChanged(bool loggerReplaced = false) {
		var active = IsLogging;
		if (active == _lastNotifiedLoggingState && !loggerReplaced || Volatile.Read(ref _notificationsDisposed) != 0)
			return;
		_lastNotifiedLoggingState = active;
		LoggingStateChanged?.Invoke();
	}

	private async Task ApplyConfigurationAsync(LogOutputConfiguration configuration) {
		if (!IsLogging) {
			StartLogging(configuration);
			return;
		}
		// Validate and let the consumer veto before interrupting its accepted output.
		LogOutputConfiguration normalized;
		try {
			normalized = configuration.ValidateAndNormalize();
			LoggingStarting?.Invoke(normalized.Clone());
		} catch (Exception error) {
			Volatile.Write(ref _lastError, error);
			UpdateUiState();
			throw;
		}
		var previousConfiguration = Configuration;
		CloseActiveSession();
		var drain = Volatile.Read(ref _pendingDrain);
		try {
			await drain;
			if (OutputLifetimeEnded)
				return;
			var session = CreateSession(normalized);
			session.LoggingFailed += OnLoggerFailed;
			Interlocked.Exchange(ref _activeSession, session);
		} catch (Exception error) {
			if (OutputLifetimeEnded) {
				CloseActiveSession();
				return;
			}
			// Cancellation never reaches this boundary. A failed Finish recovers the old output when possible.
			Volatile.Write(ref _lastError, error);
			try {
				var recovered = CreateSession(previousConfiguration);
				recovered.LoggingFailed += OnLoggerFailed;
				Interlocked.Exchange(ref _activeSession, recovered)?.Dispose();
			} catch (Exception recoveryError) {
				Volatile.Write(ref _lastError, new AggregateException("The changed output failed and the previous output could not be reopened.", error, recoveryError));
			}
			if (OutputLifetimeEnded) {
				CloseActiveSession();
				return;
			}
			UpdateUiState();
			NotifyLoggingStateChanged(loggerReplaced: true);
			throw;
		}
		if (OutputLifetimeEnded) {
			CloseActiveSession();
			return;
		}
		_configuration = normalized;
		_hasConfiguration = true;
		_rememberedEnabled = true;
		Volatile.Write(ref _lastError, null);
		AcceptLogOutputSettings();
		UpdateUiState();
		NotifyLoggingStateChanged(loggerReplaced: true);
	}

	private void DisposeLogger() {
		Volatile.Write(ref _notificationsDisposed, 1);
		Interlocked.Exchange(ref _pendingFailure, null);
		CloseActiveSession()?.Dispose();
		foreach (var screen in _logScreens)
			screen.ScreenDestroyed -= OutputScreenDestroyed;
		_logScreens.Clear();
		_activeLogScreen = null;
		_standaloneScreenHost?.Dispose();
		_standaloneScreenHost = null;
	}

	private bool OutputLifetimeEnded => Volatile.Read(ref _notificationsDisposed) != 0 || IsDisposed || Disposing;

	private LogOutputSession CreateSession(LogOutputConfiguration configuration) {
		if (configuration.Destination != LogOutputDestination.Screen)
			return LogOutputSession.Create(configuration, ApplicationLogger);
		var host = ResolveScreenHost();
		var title = ResolveLogScreenTitle(host);
		if (host == null || host.ScreenMode != ScreenMode.MultiView) {
			_standaloneScreenHost ??= new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
			host = _standaloneScreenHost;
		}
		var screen = new LogScreen { Title = title };
		if (!ReferenceEquals(host, _standaloneScreenHost))
			screen.Block = ResolveScreenBlock(host);
		var shown = false;
		using var failedScreen = Tools.Scope.ExecuteOnDispose(() => {
			if (!shown)
				screen.Dispose();
		});
		Guard.Ensure(host.ShowScreenUndocked(screen), "The screen host did not accept the log output screen.");
		if (ReferenceEquals(host, _standaloneScreenHost) && screen.FindForm() is { } window && FindForm() is { } owner)
			window.Owner = owner;
		var session = LogOutputSession.Create(configuration, ApplicationLogger, screen.Logger);
		screen.ScreenDestroyed += OutputScreenDestroyed;
		_logScreens.Add(screen);
		_activeLogScreen = screen;
		shown = true;
		return session;
	}

	private string ResolveLogScreenTitle(IWinFormsApplicationScreenHost host) {
		var source = NormalizeScreenSourceName(ScreenTitle);
		for (Control control = this; source == null && control != null; control = control.Parent)
			if (control is WinFormsApplicationScreen screen)
				source = NormalizeScreenSourceName(screen.Title);
		var form = FindForm();
		if (source == null && form is WinFormsApplicationScreenForm detached)
			source = NormalizeScreenSourceName(detached.Screen.Title);
		if (source == null && host?.ActiveScreen is { } active && active is not LogScreen)
			source = NormalizeScreenSourceName(active.Title);
		source ??= NormalizeScreenSourceName(form?.Text);
		return "Log Output - " + (source ?? "Capture");
	}

	private static string NormalizeScreenSourceName(string title) {
		var source = title?.Trim();
		const string prefix = "Log Output - ";
		while (source?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true)
			source = source[prefix.Length..].Trim();
		return string.IsNullOrWhiteSpace(source) || source.Equals("Log Output", StringComparison.OrdinalIgnoreCase)
			|| source.Equals("Log Output -", StringComparison.OrdinalIgnoreCase) ? null : source;
	}

	private IWinFormsApplicationBlock ResolveScreenBlock(IWinFormsApplicationScreenHost host) {
		for (Control control = this; control != null; control = control.Parent) {
			if (control is WinFormsApplicationScreen screen && ReferenceEquals(screen.Host, host) && screen.Block != null)
				return screen.Block;
		}
		return host.ActiveScreen?.Block ?? host.ActiveBlock;
	}

	private IWinFormsApplicationScreenHost ResolveScreenHost() {
		if (ScreenHost != null)
			return ScreenHost;
		for (Control control = this; control != null; control = control.Parent) {
			if (control is WinFormsApplicationScreen { Host: { } host })
				return host;
			if (control is IWinFormsApplicationScreenHost owningHost)
				return owningHost;
		}
		// ToolStripControlHost controls belong to the strip, which can live on the main frame
		// or a detached screen window instead of beneath the contributing screen.
		var form = FindForm();
		if (form is WinFormsApplicationScreenForm detached)
			return detached.Screen.Host;
		if (form is MainForm main)
			return main.ScreenHost;
		return FindScreenHost(form);
	}

	private static IWinFormsApplicationScreenHost FindScreenHost(Control parent) {
		if (parent == null)
			return null;
		if (parent is IWinFormsApplicationScreenHost host)
			return host;
		foreach (Control child in parent.Controls) {
			if (FindScreenHost(child) is { } found)
				return found;
		}
		return null;
	}

	private async void OutputScreenDestroyed(object sender, EventArgs args) {
		var screen = (LogScreen)sender;
		screen.ScreenDestroyed -= OutputScreenDestroyed;
		_logScreens.Remove(screen);
		if (ReferenceEquals(screen, _activeLogScreen))
			await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => await StopLoggingAsync().ConfigureAwait(false)).ConfigureAwait(false);
	}

	private LogOutputSelectorSettings _outputSettings;
	private ISettingsProvider _outputSettingsProvider;
	private string _settingsKey = string.Empty;
	private bool _restoreCompleted;
	private bool _restoringSettings;
	private bool _settingsDirty;
	private bool _rememberedEnabled;
	private Exception _lastSettingsError;

	[Category("Behavior"), DefaultValue("")]
	[Description("Optional stable UserSettings ID. Otherwise the owning screen/control type and named child path identify remembered logging preferences.")]
	public string SettingsKey {
		get => _settingsKey;
		set {
			value ??= string.Empty;
			if (_settingsKey == value)
				return;
			EnsureOwnerThread();
			Guard.Ensure(!_restoreCompleted && _outputSettings == null, "Set SettingsKey before restoring or accepting logging preferences.");
			_settingsKey = value;
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Exception LastSettingsError => _lastSettingsError;

	/// <summary>Restores optional preferences after the owner has assigned its application logger and preflight/failure handlers.
	/// Returns false when no stable identity/provider is available yet, allowing the owner to retry after initialization.</summary>
	public bool RestoreSettings() {
		EnsureOwnerThread();
		if (DesignMode || Tools.Runtime.IsDesignMode)
			return false;
		if (_restoreCompleted)
			return true;
		var key = ResolveLogSettingsKey();
		if (key == null || !TryGetSettingsProvider(out var provider))
			return false;
		_restoringSettings = true;
		using var restoreScope = Tools.Scope.ExecuteOnDispose(() => _restoringSettings = false);
		try {
			var exists = provider.ContainsSetting(typeof(LogOutputSelectorSettings), key);
			_outputSettingsProvider = provider;
			_outputSettings = provider.Get<LogOutputSelectorSettings>(key);
			_restoreCompleted = true;
			// A choice already accepted by this instance takes precedence over older stored preferences.
			if (_hasConfiguration || !exists || !_outputSettings.HasConfiguration || _outputSettings.Configuration?.RememberAfterClose != true)
				return true;
			_configuration = _outputSettings.Configuration.ValidateOptionsAndNormalize();
			_hasConfiguration = true;
			_rememberedEnabled = _outputSettings.IsLogging;
			if (_rememberedEnabled)
				Tools.Exceptions.Execute(StartLogging, error => LoggingFailed?.Invoke(error));
			UpdateUiState();
			return true;
		} catch (Exception error) {
			_restoreCompleted = true;
			RecordSettingsFailure(error, "Could not restore logging preferences.");
			if (_outputSettings == null)
				Tools.Exceptions.Execute(() => {
					_outputSettings = (LogOutputSelectorSettings)provider.NewSetting(typeof(LogOutputSelectorSettings), key);
				}, recoveryError => RecordSettingsFailure(recoveryError, "Could not initialize logging preferences."));
			UpdateUiState();
			return true;
		}
	}

	private void AcceptLogOutputSettings() {
		if (_restoringSettings || DesignMode || Tools.Runtime.IsDesignMode)
			return;
		_settingsDirty = true;
		SaveLogOutputSettings();
	}

	private void SaveLogOutputSettings() {
		if (!_settingsDirty || _restoringSettings || DesignMode || Tools.Runtime.IsDesignMode)
			return;
		var key = ResolveLogSettingsKey();
		if (key == null)
			return;
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			if (_outputSettings == null) {
				if (!TryGetSettingsProvider(out var provider))
					return;
				_outputSettings = provider.Get<LogOutputSelectorSettings>(key);
				_outputSettingsProvider = provider;
			}
			_restoreCompleted = true;
			if (!_hasConfiguration || !_configuration.RememberAfterClose) {
				_outputSettingsProvider.DeleteSetting(_outputSettings);
				_outputSettings = null;
				_outputSettingsProvider = null;
			} else {
				_outputSettings.HasConfiguration = true;
				_outputSettings.IsLogging = _rememberedEnabled;
				_outputSettings.Configuration = Configuration;
				_outputSettings.Save();
			}
			_settingsDirty = false;
			_lastSettingsError = null;
		}, out var error);
		if (error != null) {
			RecordSettingsFailure(error, "Could not save logging preferences.");
		}
	}

	private static bool TryGetSettingsProvider(out ISettingsProvider provider) {
		ISettingsProvider resolved = null;
		Tools.Exceptions.ExecuteCapturingException<InvalidOperationException>(() => resolved = UserSettings.Provider, out var error);
		provider = resolved;
		return error == null;
	}

	private string ResolveLogSettingsKey() {
		if (!string.IsNullOrWhiteSpace(_settingsKey))
			return _settingsKey;
		var path = new List<string>();
		for (var control = (Control)this; control != null; control = control.Parent) {
			if (control is WinFormsApplicationScreen || control != this && control is UserControl && control.GetType() != typeof(UserControl))
				return $"{control.GetType().FullName}.{string.Join(".", path)}";
			if (control is Form form) {
				if (form.GetType() == typeof(Form) && string.IsNullOrWhiteSpace(form.Name))
					return null;
				return $"{form.GetType().FullName}.{form.Name}.{string.Join(".", path)}";
			}
			if (!string.IsNullOrWhiteSpace(control.Name))
				path.Insert(0, control.Name);
		}
		return null;
	}

	private void RecordSettingsFailure(Exception error, string message) {
		_lastSettingsError = error;
		Tools.Exceptions.ExecuteCapturingException<System.Exception>(() => {
			(ApplicationLogger ?? SystemLog.Logger).Exception(error, message);
		}, out var capturedError);
	}
}
