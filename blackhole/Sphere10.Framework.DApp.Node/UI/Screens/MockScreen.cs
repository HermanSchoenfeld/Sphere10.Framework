// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Sphere10.Framework.DApp.Node.UI;

public class MockScreen : Screen {

	protected override void LoadInternal() {
		var login = new Label { Text = "Login: ", X = 3, Y = 2 };
		var password = new Label {
			Text = "Password: ",
			X = Pos.Left(login),
			Y = Pos.Top(login) + 1
		};
		var loginText = new TextField {
			Text = string.Empty,
			X = Pos.Right(password),
			Y = Pos.Top(login),
			Width = 40
		};
		var passText = new TextField {
			Text = string.Empty,
			Secret = true,
			X = Pos.Left(loginText),
			Y = Pos.Top(password),
			Width = Dim.Width(loginText)
		};

		// Add some controls, 
		this.Add(login, password, loginText, passText);

		var remember = new CheckBox { X = 3, Y = 6, Text = "Remember me" };
		var ok = new Button { X = 3, Y = 14, Text = "Ok" };
		var cancel = new Button { X = 10, Y = 14, Text = "Cancel" };
		var info = new Label { X = 3, Y = 18, Text = "Press F9 or ESC plus 9 to activate the menubar" };
		this.Add(remember, ok, cancel, info);
	}

}

