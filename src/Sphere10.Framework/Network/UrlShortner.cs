// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Sphere10.Framework;

public static class UrlShortner {
	public static async Task<string> TinyUrlAsync(string url, string apiKey, string provider = "0_mk") {
		//string yourUrl = "http://your-site.com/your-url-for-minification";
		//string apikey = "YOUR-API-KEY-GOES-HERE";
		//string provider = "0_mk"; // see provider strings list in API docs
		var uriString = string.Format(
			"http://tiny-url.info/api/v1/create?url={0}&apikey={1}&provider={2}&format=text",
			url,
			apiKey,
			provider);

		var address = new Uri(uriString);
		using var client = new HttpClient();
		return await client.GetStringAsync(address);
	}
	public static string TinyUrl(string url, string apiKey, string provider = "0_mk") {
		//string yourUrl = "http://your-site.com/your-url-for-minification";
		//string apikey = "YOUR-API-KEY-GOES-HERE";
		//string provider = "0_mk"; // see provider strings list in API docs
		var uriString = string.Format(
			"http://tiny-url.info/api/v1/create?url={0}&apikey={1}&provider={2}&format=text",
			url,
			apiKey,
			provider);

		var address = new Uri(uriString);
		using var client = new HttpClient();
		return client.GetStringAsync(address).ResultSafe();
	}
	public static async Task<string> GoogleAsync(string url, string apiKey) {
		using var client = new HttpClient();
		var json = "{\"longUrl\":\"" + url + "\",\"key\":\"" + apiKey + "\"}";
		using var content = new StringContent(json, Encoding.UTF8, "application/json");
		using var response = await client.PostAsync("https://www.googleapis.com/urlshortener/v1/url?key=" + apiKey, content);
		response.EnsureSuccessStatusCode();
		var responseJson = await response.Content.ReadAsStringAsync();

		/* {
			 "kind": "urlshortener#url",
			 "id": "https://goo.gl/Akn82b",
			 "longUrl": "https://sphere10.com/"
			} */

		// Was in a rush TODO: parse nicer
		return
			responseJson
				.Replace("\n", string.Empty)
				.Replace("\r", string.Empty)
				.Replace("\t", string.Empty)
				.Replace(" ", string.Empty)
				.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[1]
				.Replace("https:", "https^")
				.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[1]
				.Replace("https^", "https:")
				.Replace("\"", string.Empty)
				.Trim();

	}
	public static string Google(string url, string apiKey) {
		using var client = new HttpClient();
		var json = "{\"longUrl\":\"" + url + "\",\"key\":\"" + apiKey + "\"}";
		using var content = new StringContent(json, Encoding.UTF8, "application/json");
		using var response = client.PostAsync("https://www.googleapis.com/urlshortener/v1/url?key=" + apiKey, content).ResultSafe();
		response.EnsureSuccessStatusCode();
		var responseJson = response.Content.ReadAsStringAsync().ResultSafe();

		/* {
			 "kind": "urlshortener#url",
			 "id": "https://goo.gl/Akn82b",
			 "longUrl": "https://sphere10.com/"
			} */

		// Was in a rush TODO: parse nicer
		return
			responseJson
				.Replace("\n", string.Empty)
				.Replace("\r", string.Empty)
				.Replace("\t", string.Empty)
				.Replace(" ", string.Empty)
				.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[1]
				.Replace("https:", "https^")
				.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)[1]
				.Replace("https^", "https:")
				.Replace("\"", string.Empty)
				.Trim();
	}
}

