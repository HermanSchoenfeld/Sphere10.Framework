// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application;

/// <summary>The application's loading indicator, shown with a message while the application works, such as when it opens or closes a screen
/// or reads data in the background. A platform presents it (the WinForms main window in its status bar).</summary>
public interface ILoadingIndicator {
	/// <summary>Raised when the indicator turns on or off or its message changes, on the thread that changed it.</summary>
	event EventHandlerEx Changed;

	/// <summary>Whether the indicator is on.</summary>
	bool IsActive { get; }

	/// <summary>The message shown with the indicator, or null when it is off.</summary>
	string Message { get; }

	/// <summary>Turns the indicator on with the message until the returned scope is disposed, which turns it off. Indicators shown together
	/// stay on until the last is disposed, and the most recent message still shown is the one displayed.</summary>
	IScope Show(string message);
}
