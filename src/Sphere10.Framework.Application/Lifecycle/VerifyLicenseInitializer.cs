// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application;

public class VerifyLicenseInitializer : AsyncApplicationInitializerBase {
	public VerifyLicenseInitializer(IBackgroundLicenseVerifier backgroundLicenseVerifier, IUserInterfaceServices userInterfaceServices) {
		BackgroundLicenseVerifier = backgroundLicenseVerifier;
		UserInterfaceServices = userInterfaceServices;
	}

	protected IBackgroundLicenseVerifier BackgroundLicenseVerifier { get; }

	public IUserInterfaceServices UserInterfaceServices { get; }

	public override bool Parallelizable => true;

	public override int Priority => int.MaxValue; // run last

	public override Task InitializeAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		return BackgroundLicenseVerifier.VerifyLicense(cancellationToken);
	}

}

