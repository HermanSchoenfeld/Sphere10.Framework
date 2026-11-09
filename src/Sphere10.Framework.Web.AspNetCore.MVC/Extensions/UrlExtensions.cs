// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.AspNetCore.Mvc;

namespace Sphere10.Framework.Web.AspNetCore.MVC;

public static class UrlExtensions {
	public static string Content(this IUrlHelper urlHelper, string contentPath, bool toAbsolute = false) {
		var path = urlHelper.Content(contentPath);
		var url = new Uri(urlHelper.ActionContext.HttpContext.Request.GetUri(), path);

		return toAbsolute ? url.AbsoluteUri : path;
	}
}

