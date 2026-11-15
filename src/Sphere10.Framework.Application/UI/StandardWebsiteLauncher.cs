// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Diagnostics;

using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application;

public class StandardWebsiteLauncher : IWebsiteLauncher {

	public StandardWebsiteLauncher(IProductInformationProvider productProvider) {
		ProductProvider = productProvider;

	}

	public IProductInformationProvider ProductProvider { get; private set; }

	public Task LaunchWebsiteAsync(string url, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		Process.Start(new ProcessStartInfo {
			FileName = $"{url.Trim('"')}",
			UseShellExecute = true,
		});
		return Task.CompletedTask;
	}

	public Task LaunchCompanyWebsiteAsync(CancellationToken cancellationToken = default)
		=> LaunchWebsiteAsync(ProductProvider.ProductInformation.CompanyUrl, cancellationToken);

	public Task LaunchProductWebsiteAsync(CancellationToken cancellationToken = default)
		=> LaunchWebsiteAsync(ProductProvider.ProductInformation.ProductUrl, cancellationToken);

	public Task LaunchProductPurchaseWebsiteAsync(CancellationToken cancellationToken = default)
		=> LaunchWebsiteAsync(ProductProvider.ProductInformation.ProductPurchaseUrl, cancellationToken);

}

