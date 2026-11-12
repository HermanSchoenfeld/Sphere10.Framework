// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows;

namespace Tools;

public static partial class WinForms {

	/// <summary>Discovers the existing byte editor and complete text/byte review dialogs. Create and show these native surfaces on their owner's UI thread.</summary>
	public static class Review {
		/// <summary>Creates a caller-owned stream-backed hex editor with no document bound.</summary>
		public static HexEditorControl CreateHexEditor() => new HexEditorControl();

		/// <summary>Shows the existing owned hex editor window over a copy of the bytes. Returns the saved copy or null when the window closes without saving; token cancellation throws after ending the owned window.</summary>
		public static Task<byte[]> ShowBytesAsync(IWin32Window owner, string title, byte[] bytes, bool readOnly = false, CancellationToken cancellationToken = default)
			=> HexEditorForm.ShowBytesAsync(owner, title, bytes, readOnly, cancellationToken);

		/// <summary>Shows the existing complete-text review and optional save workflow. Returns its saved path or null on closure; token cancellation retains the dialog's normal cancellation contract.</summary>
		public static Task<string> ShowTextAsync(IWin32Window owner, string title, string instructions, string text, string defaultFileName = "export.txt",
			string filter = ReviewTextDialog.DefaultFilter, Encoding encoding = null, CancellationToken cancellationToken = default, ILogger logger = null
		) {
			return ReviewTextDialog.ShowAsync(owner, title, instructions, text, defaultFileName, filter, encoding, cancellationToken, logger);
		}
	}
}
