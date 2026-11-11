// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Sphere10.Framework.Web.AspNetCore;

public static class HttpRequestExtensions {

	public static Uri GetUri(this HttpRequest request) {
		var uriBuilder = new UriBuilder {
			Scheme = request.Scheme,
			Host = request.Host.Host,
			// -1 selects the scheme's default port when the host carries none
			Port = request.Host.Port ?? -1,
			Path = request.Path.ToString(),
			Query = request.QueryString.ToString()
		};
		return uriBuilder.Uri;
	}

	public static string GetParameter(this HttpRequest request, Enum param) => request.GetParameter<string>(param);

	public static bool ContainsParameter(this HttpRequest request, Enum param) {
		PageParameterProcessor processor = new PageParameterProcessor(request);
		return processor.ContainsParameter(param);
	}

	public static T GetParameter<T>(this HttpRequest request, Enum param) {
		PageParameterProcessor processor = new PageParameterProcessor(request);
		return processor.GetParameter<T>(param);
	}

	public static async Task<string> ToStringAsync(this HttpRequest request) {
		var requestString = new StringBuilder();
		requestString.Append($"{request.Method} {request.Path}{request.QueryString} {request.Protocol}\n");
		foreach (var header in request.Headers)
			requestString.Append($"{header.Key}: {header.Value}\n");
		requestString.Append("\n");
		if (!request.Body.CanRead)
			return requestString.ToString();
		var startPosition = request.Body.CanSeek ? request.Body.Position : -1;
		// Leave the body stream open so it's still available for further processing.
		using var reader = new StreamReader(request.Body, leaveOpen: true);
		var body = await reader.ReadToEndAsync();
		// Restore a seekable (buffered) body's position so the next reader can read it.
		if (startPosition >= 0)
			request.Body.Position = startPosition;
		requestString.Append(body);
		return requestString.ToString();
	}
}

