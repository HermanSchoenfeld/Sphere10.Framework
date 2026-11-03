// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.iOS {
	public class DropDownListSelection<T> {

		public DropDownListSelection() {
			IsItemSelected = false;	
		}

		public bool IsItemSelected { get; private set; }

		public T SelectedItem {
			get {
				return !IsItemSelected ? throw new Exception(string.Format("No item was selected")) : field;
			}
			set {
				field = value;
				IsItemSelected = true;
			}
		}
	}
}


