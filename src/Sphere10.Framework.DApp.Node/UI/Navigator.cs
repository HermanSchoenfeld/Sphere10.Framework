// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sphere10.Framework.DApp.Node.UI;

// change to IOC
public static class Navigator {
	private const string TitlePrefix = "VelocityNET";
	private static View _top;
	private static FrameView _screenFrame;
	private static Screen _currentScreen;
	private static StatusBar _statusBar;
	private static Type[] _applicationScreenTypes;
	private static IDictionary<Type, Screen> _activatedScreens;

	public static void Start(CancellationToken stopRunningToken) {
		TGApplication.Init();
		_top = TGApplication.TopRunnableView;
		stopRunningToken.Register(Quit);
		_activatedScreens = new Dictionary<Type, Screen>();
		_applicationScreenTypes = ScanApplicationScreens().ToArray();

		_activatedScreens =
			_applicationScreenTypes
				.Where(x => x.GetCustomAttributeOfType<LifetimeAttribute>().Lifetime == ScreenLifetime.Application)
				.ToDictionary(x => x, CreateScreen);

		_top.Add(BuildMenu());
		Show<DashboardScreen>();
		_statusBar = BuildStatusBar();
		_top.Add(_statusBar);
		RunApplication(
			(IRunnable)_top,
			error => {
				SystemLog.Exception(nameof(Navigator), nameof(Start), error);
				Dialogs.Exception(error);
				return true;
			}
		);
	}

	public static void Quit() {
		TGApplication.RequestStop((IRunnable)TGApplication.TopRunnable);
	}

	public static void NotifyStatusBarChanged() {
		_top.Remove(_statusBar);
		_statusBar = BuildStatusBar();
		_top.Add(_statusBar);
	}

	private static IEnumerable<Type> ScanApplicationScreens() {
		// Dynamically build menu from attributes
		var screenTypes = Assembly.GetAssembly(typeof(Screen)).GetDerivedTypes<Screen>().Where(x => !x.IsAbstract).ToArray();

		// make sure each screen has correct attributes
		foreach (var screenType in screenTypes) {
			var error = false;

			// Screen Title check
			if (!screenType.GetCustomAttributesOfType<TitleAttribute>().Any()) {
				error = true;
				SystemLog.Error(nameof(Navigator), nameof(ScanApplicationScreens), $"Screen '{screenType.FullName}' missing '{nameof(TitleAttribute)}' attribute");
			}

			// Lifetime check
			if (!screenType.GetCustomAttributesOfType<LifetimeAttribute>().Any()) {
				error = true;
				SystemLog.Error(nameof(Navigator), nameof(ScanApplicationScreens), $"Screen '{screenType.FullName}' missing '{nameof(LifetimeAttribute)}' attribute");
			}

			// Menu Location check
			if (!screenType.GetCustomAttributesOfType<MenuLocationAttribute>().Any()) {
				error = true;
				SystemLog.Error(nameof(Navigator), nameof(ScanApplicationScreens), $"Screen '{screenType.FullName}' missing '{nameof(MenuLocationAttribute)}' attribute");
			}

			if (!error)
				yield return screenType;

		}

		// NOTE: should load from plugin dll's as well
		var dict = new ExtendedLookup<AppMenu, Tuple<MenuLocationAttribute, Type>>();

		foreach (var screenType in screenTypes) {
			var menuLoc = screenType.GetCustomAttributeOfType<MenuLocationAttribute>(throwOnMissing: false);
			var menyTitle = screenType.GetCustomAttributeOfType<MenuLocationAttribute>(throwOnMissing: false);
			if (menuLoc == default) {
				SystemLog.Error(nameof(Navigator), nameof(BuildMenu), $"Screen '{screenType.FullName}' missing '{nameof(MenuLocationAttribute)}' attribute");
				continue;
			}

			dict.Add(menuLoc.Menu, Tuple.Create(menuLoc, screenType));
		}
	}

	private static MenuBar BuildMenu() {
		var screensByAppMenu =
			_applicationScreenTypes.ToLookup(
				x => x.GetCustomAttributeOfType<MenuLocationAttribute>().Menu,
				x => Tuple.Create(x.GetCustomAttributeOfType<MenuLocationAttribute>(), x)
			);

		var items =
			Enum
				.GetValues<AppMenu>()
				.Select(x => new MenuBarItem(
					x.GetDescription(),
					screensByAppMenu[x]
						.OrderBy(x => x.Item1.PreferredIndex)
						.Select(x => new MenuItem(x.Item1.Name, null, () => ShowScreen(x.Item2)))
						.ToArray()
				)).ToArray();


		return new MenuBar(items);

	}

	private static StatusBar BuildStatusBar() {
		var screenItems = _currentScreen?.BuildStatusItems() ?? Enumerable.Empty<Shortcut>();
		var statusBar = new StatusBar(screenItems.Concat(BuildGlobalStatusItems()));
		return statusBar;
	}

	private static IEnumerable<Shortcut> BuildGlobalStatusItems() => Enumerable.Empty<Shortcut>();

	private static void Show<TScreen>() where TScreen : Screen, new() {
		ShowScreen(typeof(TScreen));
	}

	private static void ShowScreen(Type screenType) {
		if (_currentScreen != null) {
			// Same screen
			if (screenType == _currentScreen.GetType())
				return;


			// Notify current screen disappearing
			_currentScreen.NotifyDisappearing(out var cancel);
			if (cancel)
				return;
		}

		var priorScreen = _currentScreen;

		// Create new screen
		if (!_activatedScreens.TryGetValue(screenType, out var newScreen)) {
			newScreen = CreateScreen(screenType);
			_activatedScreens[screenType] = newScreen;
		}
		_currentScreen = newScreen; // need to set current screen immediately since OnAppearing() needs to know
		newScreen.NotifyAppearing();

		if (priorScreen != null) {
			// Remove/destroy current screen

			if (_screenFrame != null) {
				_screenFrame.Remove(priorScreen); // remove for not disposing
				_top.Remove(_screenFrame);
				_screenFrame.Dispose();
				_screenFrame = null;
			}
			if (priorScreen != null) {
				_top.Remove(priorScreen);
			}
			priorScreen.NotifyDisappeared();

			switch (priorScreen.GetType().GetCustomAttributeOfType<LifetimeAttribute>().Lifetime) {
				case ScreenLifetime.WhenVisible:
					priorScreen.NotifyDestroying();
					_activatedScreens.Remove(priorScreen.GetType());
					priorScreen.Dispose();
					break;
				case ScreenLifetime.Application:
				case ScreenLifetime.LazyLoad:
					// keep activated
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		// Show new screen
		if (newScreen is TabbedScreen) {
			// Multi-part screens have no screen frame
			newScreen.X = 0;
			newScreen.Y = 1;
			newScreen.Width = Dim.Fill();
			newScreen.Height = Dim.Fill(1);
			_top.Add(newScreen);
		} else {
			_screenFrame = new FrameView {
				Title = $"{TitlePrefix} {newScreen.Title}",
				X = 0,
				Y = 1, // 1 for menu  
				Width = Dim.Fill(),
				Height = Dim.Fill(1) // 1 for statusbar
			};
			_screenFrame.Add(newScreen);
			_top.Add(_screenFrame);
		}
		NotifyStatusBarChanged();
		_top.SetNeedsDraw();
		_currentScreen.NotifyAppeared();
	}

	private static Screen CreateScreen(Type screenType) {
		var screen = Activator.CreateInstance(screenType) as Screen;
		screen.NotifyCreated();
		screen.Load();
		return screen;
	}

	private static void RunApplication(IRunnable runnable, Func<Exception, bool> errorHandler = null) {
		TGApplication.Run(runnable, errorHandler);
	}

}

