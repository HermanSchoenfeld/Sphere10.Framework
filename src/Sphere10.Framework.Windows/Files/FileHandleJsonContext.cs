// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Text.Json.Serialization;

namespace Sphere10.Framework.Windows.Files;

/// <summary>Static metadata for the bounded file-handle worker's existing JSON protocol.</summary>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(FileHandleService.WorkerMessage))]
[JsonSerializable(typeof(FileHandleInfo))]
[JsonSerializable(typeof(FileHandleCaptureOptions))]
internal partial class FileHandleJsonContext : JsonSerializerContext;
