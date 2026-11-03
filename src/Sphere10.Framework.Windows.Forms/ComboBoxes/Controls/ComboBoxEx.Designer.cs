// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

partial class ComboBoxEx {
	private PlaceHolderTextExtender _placeHolderTextExtender;
	private System.ComponentModel.IContainer _components;

	// Keeps the existing placeholder component editable on the native design surface.
	private void InitializeComponent() {
		_components = new System.ComponentModel.Container();
		_placeHolderTextExtender = new PlaceHolderTextExtender(_components);
		SuspendLayout();
		_placeHolderTextExtender.SetPlaceHolderText(this, string.Empty);
		ResumeLayout(false);
	}
}
