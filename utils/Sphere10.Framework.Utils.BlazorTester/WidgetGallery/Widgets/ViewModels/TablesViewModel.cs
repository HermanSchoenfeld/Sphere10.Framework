// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

namespace Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.ViewModels;

public class TablesViewModel : ExtendedComponentViewModel {
	public INodeService NodeService { get; }

	public TablesViewModel(INodeService nodeService, IEndpointManager endpointManager) : base(endpointManager) {
		Guard.ArgumentNotNull(nodeService, nameof(nodeService));
		NodeService = nodeService;
	}

	protected override async Task InitCoreAsync() {
		await Task.CompletedTask;
	}
}


