// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Data;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.PostgreSQL;

public class ModuleConfiguration : ModuleConfigurationBase {
	public override void RegisterComponents(IServiceCollection serviceCollection) {
		serviceCollection.AddNamedTransient<ConnectionBarBase, PostgreSQLConnectionBar>(nameof(DBMSType.PostgreSQL));
		serviceCollection.AddNamedTransient<ConnectionPanelBase, PostgreSQLConnectionPanel>(nameof(DBMSType.PostgreSQL));
	}
}
