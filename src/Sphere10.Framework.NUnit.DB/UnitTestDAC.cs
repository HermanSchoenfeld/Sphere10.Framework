// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Data;

namespace Sphere10.Framework.NUnit {
	public class UnitTestDAC : DACDecorator, IDisposable {

		public UnitTestDAC(Action endAction, IDAC innerDAC) : base(innerDAC) {
			EndAction = endAction;
		}

		public Action EndAction { get; private set; }

		public void Dispose() {
			if (EndAction != null) {
				EndAction();
			}
		}
	}
}

