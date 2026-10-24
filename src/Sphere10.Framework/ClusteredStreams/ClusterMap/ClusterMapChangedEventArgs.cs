// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

public class ClusterMapChangedEventArgs : EventArgs {
	private ISet<long> _addedClusters;
	private ISet<long> _modifiedClusters;
	private ISet<long> _removedClusters;
	private BijectiveDictionary<long, long> _movedClusters;
	private IDictionary<long, (long? NewStart, long? NewEnd)> _movedTerminals;
	private long _firstModifiedCluster;
	private bool _hasFirstModifiedCluster;
	private bool _addedClustersInitialized;
	private bool _modifiedClustersInitialized;
	private bool _removedClustersInitialized;
	private bool _movedClustersInitialized;
	private bool _movedTerminalsInitialized;

	public ClusterMapChangedEventArgs() {
	}

	public long ClusterCountDelta { get; set; }

	public ISet<long> AddedClusters {
		get {
			if (!_addedClustersInitialized) {
				_addedClusters = new HashSet<long>();
				_addedClustersInitialized = true;
			}
			return _addedClusters;
		}
		set {
			_addedClusters = value;
			_addedClustersInitialized = true;
		}
	}

	public ISet<long> ModifiedClusters {
		get {
			if (!_modifiedClustersInitialized) {
				_modifiedClusters = new HashSet<long>();
				if (_hasFirstModifiedCluster)
					_modifiedClusters.Add(_firstModifiedCluster);
				_modifiedClustersInitialized = true;
			}
			return _modifiedClusters;
		}
		set {
			_modifiedClusters = value;
			_modifiedClustersInitialized = true;
			_hasFirstModifiedCluster = false;
		}
	}

	public ISet<long> RemovedClusters {
		get {
			if (!_removedClustersInitialized) {
				_removedClusters = new HashSet<long>();
				_removedClustersInitialized = true;
			}
			return _removedClusters;
		}
		set {
			_removedClusters = value;
			_removedClustersInitialized = true;
		}
	}

	public BijectiveDictionary<long, long> MovedClusters {
		get {
			if (!_movedClustersInitialized) {
				_movedClusters = new BijectiveDictionary<long, long>();
				_movedClustersInitialized = true;
			}
			return _movedClusters;
		}
		set {
			_movedClusters = value;
			_movedClustersInitialized = true;
		}
	}

	public IDictionary<long, (long? NewStart, long? NewEnd)> MovedTerminals {
		get {
			if (!_movedTerminalsInitialized) {
				_movedTerminals = new Dictionary<long, (long? NewStart, long? NewEnd)>();
				_movedTerminalsInitialized = true;
			}
			return _movedTerminals;
		}
		set {
			_movedTerminals = value;
			_movedTerminalsInitialized = true;
		}
	}

	public long? ChainTerminal { get; set; }
	
	public long? ChainOriginalStartCluster { get; set; }

	public long? ChainNewStartCluster { get; set; }

	public long? ChainOriginalEndCluster { get; set; }

	public long? ChainNewEndCluster { get; set; }

	public bool AddedChain => !ChainOriginalStartCluster.HasValue && ChainNewStartCluster.HasValue;
	
	public bool RemovedChain => ChainOriginalStartCluster.HasValue && !ChainNewStartCluster.HasValue;

	public bool IncreasedChainSize => ChainNewEndCluster.HasValue && ClusterCountDelta > 0;

	public bool DecreasedChainSize => ChainNewEndCluster.HasValue && ClusterCountDelta < 0;

	internal bool ClusterLinksChanged { get; set; }

	internal bool HasMovedClusters => _movedClustersInitialized && _movedClusters.Count != 0;

	internal bool HasMovedTerminals => _movedTerminalsInitialized && _movedTerminals.Count != 0;

	public void InformMovedCluster(long from, long to) {
		// Special case: When a cluster A is moved to B and then B is moved to C,
		// it means A is moved to C and B was delted
		if (MovedClusters.TryGetKey(from, out var originalFrom)) {
			RemovedClusters.Add(from);
			MovedClusters.Remove(originalFrom);
			from = originalFrom;
		}

		// Special case: When a cluster A is moved to C and then B is moved to C,
		// it means that A was deleted and B was moved to C
		if (MovedClusters.TryGetKey(to, out originalFrom)) {
			RemovedClusters.Add(originalFrom);
			MovedClusters.Remove(originalFrom);
		}

		MovedClusters.Add(from, to);
	}

	public void InformChainNewStart(long terminal, long cluster) {
		if (!MovedTerminals.TryGetValue(terminal, out var terminalMove)) {
			terminalMove = (null, null);
		}
		terminalMove.NewStart = cluster;
		MovedTerminals[terminal] = terminalMove;
	}

	public void InformChainNewEnd(long terminal, long cluster) {
		if (!MovedTerminals.TryGetValue(terminal, out var terminalMove)) {
			terminalMove = (null, null);
		}
		terminalMove.NewEnd = cluster;
		MovedTerminals[terminal] = terminalMove;
	}

	internal void AddModifiedCluster(long cluster) {
		// Most writes affect one cluster; defer the public mutable set until it is needed.
		if (_modifiedClustersInitialized) {
			_modifiedClusters.Add(cluster);
			return;
		}
		if (!_hasFirstModifiedCluster) {
			_firstModifiedCluster = cluster;
			_hasFirstModifiedCluster = true;
			return;
		}
		if (_firstModifiedCluster != cluster)
			ModifiedClusters.Add(cluster);
	}

	internal bool TryGetMovedCluster(long originalCluster, out long movedCluster) {
		if (_movedClustersInitialized)
			return _movedClusters.TryGetValue(originalCluster, out movedCluster);
		movedCluster = default;
		return false;
	}

	internal bool TryGetMovedTerminal(long terminal, out (long? NewStart, long? NewEnd) movedTerminal) {
		if (_movedTerminalsInitialized)
			return _movedTerminals.TryGetValue(terminal, out movedTerminal);
		movedTerminal = default;
		return false;
	}

	internal IEnumerable<KeyValuePair<long, (long? NewStart, long? NewEnd)>> GetMovedTerminals()
		=> _movedTerminalsInitialized ? _movedTerminals : Array.Empty<KeyValuePair<long, (long? NewStart, long? NewEnd)>>();

	public enum MutationType {
		Added,
		Modified,
		Removed,
	}

}

