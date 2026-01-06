// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.DApp.Core.Consensus;

namespace Sphere10.Framework.DApp.Node.RPC;

public interface IMiningBlockProducer {
	public event EventHandlerEx<SynchronizedList<BlockChainTransaction>> OnBlockAccepted;

	public byte[] GetPrevMinerElectionHeader();

	public byte[] GetBlockPolicy();

	public byte[] GetKernelID();

	public byte[] GetSignature();

	NewMinerBlockSurogate GenerateNewMiningBlock();

	public void NotifyNewBlock();

	public void NotifyNewDiff();
}

