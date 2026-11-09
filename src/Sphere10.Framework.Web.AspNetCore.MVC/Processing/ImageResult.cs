// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Sphere10.Framework.Web.AspNetCore.MVC;

/// <summary>
/// Streams an Image into the output stream
/// </summary>
[SupportedOSPlatform("windows")]
public class ImageResult : ActionResult {

	public Image Image { get; set; }

	public ImageFormat Format { get; set; }

	public ImageResult(Image image) : this(image, ImageFormat.Jpeg) {
	}

	public ImageResult(Image image, ImageFormat imageFormat) {
		this.Image = image;
		this.Format = imageFormat;
	}

	public override void ExecuteResult(ActionContext context) {
		try {
			HttpResponse response = context.HttpContext.Response;
			response.ContentType = GetContentType();
			Image.Save(response.Body, Format);
		} finally {
			Image.Dispose();
		}
	}

	public override async Task ExecuteResultAsync(ActionContext context) {
		// Encode into memory first, because Image.Save writes synchronously and servers reject synchronous response writes by default.
		using var buffer = new MemoryStream();
		try {
			HttpResponse response = context.HttpContext.Response;
			response.ContentType = GetContentType();
			Image.Save(buffer, Format);
			response.ContentLength = buffer.Length;
			await response.Body.WriteAsync(buffer.GetBuffer().AsMemory(0, (int)buffer.Length), context.HttpContext.RequestAborted).ConfigureAwait(false);
		} finally {
			Image.Dispose();
		}
	}

	private string GetContentType() {
		if (Equals(Format, ImageFormat.Jpeg))
			return "image/jpeg";
		if (Equals(Format, ImageFormat.Png))
			return "image/png";
		if (Equals(Format, ImageFormat.Gif))
			return "image/gif";
		throw (new Exception("Unsupported Image Format."));
	}

}

