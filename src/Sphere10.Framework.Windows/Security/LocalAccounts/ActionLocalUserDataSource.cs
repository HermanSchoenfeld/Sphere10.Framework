// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows.Security;

/// <summary>A local user data source whose confirmation and logger the caller supplies, for clients which compose a source without deriving one.</summary>
public sealed class ActionLocalUserDataSource : LocalUserDataSourceBase {
	private readonly Func<CrudAction, LocalUserEntry, Task<bool>> _confirm;
	private readonly Func<ILogger> _logger;

	public ActionLocalUserDataSource(LocalAccountsService service, Func<CrudAction, LocalUserEntry, Task<bool>> confirm, Func<ILogger> logger, CancellationToken lifetime = default)
		: base(service, lifetime) {
		Guard.ArgumentNotNull(confirm, nameof(confirm));
		Guard.ArgumentNotNull(logger, nameof(logger));
		_confirm = confirm;
		_logger = logger;
	}

	protected override ILogger Logger => _logger();

	protected override Task<bool> ConfirmAsync(CrudAction action, LocalUserEntry entry) => _confirm(action, entry);
}
