# Sphere10 Framework Runtime Model

## Overview

The **Sphere10 Framework Runtime** describes how blockchain-based applications (DApps) execute within the framework's specialized deployment model. This is specific to **DApp/blockchain development** and does not apply to general desktop, web, or mobile applications using the core framework.

For general application development, see [Sphere10.Framework](Sphere10.Framework.md).

---

## DApp Application Architecture

A Sphere10 Framework DApp application consists of a distributed system with three core components:

### 1. Host

The **Host** is a lightweight top-level application installed by the user that manages the lifecycle of a **Sphere10 Framework Application Package (HAP)**.

**Responsibilities**:
- Deployment and initialization of HAPs
- Lifecycle management (start, stop, upgrade)
- Process supervision of Node and GUI
- Maintenance of upgrade channels

**Key Property**: The Host remains constant throughout the application lifetime and is never upgraded. This ensures the application can completely auto-upgrade itself through the blockchain consensus mechanism.

### 2. Sphere10 Framework Application Package (HAP)

A **HAP** is a distribution unit containing a complete DApp implementation (Node + GUI).

**Structure**:
```
%root%/
├── hap/                 # Current HAP deployment
│   ├── node/           # Blockchain node executable and libraries
│   └── gui/            # GUI application (Blazor/web-based)
├── consensus-data/     # Blockchain state (blocks, wallets, state DB)
└── logs/               # Application logs
```

**Composition**:
- **Node** — Console application running as Host sub-process, handles blockchain consensus and P2P networking
- **GUI** — Web application (Blazor + Kestrel) running as Node sub-process, provides user interface
- **Consensus Data** — Persistent blockchain files (separate from HAP, survives upgrades)

### 3. Consensus Databases

**Purpose**: Store application state constructed during operation (blocks, transactions, wallet data, state snapshots).

**Key Property**: Not modified by the Host. During HAP upgrades, the application must handle schema changes to existing consensus data.

**Storage Types**:
- **Blockchain streams** — Immutable append-only consensus logs
- **Object spaces** — Mutable state databases with merkle tree tracking
- **SQL databases** — Traditional relational data storage (SQLite, SQL Server, Firebird)

---

## Component Interaction Model

### Process Hierarchy

```
┌─────────────────────────┐
│  Host (User-Installed)  │  ← Never upgraded, maximally thin
└──────────────┬──────────┘
               │ stdin/stdout/IPC pipes
               ↓
        ┌──────────────┐
        │ HAP Node     │  ← Blockchain consensus engine
        │ Process      │  ← Upgradeable via consensus
        └──────┬───────┘
               │ WebSocket / IPC pipes
               ↓
        ┌──────────────┐
        │ HAP GUI      │  ← Blazor web UI
        │ Process      │  ← Upgradeable via consensus
        └──────────────┘
```

### Communication Channels

| Channel | Direction | Purpose |
|---------|-----------|---------|
| **Host → Node (Anonymous Pipe)** | Bidirectional | Lifecycle control, upgrade notification, shutdown coordination |
| **Node → GUI (WebSocket)** | Bidirectional | RPC calls, state updates, event subscription |
| **Node → Consensus Data** | Read/Write | Block validation, transaction processing, wallet state |
| **Remote GUI → Node (WSS)** | Bidirectional | Optional: Remote GUI can connect to public node APIs |

---

## Application Lifecycle

### HAP States

```
Ready → Deploying → Stopped → Loading → Started → Archiving
   ↑                           ↑                         ↓
   └─────────────────────────────────────────────────────┘
              (Upgrade cycle)
```

| State | Description |
|-------|-------------|
| **Ready** | New HAP waiting in `in/` folder for deployment |
| **Deploying** | Previous HAP archived, new HAP unzipped into `hap/` folder |
| **Stopped** | HAP deployed but not running (after shutdown) |
| **Loading** | Host is initializing Node process, upgrades can occur here |
| **Started** | Node and GUI are actively running and processing |
| **Archiving** | Current HAP being compressed for backup/history |

### Upgrade Mechanism

1. **Blockchain Consensus**: Consensus rules determine when an application upgrade should occur
2. **HAP Distribution**: New HAP is distributed to all nodes (via P2P protocol or download URLs)
3. **Host Activation**: Host is notified of pending upgrade via anonymous pipe
4. **Graceful Shutdown**: Current HAP is stopped cleanly, consensus data persisted
5. **Deployment**: New HAP unzipped, old HAP archived
6. **Reinitialization**: New Node process loaded, consensus data migrated if needed
7. **Resume**: New GUI starts and reconnects to resumed blockchain state

**Key Advantage**: Application logic evolves through consensus, not manual deployment.

---

## Process Supervision Model

### Host Protocol

The Host maintains anonymous pipes to the Node for lifecycle supervision:

**Messages**:
- `READY` — Node has started and is ready to accept connections
- `UPGRADE_AVAILABLE` — New HAP available, prepare for update
- `SHUTDOWN` — Graceful shutdown request
- `HEARTBEAT` — Periodic health check

**Automatic Recovery**:
- If Node crashes, Host detects via pipe closure and attempts restart
- If GUI crashes, Node detects and restarts it
- Maximum restart attempts prevent infinite crash loops

---

## Consensus Data Migration

When a HAP is upgraded, the new version must handle any changes to consensus data format:

**Approach 1: In-Place Migration**
```csharp
// New version checks consensus data version
if (dataVersion < 2) {
    // Migrate blocks to new format
    // Update merkle trees
    // Reindex state database
}
```

**Approach 2: Snapshot & Replay**
```csharp
// Export current state
ExportBlockchain(current);

// Import into new version with migration rules applied
ImportBlockchain(migrationRules);
```

**Approach 3: Genesis from Snapshot**
```csharp
// For major version changes, create new blockchain state
// from current snapshot, ignoring old consensus history
```

The framework provides utilities in `Sphere10.Framework.DApp.Core` for all three patterns.

---

## Network Topology

### Node-to-Node (P2P)

Nodes communicate via the framework's P2P protocol (TCP or custom transport):

```
Node A ←→ Node B ←→ Node C ←→ Node D
     \↓       ↓       ↓       ↓/
      └─────  Consensus  ──────┘
         (Block propagation, transaction gossip)
```

### Node-to-GUI

Local GUI connects via WebSocket to local Node:

```
  Browser (localhost:8080)
         ↓
    Blazor App
         ↓
    WebSocket
         ↓
    Local Node (localhost:5000)
```

Remote GUI can optionally connect to a public Node:

```
  Remote Browser
         ↓
    Remote Blazor App
         ↓
    WSS/HTTPS
         ↓
    Public Node (network-accessible)
```

---

## Storage Layout

### Filesystem Structure

```
/application/
├── Sphere10FrameworkHost.exe         ← Host executable (never updates)
├── hap/                              ← Current HAP
│   ├── node/
│   │   ├── Sphere10.Framework.DApp.Node.exe
│   │   ├── Sphere10.Framework.DApp.Core.dll
│   │   └── [other dependencies]
│   └── gui/
│       ├── wwwroot/
│       ├── appsettings.json
│       └── [Blazor files]
├── consensus-data/
│   ├── blockchain.stream             ← Immutable block ledger
│   ├── wallets.db                    ← Wallet state database
│   ├── state.merkletree              ← State merkle tree index
│   └── [other consensus artefacts]
├── archives/
│   ├── hap-v1.0.zip                 ← Previous versions
│   ├── hap-v1.1.zip
│   └── hap-v2.0.zip
└── logs/
    └── application.log
```

---

## Related Documentation

- [Sphere10.Framework Architecture](Sphere10.Framework.md) — Core framework design
- [Framework Domains](Domains.md) — Detailed domain breakdown
- [DApp Development Guide](../DApp-Development-Guide.md) — Building DApps with the framework


