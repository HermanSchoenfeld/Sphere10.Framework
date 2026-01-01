// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using Terminal.Gui;

namespace Sphere10.Framework.DApp.Node.UI;

public abstract class FramedScreen<T> : Screen<T> {
	protected new FrameView Frame;

	protected override void LoadInternal() {
		Frame = new FrameView {
			X = 0,
			Y = 0,
			Width = Dim.Fill(),
			Height = Dim.Fill(),
			Title = this.Title
		};
		base.Add(Frame);
	}

	public new void Add(View view) {
		Frame.Add(view);
	}

	public new void Remove(View view) {
		Frame.Remove(view);
	}

	public new IReadOnlyCollection<View> RemoveAll() {
		return Frame.RemoveAll();
	}
}

