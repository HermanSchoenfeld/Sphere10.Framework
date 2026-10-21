// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application;

/// <summary>Optional product support transport supplied by the application's composition.</summary>
public interface IProductSupportService {
	Task<Result> RequestFeatureAsync(FeatureRequest request, CancellationToken cancellationToken = default);
	Task<Result> SendCommentAsync(ProductComment request, CancellationToken cancellationToken = default);
	Task<Result> SubmitBugReportAsync(BugReport request, CancellationToken cancellationToken = default);
	Task<Result> ReportCrashAsync(BugReport request, CancellationToken cancellationToken = default);
}
