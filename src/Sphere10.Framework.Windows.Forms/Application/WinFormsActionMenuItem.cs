// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsActionMenuItem : WinFormsApplicationMenuItemBase, IWinFormsLinkMenuItem {
	private readonly ApplicationAction _action = new();

	public WinFormsActionMenuItem(Action onClick)
		: this(string.Empty, onClick) {
	}

	public WinFormsActionMenuItem(Action<IUserInterfaceServices> onClick)
		: this(string.Empty, onClick) {
	}

	public WinFormsActionMenuItem(string text, Action select) {
		Guard.ArgumentNotNull(select, nameof(select));
		Text = text;
		_action.Action = select;
	}

	public WinFormsActionMenuItem(string text, Action<IUserInterfaceServices> select) {
		Guard.ArgumentNotNull(select, nameof(select));
		Text = text;
		_action.UIAction = select;
	}

	public WinFormsActionMenuItem(string text, Image image16x16, Action OnClick)
		: this(text, OnClick) {
		Image16x16 = image16x16;
	}

	public WinFormsActionMenuItem(string text, Image image16x16, Action<IUserInterfaceServices> onClick)
		: this(text, onClick) {
		Image16x16 = image16x16;
	}

	public WinFormsActionMenuItem(
		string text,
		Image image16x16,
		bool showOnExplorerBar = true,
		bool showOnToolBar = true,
		bool executeOnLoad = false
	)
		: base(image16x16, showOnExplorerBar, showOnToolBar, executeOnLoad) {
		Text = text;
	}


	public virtual string Text {
		get => Title;
		set => Title = value;
	}

	public virtual void OnSelect() {
		_action.Execute();
		NotifySelect();
	}

	public virtual void OnSelect(IUserInterfaceServices userInterface) {
		if (_action.UIAction == null) {
			OnSelect();
			return;
		}
		_action.Execute(userInterface);
		NotifySelect();
	}

	public override void Dispose() {
		base.Dispose();
	}

}

