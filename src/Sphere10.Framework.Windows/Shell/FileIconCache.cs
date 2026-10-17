// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows.Shell;

/// <summary>Owns worker-loaded local file icons and lends cached images at a caller-selected size.</summary>
[SupportedOSPlatform("windows6.1")]
public class FileIconCache : SynchronizedObject, IDisposable {
	private readonly Dictionary<string, Image> _images = new(StringComparer.OrdinalIgnoreCase);
	private readonly Image _fallback;
	private readonly int _imageSize;
	private readonly int _capacity;
	private readonly TimeSpan _loadingBudget;
	private bool _disposed;

	public FileIconCache(int imageSize = 16, int capacity = 1024, TimeSpan? loadingBudget = null) {
		Guard.ArgumentGT(imageSize, 0, nameof(imageSize));
		Guard.ArgumentGT(capacity, 0, nameof(capacity));
		_imageSize = imageSize;
		_capacity = capacity;
		_loadingBudget = loadingBudget ?? TimeSpan.FromSeconds(1);
		Guard.Argument(_loadingBudget > TimeSpan.Zero, nameof(loadingBudget), "The loading budget must be positive.");
		_fallback = CreateFallback(imageSize);
	}

	public Image GetImage(FileIconLocation? location) {
		using var scope = EnterReadScope();
		if (_disposed)
			return null;
		return location.HasValue && IsLocal(location.Value.FilePath) && _images.TryGetValue(GetKey(location.Value), out var image) ? image : _fallback;
	}

	public Task LoadAsync(FileIconLocation[] locations, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(locations, nameof(locations));
		var sources = locations.Where(location => IsLocal(location.FilePath)).DistinctBy(GetKey, StringComparer.OrdinalIgnoreCase).ToArray();
		return Task.Run(() => {
			var budget = Stopwatch.StartNew();
			foreach (var source in sources) {
				cancellationToken.ThrowIfCancellationRequested();
				var key = GetKey(source);
				using (EnterReadScope()) {
					if (_disposed || _images.Count >= _capacity)
						return;
					if (_images.ContainsKey(key))
						continue;
				}
				if (budget.Elapsed > _loadingBudget)
					return;
				Image image = null;
				try {
					// Mapped network drives can appear local; do not load their resources automatically.
					if (new DriveInfo(Path.GetPathRoot(source.FilePath)).DriveType != DriveType.Network && IsRasterImage(source.FilePath)) {
						// Package logos are ordinary image assets; detach their pixels so the cached image never locks the source file.
						using var original = Image.FromFile(source.FilePath);
						image = original.Resize(new Size(_imageSize, _imageSize), ResizeMethod.AspectFitPadded, ResizeAlignment.CenterCenter, newPixelFormat: PixelFormat.Format32bppArgb);
					} else if (new DriveInfo(Path.GetPathRoot(source.FilePath)).DriveType != DriveType.Network) {
						var handles = new IntPtr[1];
						using var release = Tools.Scope.ExecuteOnDispose(() => { foreach (var item in handles) if (item != IntPtr.Zero) WinAPI.USER32.DestroyIcon(item); });
						WinAPI.SHELL32.ExtractIconExW(source.FilePath, source.IconIndex, null, handles, 1);
						var handle = handles[0];
						if (handle != IntPtr.Zero) {
							using var icon = Icon.FromHandle(handle);
							image = icon.ToBitmap(_imageSize, _imageSize);
						}
					}
				} catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or System.ComponentModel.Win32Exception
					or System.Security.SecurityException or System.Runtime.InteropServices.ExternalException
					|| error is OutOfMemoryException && IsRasterImage(source.FilePath)) {
					// Cache unavailable resources as well as successful loads.
				}
				using (EnterWriteScope()) {
					if (_disposed || _images.Count >= _capacity || _images.ContainsKey(key))
						image?.Dispose();
					else
						_images[key] = image ?? _fallback;
				}
			}
		}, cancellationToken);
	}

	public void Dispose() {
		using var scope = EnterWriteScope();
		if (_disposed)
			return;
		_disposed = true;
		foreach (var image in _images.Values.Distinct())
			if (!ReferenceEquals(image, _fallback))
				image.Dispose();
		_images.Clear();
		_fallback.Dispose();
	}

	private static bool IsLocal(string path) => !string.IsNullOrWhiteSpace(path) && !path.StartsWith("\\\\", StringComparison.Ordinal) && Path.IsPathFullyQualified(path);

	private static bool IsRasterImage(string path) => Path.GetExtension(path).ToLowerInvariant() is ".png" or ".bmp" or ".jpg" or ".jpeg" or ".gif";

	private static string GetKey(FileIconLocation location) => location.FilePath + '\0' + location.IconIndex.ToString(CultureInfo.InvariantCulture);

	private static Image CreateFallback(int imageSize) => SystemIcons.Application.ToBitmap(imageSize, imageSize);
}
