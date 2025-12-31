// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.IO;
using Sphere10.Framework.Windows.Forms;


namespace Sphere10.Framework.Utils.WinFormsTester;

public partial class PadLockTestScreen : ApplicationScreen {
	private readonly TextWriter _outputTextWriter;
	public PadLockTestScreen() {
		InitializeComponent();
		_outputTextWriter = new TextBoxWriter(_outputTextBox);
	}


	private void padLockButton1_PadLockStateChanged(PadLockButton arg1, PadLockButton.PadLockState arg2) {
		_outputTextWriter.WriteLine("{0}: {1}", arg1.Name, arg2);
	}


}


