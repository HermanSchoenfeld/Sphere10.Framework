// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Application;

public class Sphere10Framework {
	private bool _frameworkOwnsServicesProvider;
	private ICoreModuleConfiguration[] _moduleConfigurations;

	public event EventHandlerEx Initializing;
	public event EventHandlerEx Initialized;
	public event EventHandlerEx Finalizing;
	public event EventHandlerEx Finalized;

	public event EventHandlerEx<ProductInformation, ProductInformation> VersionChangeDetected;

	static Sphere10Framework() {
		Instance = new Sphere10Framework();
	}

	public Sphere10Framework() {
		IsStarted = false;
		_frameworkOwnsServicesProvider = false;
		_moduleConfigurations = Array.Empty<ICoreModuleConfiguration>();
	}

	public static Sphere10Framework Instance { get; }

	public IServiceProvider ServiceProvider { get; private set; }

	public bool IsStarted { get; private set; }

	public Sphere10FrameworkOptions Options { get; internal set; }

	internal Sphere10FrameworkBuilder PendingBuilder { get; set; }

	public Sphere10FrameworkBuilder Build() {
		CheckNotStarted();
		return new Sphere10FrameworkBuilder(this);
	}

	internal void StartInternal(IServiceProvider serviceProvider, ICoreModuleConfiguration[] modules, bool ownsProvider) {
		CheckNotStarted();
		ServiceProvider = serviceProvider;
		_moduleConfigurations = modules;
		_frameworkOwnsServicesProvider = ownsProvider;
		Initializing?.Invoke();
		InitializeModules(serviceProvider);
		InitializeApplication();
		Initialized?.Invoke();
		IsStarted = true;
	}

	public void EndFramework() {
		CheckStarted();
		Finalizing?.Invoke();
		FinalizeApplication();
		FinalizeModules(ServiceProvider);
		if (_frameworkOwnsServicesProvider && ServiceProvider is IDisposable Disposable)
			Tools.Exceptions.ExecuteIgnoringException(Disposable.Dispose);
		IsStarted = false;
		_moduleConfigurations = Array.Empty<ICoreModuleConfiguration>();
		Finalized?.Invoke();
	}

	public void TerminateApplication(int exitCode) {
		if (IsStarted)
			EndFramework();
		System.Environment.Exit(exitCode);
	}

	private void InitializeModules(IServiceProvider serviceProvider)
		=> _moduleConfigurations.ForEach(m => m.OnInitialize(serviceProvider));

	private void InitializeApplication() {
		var Initializers = ServiceProvider.GetServices<IApplicationInitializer>().ToArray();

		// Execute non-parallelizable initializers in sequence first
		Initializers
			.Where(x => !x.Parallelizable)
			.OrderBy(x => x.Priority)
			.ForEach(x => x.Initialize());

		// Parallel execute all parallelizable initializers
		Parallel.ForEach(
			Initializers
				.Where(x => x.Parallelizable)
				.OrderBy(x => x.Priority),
			x => x.Initialize()
		);
	}

	internal void FinalizeModules(IServiceProvider serviceProvider)
		=> _moduleConfigurations.Update(m => m.OnFinalize(serviceProvider));

	private void FinalizeApplication() {
		ServiceProvider
			.GetServices<IApplicationFinalizer>()
			.ForEach(f => f.Finalize());
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

