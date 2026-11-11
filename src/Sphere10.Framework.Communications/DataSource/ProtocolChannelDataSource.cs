// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sphere10.Framework.Communications;

public abstract class ProtocolChannelDataSource<TItem> : AsyncBatchDataSourceBase<TItem> {

	public delegate string InitializeDelegate(TItem item, int id);


	public delegate string UpdateDelegate(TItem item);


	public delegate string IdDelegate(TItem item);


	protected ProtocolChannelDataSource(ProtocolChannel protocolChannel) {
		ProtocolChannel = protocolChannel;
		//ProtocolChannel.ReceivedBytes += ProtocolChannel_ReceivedBytes;
		// A constructor cannot await, and a server channel's open waits for its first client, so the open runs in the background and stays observable
		OpenTask = ProtocolChannel.Open();
	}

	protected ProtocolChannel ProtocolChannel { get; set; }

	/// <summary>
	/// Completes when the channel has opened, and faults with the channel's error when it cannot open.
	/// </summary>
	public Task OpenTask { get; }

	public event EventHandlerEx<DataSourceMutatedItems<TItem>> MutatedItems;

	protected void SendBytes(System.ReadOnlyMemory<byte> bytes) => ProtocolChannel.TrySendBytes(bytes.ToArray());

	protected void RaiseMutatedItems(DataSourceMutatedItems<TItem> items) => MutatedItems?.Invoke(items);

	//private void ProtocolChannel_ReceivedBytes(System.ReadOnlyMemory<byte> bytes) {
	//}

 public abstract override Task<long> CountAsync { get; }

	public override Task<DataSourceCapabilities> CapabilitiesAsync => Task.FromResult(
		DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage
	);
}

