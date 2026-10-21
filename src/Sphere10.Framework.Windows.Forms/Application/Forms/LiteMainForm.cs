// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

public partial class LiteMainForm : ApplicationForm, IMainForm {
	public event EventHandler FirstActivation;
	public event EventHandler FirstTimeExecutedBySystemEvent;
	public event EventHandler NotFirstTimeExecutedBySystemEvent;
	public event EventHandler FirstTimeExecutedByUserEvent;
	public event EventHandler NotFirstTimeExecutedByUserEvent;
	public event EventHandler<CancelEventArgs> ApplicationExitingEvent;

	private bool _confirmingExit;
	private bool _applicationExiting;
	private bool _exitConfirmed;
	private bool _forceExitConfirmed;
	private bool _runningShutdown;
	private bool _finalClose;
	private bool _nativeCloseApproved;
	private Func<Task> _approvedShutdownAsync;
	private TaskCompletionSource<(bool Closed, Exception ShutdownError)> _approvedCloseCompletion;
	private SynchronizationContext _approvedCloseContext;
	private FormCloseAction? _closeActionBeforeExit;
	private CancellationToken _exitConfirmationCancellationToken;

	public LiteMainForm() {
		System.Windows.Forms.Application.ThreadException += ApplicationOnThreadException;
		InitializeComponent();
		Nagged = false;
		NumberActivations = 0;
	}

	protected bool SuppressExitConfirmation { get; set; }

	protected bool ScreenExitPermissionsChecked { get; private set; }

	internal IApplication Application { get; set; }

	#region Form Methods

	protected virtual async void OnFirstActivated() {
		if (!Tools.Runtime.IsDesignMode)
			await EnforceLicense();
	}

	// Shown every time window becomes active window
	protected override void OnActivated(EventArgs e) {
		NumberActivations++;
		base.OnActivated(e);
		if (!Nagged) {
			Nagged = true;
			if (!ApplicationExiting)
				FireFirstActivatedEvent();
		}
	}


	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (!Tools.Runtime.IsDesignMode) {

			//// initialize local members

			#region Fire First Time Use Events

			var productUsageServices = Sphere10Framework.Instance.ServiceProvider.GetService<IProductUsageServices>();
			var usageInfo = productUsageServices.ProductUsageInformation;
			if (usageInfo.NumberOfUsesBySystem == 1) {
				FireFirstTimeExecutedBySystemEvent();
			} else {
				FireNotFirstTimeExecutedBySystemEvent();
			}

			if (usageInfo.NumberOfUsesByUser == 1) {
				FireFirstTimeExecutedByUserEvent();
			} else {
				FireNotFirstTimeExecutedByUserEvent();
			}

			#endregion

		}
	}

	protected sealed override async void OnFormClosing(FormClosingEventArgs Args) {
		// Native observers approved this close before asynchronous shutdown started.
		if (_finalClose)
			return;
		if (_runningShutdown) {
			Args.Cancel = true;
			return;
		}
		if (Application != null && _confirmingExit && !_exitConfirmed) {
			Args.Cancel = true;
			return;
		}
		if (_exitConfirmed && _nativeCloseApproved)
			_nativeCloseApproved = false;
		else
			base.OnFormClosing(Args);
		if (_forceExitConfirmed)
			Args.Cancel = false;
		if (Args.Cancel) {
			ApplicationExiting = false;
			RestoreCloseAction();
			_approvedCloseCompletion?.TrySetResult((false, null));
			return;
		}
		if (_exitConfirmed) {
			var shutdown = _approvedShutdownAsync;
			if (shutdown == null)
				return;
			Args.Cancel = true;
			_approvedShutdownAsync = null;
			var completion = _approvedCloseCompletion;
			var closeContext = _approvedCloseContext;
			_runningShutdown = true;
			var previouslyEnabled = Enabled;
			using var restoreShutdown = Tools.Scope.ExecuteOnDispose(() => {
				if (!IsDisposed)
					Enabled = previouslyEnabled;
				_runningShutdown = false;
				_finalClose = false;
			});
			try {
				Enabled = false;
				// Let the cancelled native close unwind before requesting the final close, even for synchronous tasks.
				await Task.Yield();
				var shutdownError = await Tools.Exceptions.ExecuteCapturingExceptionAsync(shutdown);
				_finalClose = true;
				Close();
				// Closing the last window can uninstall its context; finish the approved exit inline before its message loop ends.
				var completedContext = SynchronizationContext.Current;
				using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(completedContext));
				SynchronizationContext.SetSynchronizationContext(closeContext);
				completion.TrySetResult((IsDisposed, shutdownError));
			} catch (Exception error) {
				var completedContext = SynchronizationContext.Current;
				using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(completedContext));
				SynchronizationContext.SetSynchronizationContext(closeContext);
				completion.TrySetException(error);
			}
			return;
		}
		if (SuppressExitConfirmation && Application == null) {
			ApplicationExiting = true;
			FireApplicationExitingEvent(Args);
			ApplicationExiting = !Args.Cancel;
			return;
		}

		// WinForms cannot await a closing event. Cancel this attempt and close again after confirmation.
		Args.Cancel = true;
		if (_confirmingExit)
			return;
		_confirmingExit = true;
		ApplicationExiting = true;
		using var ConfirmationScope = Tools.Scope.ExecuteOnDispose(() => {
			_confirmingExit = false;
			_nativeCloseApproved = false;
			if (!IsDisposed) {
				_exitConfirmed = false;
				ApplicationExiting = false;
				RestoreCloseAction();
			}
		});
		try {
			_nativeCloseApproved = Application != null;
			await Task.Yield();
			if (Application != null) {
				await Application.ExitAsync(SuppressExitConfirmation);
				return;
			}
			if (!await ConfirmExitAsync() || IsDisposed)
				return;
			var Cancellation = new CancelEventArgs();
			FireApplicationExitingEvent(Cancellation);
			if (Cancellation.Cancel)
				return;
			_exitConfirmed = true;
			Close();
		} catch (OperationCanceledException) {
			// Application disposal or cancellation can withdraw a pending exit request.
		} catch (Exception Error) {
			ReportError(Error);
		}
	}

	protected virtual async Task<bool> ConfirmExitAsync()
		=> await DialogEx.ShowAsync(this, SystemIconType.Question, "Confirm", "Are you sure you want to exit?", _exitConfirmationCancellationToken, "&No", "&Yes") == DialogExResult.Button2;

	internal async Task<bool> ConfirmApplicationExitAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		var previousToken = _exitConfirmationCancellationToken;
		_exitConfirmationCancellationToken = cancellationToken;
		using var restoreToken = Tools.Scope.ExecuteOnDispose(() => _exitConfirmationCancellationToken = previousToken);
		return await ConfirmExitAsync().WaitAsync(cancellationToken);
	}

	internal bool CanApplicationExit() {
		var previouslyChecked = ScreenExitPermissionsChecked;
		ScreenExitPermissionsChecked = true;
		using var restorePermissions = Tools.Scope.ExecuteOnDispose(() => ScreenExitPermissionsChecked = previouslyChecked);
		var cancellation = new CancelEventArgs();
		FireApplicationExitingEvent(cancellation);
		return !cancellation.Cancel;
	}

	internal async Task<(bool Closed, Exception ShutdownError)> CloseFromApplicationAsync(bool force, Func<Task> shutdown) {
		if (IsDisposed)
			return (true, null);
		Guard.ArgumentNotNull(shutdown, nameof(shutdown));
		// Continuations must complete before the final native close can end its message loop.
		var completion = new TaskCompletionSource<(bool Closed, Exception ShutdownError)>();
		_approvedCloseCompletion = completion;
		_approvedCloseContext = SynchronizationContext.Current;
		_approvedShutdownAsync = shutdown;
		_exitConfirmed = true;
		_forceExitConfirmed = force;
		ApplicationExiting = true;
		_closeActionBeforeExit ??= CloseAction;
		CloseAction = FormCloseAction.Close;
		using var restoreExit = Tools.Scope.ExecuteOnDispose(() => {
			_approvedShutdownAsync = null;
			_approvedCloseCompletion = null;
			_approvedCloseContext = null;
			_forceExitConfirmed = false;
			if (!IsDisposed) {
				_exitConfirmed = false;
				ApplicationExiting = false;
				RestoreCloseAction();
			}
		});
		Close();
		return await completion.Task;
	}

	protected virtual void OnApplicationExiting(CancelEventArgs cancelEventArgs) {
	}

	protected virtual void OnFirstTimeExecutedBySystem() {
	}

	protected virtual void OnFirstTimeExecutedByUser() {
	}

	protected virtual void OnNotFirstTimeExecutedBySystem() {
	}

	protected virtual void OnNotFirstTimeExecutedByUser() {
	}

	#endregion

	#region Form Properties

	private int NumberActivations { get; set; }

	private bool Nagged { get; set; }

	#endregion

	#region IApplicationIconProvider Implementation

	public virtual Icon ApplicationIcon {
		get { return Sphere10Framework.Instance.GetAppIcon() ?? Icon; }
	}

	#endregion

	#region IUserInterfaceServices Implementation

	protected override void WndProc(ref Message m) {
		if (m.Msg == WinAPI.USER32.WM_QUERYENDSESSION) {
			// CloseActions Hide | Minimize will hold up session shutdown, and SystemEvents doesn't get fired!
			CloseAction = FormCloseAction.Close;
			SuppressExitConfirmation = true;
			if (Application != null) {
				var exit = Application.ExitAsync(true);
				m.Result = exit.IsCompletedSuccessfully && !exit.Result ? IntPtr.Zero : new IntPtr(1);
				_ = ObserveApplicationExit(exit);
				return;
			}
		}
		base.WndProc(ref m);
	}

	public virtual void Exit(bool force = false) {
		if (IsDisposed)
			return;
		if (Application != null) {
			_ = ObserveApplicationExit(Application.ExitAsync(force));
			return;
		}
		if (force)
			Sphere10Framework.Instance.TerminateApplication(0);
		if (IsHandleCreated)
			this.InvokeEx(ExitInternal);
		else
			ExitInternal();

		void ExitInternal() {
			// File > Exit also closes forms configured to hide or minimize with the window close button.
			_closeActionBeforeExit ??= CloseAction;
			CloseAction = FormCloseAction.Close;
			Close();
			if (!_confirmingExit && !IsDisposed)
				RestoreCloseAction();
		}
	}

	private void RestoreCloseAction() {
		if (_closeActionBeforeExit.HasValue) {
			CloseAction = _closeActionBeforeExit.Value;
			_closeActionBeforeExit = null;
		}
	}

	private async Task ObserveApplicationExit(Task<bool> exit) {
		try {
			await exit;
		} catch (OperationCanceledException) {
			// UI exit requests do not report expected cancellation as an application error.
		} catch (Exception error) {
			if (IsDisposed)
				SystemLog.Exception(error);
			else
				ReportError(error);
		}
	}

	public virtual bool ApplicationExiting {
		get => _applicationExiting || _confirmingExit;
		set => _applicationExiting = value;
	}

	public virtual string Status { get; set; }

	public virtual void ExecuteInUIFriendlyContext(Action function, bool executeAsync = false) {
		if (executeAsync) {
			BeginInvoke(function);
		} else {
			Invoke(function);
		}
	}

	public virtual Task ShowNagScreen(string nagMessage)
		=> this.InvokeAsyncEx(async _ => {
			var nagDialogInstance = Sphere10Framework.Instance.ServiceProvider.GetService<INagDialog>();
			if (WindowState == FormWindowState.Minimized)
				nagDialogInstance.StartPosition = FormStartPosition.CenterScreen;
			nagDialogInstance.NagMessage = nagMessage;
			await nagDialogInstance.ShowDialogAsync(this);
		});


	public virtual object PrimaryUIController {
		get { return this; }
	}

	#endregion

	#region IUserNotificationServices Implementation

	public virtual Task ShowSendCommentDialog()
		=> this.InvokeAsyncEx(_ => Sphere10Framework.Instance.ServiceProvider.GetService<IProductSendCommentsDialog>().ShowDialogAsync(this));

	public virtual Task ShowSubmitBugReportDialog()
		=> this.InvokeAsyncEx(_ => Sphere10Framework.Instance.ServiceProvider.GetService<IProductReportBugDialog>().ShowDialogAsync(this));

	public virtual Task ShowRequestFeatureDialog()
		=> this.InvokeAsyncEx(_ => Sphere10Framework.Instance.ServiceProvider.GetService<IProductRequestFeatureDialog>().ShowDialogAsync(this));

	public virtual Task ShowAboutBox()
		=> this.InvokeAsyncEx(async _ => {
			using var scope = Sphere10Framework.Instance.ServiceProvider.CreateScope();
			var about = scope.ServiceProvider.GetRequiredService<IAboutBox>();
			await about.ShowDialogAsync(this);
		});

	public virtual void ReportError(Exception error) {
		ExecuteInUIFriendlyContext(() => _ = ExceptionDialog.ShowAsync(this, error));
	}

	public virtual void ReportError(string msg) {
		ReportError("Unexpected Error", msg);
	}

	public virtual void ReportError(string title, string msg) {
		ExecuteInUIFriendlyContext(
			() =>
				MessageBox.Show(
					msg,
					title,
					MessageBoxButtons.OK,
					MessageBoxIcon.Error,
					MessageBoxDefaultButton.Button1
				)
		);
	}

	public virtual void ReportFatalError(string title, string msg) {
		ExecuteInUIFriendlyContext(
			() => {
				ReportError(title, msg);
				Exit(true);
			}
		);
	}

	public virtual void ReportInfo(string title, string msg) {
		ExecuteInUIFriendlyContext(
			async () => await DialogEx.ShowAsync(
				this,
				SystemIconType.Information,
				msg,
				title,
				MessageBoxButtons.OK,
				MessageBoxIcon.Information
			)
		);
	}

	public bool AskYN(string question) {
		// The synchronous interface must pump a modal message loop instead of blocking an async task.
		var Answer = false;
		this.InvokeEx(() => {
			Answer = MessageBox.Show(this, question, "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
		});
		return Answer;
	}

	#endregion

	#region Auxillary Methods

	private async Task EnforceLicense() {
		var licenseEnforcer = Sphere10Framework.Instance.ServiceProvider.GetService<IProductLicenseEnforcer>();
		await licenseEnforcer.EnforceLicense(false);
	}

	private void FireFirstActivatedEvent() {
		OnFirstActivated();
		if (FirstActivation != null) {
			FirstActivation(this, EventArgs.Empty);
		}
	}

	private void FireApplicationExitingEvent(CancelEventArgs cancelEvent) {
		OnApplicationExiting(cancelEvent);
		if (cancelEvent.Cancel)
			return;
		// Call each observer, if any one decides to cancel abort do not notify remaining observers
		if (ApplicationExitingEvent != null) {
			foreach (EventHandler<CancelEventArgs> exitHandler in ApplicationExitingEvent.GetInvocationList()) {
				exitHandler(this, cancelEvent);
				if (cancelEvent.Cancel) {
					break;
				}
			}
		}
	}

	private void FireFirstTimeExecutedBySystemEvent() {
		OnFirstTimeExecutedBySystem();
		if (FirstTimeExecutedBySystemEvent != null) {
			FirstTimeExecutedBySystemEvent(this, EventArgs.Empty);
		}
	}

	private void FireFirstTimeExecutedByUserEvent() {
		OnFirstTimeExecutedByUser();
		if (FirstTimeExecutedByUserEvent != null) {
			FirstTimeExecutedByUserEvent(this, EventArgs.Empty);
		}
	}

	private void FireNotFirstTimeExecutedBySystemEvent() {
		OnNotFirstTimeExecutedBySystem();
		if (NotFirstTimeExecutedBySystemEvent != null) {
			NotFirstTimeExecutedBySystemEvent(this, EventArgs.Empty);
		}
	}

	private void FireNotFirstTimeExecutedByUserEvent() {
		OnNotFirstTimeExecutedByUser();
		if (NotFirstTimeExecutedByUserEvent != null) {
			NotFirstTimeExecutedByUserEvent(this, EventArgs.Empty);
		}
	}

	private void ApplicationOnThreadException(object sender, ThreadExceptionEventArgs threadExceptionEventArgs) {
		Tools.Exceptions.ExecuteCapturingException<System.Exception>(() => {
			this.ReportError(threadExceptionEventArgs.Exception);
		}, out var capturedError);
	}

	#endregion

}

