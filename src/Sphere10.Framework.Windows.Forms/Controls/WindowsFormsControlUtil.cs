// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class WinForms {

	/// <summary>Creates fresh shared native input widgets on the caller's UI thread; the containing control or strip owns their disposal.</summary>
	public static class Controls {
		/// <summary>Creates the existing non-editable toolbar choice selector. Supply its ordinary item and selected-value configuration after creation.</summary>
		public static ToolStripChoiceSelector CreateChoiceSelector() => new ToolStripChoiceSelector();

		/// <summary>Creates the existing search textbox with its inline clear action; consumers retain their normal TextChanged filtering workflow.</summary>
		public static SearchTextBox CreateSearchBox() => new SearchTextBox();
	}
}
