// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Accepted, optional logging preferences for one stable owner/control identity. Contains no logger, writer or control references.</summary>
public sealed class LogOutputSelectorSettings : SettingsObject {
	public bool HasConfiguration { get; set; }
	public bool IsLogging { get; set; }
	public LogOutputConfiguration Configuration { get; set; }
}
