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
		ProtocolChannel.Open();
	}

	protected ProtocolChannel ProtocolChannel { get; set; }

	public event EventHandlerEx<DataSourceMutatedItems<TItem>> MutatedItems;

	protected void SendBytes(System.ReadOnlyMemory<byte> bytes) {
		ProtocolChannel.TrySendBytes(bytes.ToArray());
	}

	protected void RaiseMutatedItems(DataSourceMutatedItems<TItem> items) => MutatedItems?.Invoke(items);

	//private void ProtocolChannel_ReceivedBytes(System.ReadOnlyMemory<byte> bytes) {
	//}

 public abstract override Task<int> CountAsync { get; }

	public override Task<DataSourceCapabilities> CapabilitiesAsync => Task.FromResult(
		DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage
	);
}

