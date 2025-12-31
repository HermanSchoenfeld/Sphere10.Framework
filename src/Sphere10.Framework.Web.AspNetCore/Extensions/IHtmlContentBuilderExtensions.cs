// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Text;
using System.Text.Encodings.Web;
using Sphere10.Framework;
using Microsoft.AspNetCore.Html;

namespace Microsoft.Extensions.Hosting;

public static class IHtmlContentBuilderExtensions {
	public static IHtmlContent ToHtmlContent(this IHtmlContentBuilder contentBuilder, HtmlEncoder encoder) {
		var content = new StringBuilder();
		using var writer = new StringBuilderTextWriter(content);
		contentBuilder.WriteTo(writer, encoder);
		return new HtmlString(content.ToString());
	}


}

