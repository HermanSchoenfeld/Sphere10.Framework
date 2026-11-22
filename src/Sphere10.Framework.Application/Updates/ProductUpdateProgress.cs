// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application;

/// <summary>
/// The progress of an installation, raised by <see cref="ProductUpdaterBase.InstallProgressChanged"/>: its stage, the bytes downloaded and
/// the bytes to download, which is zero while it is not known.
/// </summary>
public sealed record ProductUpdateProgress(ProductUpdateStage Stage, long BytesTransferred, long BytesTotal);
