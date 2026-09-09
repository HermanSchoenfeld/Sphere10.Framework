// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.VisualRenderer;

namespace Sphere10.VisualRenderer.Tests;

internal sealed class MemoryAssetHandler : HttpMessageHandler {
	private readonly IReadOnlyDictionary<string, RenderAsset> _assets;

	public MemoryAssetHandler(IReadOnlyDictionary<string, RenderAsset> assets) {
		_assets = assets;
	}

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
		var path = Uri.UnescapeDataString(request.RequestUri.AbsolutePath);
		if (!_assets.TryGetValue(path, out var asset))
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
		var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(asset.Content.ToArray()) };
		response.Content.Headers.ContentType = new MediaTypeHeaderValue(asset.ContentType);
		return Task.FromResult(response);
	}
}