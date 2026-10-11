// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Renderer-specific interaction services over the portable UI coordinator.</summary>
public interface IBlazorUserInterfaceServices : IUserInterfaceServices {
	/// <summary>Binds the shell controller and renderer dispatcher for this circuit. Disposing the scope removes that binding.</summary>
	IDisposable AttachController(object controller, Func<Func<Task>, Task> dispatcher);
}
