// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Application;

public class Sphere10Framework {
	private bool _frameworkOwnsServicesProvider;
	private bool _finalizationAttempted;
	private bool _finalizingFramework;
	private bool _endingFramework;
	private ICoreModuleConfiguration[] _moduleConfigurations;

	public event EventHandlerEx Registering;
	public event EventHandlerEx Initializing;
	public event EventHandlerEx Initialized;
	public event EventHandlerEx Finalizing;
	public event EventHandlerEx Finalized;
	public event ProgressHandler Progress;

	public event EventHandlerEx<ProductInformation, ProductInformation> VersionChangeDetected;

	static Sphere10Framework() {
		Instance = new Sphere10Framework();
	}

	public Sphere10Framework() {
		IsStarted = false;
		_frameworkOwnsServicesProvider = false;
		_moduleConfigurations = Array.Empty<ICoreModuleConfiguration>();
		Disposables = new Disposables();
	}

	public static Sphere10Framework Instance { get; }

	public Disposables Disposables {get; }

	public IServiceProvider ServiceProvider { get; private set; }

	public bool IsStarted { get; private set; }

	public Sphere10FrameworkOptions Options { get; internal set; }

	internal Sphere10FrameworkBuilder PendingBuilder { get; set; }

	public Sphere10FrameworkBuilder Build() {
		CheckNotStarted();
		return new Sphere10FrameworkBuilder(this);
	}

	internal void FireRegistering() => Registering?.Invoke();

	protected virtual void OnVersionChangeDetected(ProductInformation previousVersion, ProductInformation currentVersion)
		=> VersionChangeDetected?.Invoke(previousVersion, currentVersion);

	internal void StartInternal(IServiceProvider serviceProvider, ICoreModuleConfiguration[] modules, bool ownsProvider,
		Func<IServiceProvider, ILogger> loggerSelector = null) {
		CheckNotStarted();
		ServiceProvider = serviceProvider;
		_moduleConfigurations = modules;
		_frameworkOwnsServicesProvider = ownsProvider;
		_finalizationAttempted = false;
		Initializing?.Invoke();
		InitializeModules(serviceProvider);
		var logger = loggerSelector?.Invoke(serviceProvider) ?? serviceProvider.GetService<ILogger>() ?? SystemLog.Logger;
		InitializeApplication(logger);
		Initialized?.Invoke();
		IsStarted = true;
	}

	public void EndFramework() {
		CheckStarted();
		Guard.Ensure(!_finalizingFramework && !_endingFramework, "Framework finalization is already running.");
		_endingFramework = true;
		using var ending = Tools.Scope.ExecuteOnDispose(() => _endingFramework = false);
		var errors = new List<Exception>();
		using (Tools.Scope.ExecuteOnDispose(() => {
			// Keep the provider alive until shutdown tasks have finished and the native message loop has ended.
			if (_frameworkOwnsServicesProvider && ServiceProvider is IDisposable disposable)
				Tools.Exceptions.ExecuteCapturingException(disposable.Dispose, errors);
			IsStarted = false;
			_frameworkOwnsServicesProvider = false;
			_moduleConfigurations = Array.Empty<ICoreModuleConfiguration>();
			var resources = Disposables.ToArray();
			Disposables.Clear();
			foreach (var resource in resources)
				Tools.Exceptions.ExecuteCapturingException(() => resource.Dispose(), errors);
			foreach (var handler in Finalized?.GetInvocationList().Cast<EventHandlerEx>() ?? Array.Empty<EventHandlerEx>())
				Tools.Exceptions.ExecuteCapturingException(handler.Invoke, errors);
		})) {
			Tools.Exceptions.ExecuteCapturingException(FinalizeFramework, errors);
		}
		if (errors.Count == 1)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();
		if (errors.Count > 1)
			throw new AggregateException(errors);
	}

	/// <summary>Runs process shutdown tasks once while keeping the service provider and owned resources available until EndFramework.</summary>
	internal void FinalizeFramework() {
		CheckStarted();
		Guard.Ensure(!_finalizingFramework, "Framework finalization is already running.");
		if (_finalizationAttempted)
			return;
		_finalizationAttempted = true;
		_finalizingFramework = true;
		using var finalizing = Tools.Scope.ExecuteOnDispose(() => _finalizingFramework = false);
		var errors = new List<Exception>();
		foreach (var handler in Finalizing?.GetInvocationList().Cast<EventHandlerEx>() ?? Array.Empty<EventHandlerEx>())
			Tools.Exceptions.ExecuteCapturingException(handler.Invoke, errors);
		Tools.Exceptions.ExecuteCapturingException(FinalizeApplication, errors);
		Tools.Exceptions.ExecuteCapturingException(() => FinalizeModules(ServiceProvider), errors);
		if (errors.Count == 1)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();
		if (errors.Count > 1)
			throw new AggregateException(errors);
	}

	public void TerminateApplication(int exitCode) {
		if (IsStarted)
			EndFramework();
		System.Environment.Exit(exitCode);
	}

	private void InitializeModules(IServiceProvider serviceProvider)
		=> _moduleConfigurations.ForEach(m => m.OnInitialize(serviceProvider));

	private void InitializeApplication(ILogger logger)
		=> Tools.Application.ExecuteInitializers(ServiceProvider.GetServices<IApplicationInitializer>(), logger,
			(title, message, completed, total, percent) => Progress?.Invoke(title, message, completed, total, percent));

	internal void FinalizeModules(IServiceProvider serviceProvider) {
		var errors = new List<Exception>();
		foreach (var module in _moduleConfigurations)
			Tools.Exceptions.ExecuteCapturingException(() => module.OnFinalize(serviceProvider), errors);
		if (errors.Count == 1)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();
		if (errors.Count > 1)
			throw new AggregateException(errors);
	}

	private void FinalizeApplication() {
		var errors = new List<Exception>();
		foreach (var finalizer in ServiceProvider.GetServices<IApplicationFinalizer>())
			Tools.Exceptions.ExecuteCapturingException(finalizer.Finalize, errors);
		if (errors.Count == 1)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();
		if (errors.Count > 1)
			throw new AggregateException(errors);
	}

	public ILogger CreateApplicationLogger(bool visibleToAllUsers = false)
		=> CreateApplicationLogger(Tools.Text.FormatEx("{ProductName}"), visibleToAllUsers: visibleToAllUsers);

	public ILogger CreateApplicationLogger(string fileName, bool visibleToAllUsers = false)
		=> CreateApplicationLogger(fileName, RollingFileLogger.DefaultMaxFiles, RollingFileLogger.DefaultMaxFileSizeB, visibleToAllUsers);

	public ILogger CreateApplicationLogger(string fileName, int maxFiles, int maxFileSize, bool visibleToAllUsers = false)
		=> new ThreadIdLogger(
			new TimestampLogger(
				new RollingFileLogger(
					Path.Combine(Tools.Text.FormatEx(visibleToAllUsers ? "{SystemDataDir}" : "{UserDataDir}"), Tools.Text.FormatEx("{ProductName}"), "logs", fileName),
					maxFiles,
					maxFileSize
				)
			)
		);

	private void CheckStarted() {
		if (!IsStarted)
			throw new InvalidOperationException("Sphere10.Framework Framework was not started");
	}

	private void CheckNotStarted() {
		if (IsStarted)
			throw new InvalidOperationException("Sphere10.Framework Framework is already started");
	}	
}
