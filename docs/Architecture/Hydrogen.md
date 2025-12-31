# Sphere10 Framework Framework: Complete Architecture & Overview

## Table of Contents

1. [What is Sphere10 Framework](#what-is-Sphere10 Framework)
2. [Framework Composition](#framework-composition)
3. [Key Features & Capabilities](#key-features--capabilities)
4. [Architecture Overview](#architecture-overview)
5. [Deployment Model](#deployment-model)
6. [When to Use Sphere10 Framework](#when-to-use-Sphere10 Framework)
7. [Quick Start Guide](#quick-start-guide)
8. [Related Documentation](#related-documentation)

---

## What is Sphere10 Framework

Sphere10 Framework is a mature, production-ready **.NET 5+ comprehensive framework** designed for building full-stack applications across desktop, mobile, and web platforms. Originally architected for blockchain systems and P2P applications, Sphere10 Framework has evolved into a general-purpose framework offering robust abstractions, advanced data structures, cryptographic primitives, and enterprise-grade utilities for high-performance .NET development.

### Vision & Goals

**Vision**: Provide developers with a unified, extensible foundation for building sophisticated distributed applications with minimal boilerplate, maximum code reuse, and built-in support for scalability, security, and cross-platform deployment.

The Sphere10 Framework framework is composed of 3 sub-frameworks.

### 1. Sphere10 Framework Framework (`Sphere10.Framework.*`)

The foundational layer providing **general-purpose utilities and libraries** usable across any .NET application.

**Domains include**:
- **Collections**: B-trees, merkle trees, paged collections, transactional collections, observable collections
- **Cryptography**: 100+ hash algorithms, digital signatures, encryption, key derivation, post-quantum algorithms
- **Data Access**: Multi-database support (SQL Server, SQLite, Firebird), ORM integration, query building
- **Networking**: TCP, UDP, WebSockets, RPC, pipe-based IPC, P2P abstractions
- **Serialization**: Binary, JSON, XML, streaming formats
- **Caching**: Multi-strategy (LRU, LFU, FIFO), batch, session
- **UI**: Windows Forms and Blazor components, data binding, validation
- **Utilities**: Logging, scheduling, threading, reflection, configuration

**Key Principle**: All code has **zero blockchain dependencies**, making it suitable for any .NET project.

### 2. Helium Framework (`Sphere10.Helium.*`)

An **enterprise service bus (ESB) implementation** for distributed inter-application communication.

**Purpose**: Enable independent applications (Sphere10 Framework-based or otherwise) to collaborate through standardized messaging without tight coupling.

**Features**:
- Publish-Subscribe messaging patterns
- Request-Response communication
- Distributed sagas for multi-step workflows
- Event sourcing capabilities
- Message routing and distribution
- Error handling and compensation

**Positioning**: Usable standalone or integrated with Sphere10 Framework DApps for distributed system communication.

### 3. Sphere10 Framework DApp Framework (`Sphere10.Framework.DApp.*`)

**Blockchain and decentralized application specific functionality**, built atop both Sphere10 Framework Framework and Helium.

**Components**:
- **Sphere10.Framework.DApp.Core**: Block/transaction validation, wallet management, persistence, plugin system
- **Sphere10.Framework.DApp.Node**: Full blockchain node with consensus, P2P networking, JSON APIs, terminal UI
- **Sphere10.Framework.DApp.Host**: Host process for running nodes as services with automatic upgrade support
- **Sphere10.Framework.DApp.Presentation**: Blazor-based GUI framework for DApp interfaces

# Sphere10 Framework Framework: Complete Architecture & Overview

## Table of Contents

1. [What is Sphere10 Framework](#what-is-Sphere10 Framework)
2. [Framework Composition](#framework-composition)
3. [Key Features & Capabilities](#key-features--capabilities)
4. [Architecture Overview](#architecture-overview)
5. [Deployment Model](#deployment-model)
6. [When to Use Sphere10 Framework](#when-to-use-Sphere10 Framework)
7. [Quick Start Guide](#quick-start-guide)
8. [Related Documentation](#related-documentation)

---

## What is Sphere10 Framework

Sphere10 Framework is a mature, production-ready **.NET 5+ comprehensive framework** designed for building full-stack applications across desktop, mobile, and web platforms. Originally architected for blockchain systems and P2P applications, Sphere10 Framework has evolved into a general-purpose framework offering robust abstractions, advanced data structures, cryptographic primitives, and enterprise-grade utilities for high-performance .NET development.

### Vision & Goals

**Vision**: Provide developers with a unified, extensible foundation for building sophisticated distributed applications with minimal boilerplate, maximum code reuse, and built-in support for scalability, security, and cross-platform deployment.

**Core Goals**:
- **Unified Architecture**: Consistent patterns across all platforms and application types
- **Enterprise Quality**: Production-ready, well-tested, thoroughly documented code
- **Extensibility**: Plugin architecture and extension points throughout all layers
- **Performance**: Optimized algorithms, memory efficiency, and concurrent execution
- **Developer Experience**: Intuitive APIs, comprehensive documentation, and clear patterns
- **Cross-Platform**: Single codebase targeting Windows, macOS, iOS, Android, and modern .NET runtimes

### Key Capabilities

Sphere10 Framework provides the following core features:

- **Node**: A fully functional blockchain node with console-based UI, providing data persistence, P2P networking, wallet management, JSON APIs, and standard node functionality
- **GUI**: Blazor-based graphical interface with rich widgets for composing applications
- **In-Protocol Upgrades**: Applications control their own upgrades through blockchain governanceâ€”everything including node, UI, consensus rules, and data structures can be upgraded without requiring native application updates
- **Automatic Interoperability**: Sphere10 Framework applications easily interoperate with one another using complex workflow patterns (publish-subscribe, request-response, sagas) via the Helium framework
- **Plugin Architecture**: Plugins extend both Node and GUI, installed dynamically without recompilation
- **Extensible Design**: Extension points throughout all layers for customization
- **Cross-Platform**: Runs on Windows, macOS, iOS, Android, and any OS supporting .NET 5+

---

## Framework Composition

Sphere10 Framework is not monolithic but rather **three complementary sub-frameworks**, each addressing specific domains:

### 1. Sphere10 Framework Framework (`Sphere10.Framework.*`)

The foundational layer providing **general-purpose utilities and libraries** usable across any .NET application.

**Domains include**:
- **Collections**: B-trees, merkle trees, paged collections, transactional collections, observable collections
- **Cryptography**: 100+ hash algorithms, digital signatures, encryption, key derivation, post-quantum algorithms
- **Data Access**: Multi-database support (SQL Server, SQLite, Firebird), ORM integration, query building
- **Networking**: TCP, UDP, WebSockets, RPC, pipe-based IPC, P2P abstractions
- **Serialization**: Binary, JSON, XML, streaming formats
- **Caching**: Multi-strategy (LRU, LFU, FIFO), batch, session
- **UI**: Windows Forms and Blazor components, data binding, validation
- **Utilities**: Logging, scheduling, threading, reflection, configuration

**Key Principle**: All code has **zero blockchain dependencies**, making it suitable for any .NET project.

### 2. Helium Framework (`Sphere10.Helium.*`)

An **enterprise service bus (ESB) implementation** for distributed inter-application communication.

**Purpose**: Enable independent applications (Sphere10 Framework-based or otherwise) to collaborate through standardized messaging without tight coupling.

**Features**:
- Publish-Subscribe messaging patterns
- Request-Response communication
- Distributed sagas for multi-step workflows
- Event sourcing capabilities
- Message routing and distribution
- Error handling and compensation

**Positioning**: Usable standalone or integrated with Sphere10 Framework DApps for distributed system communication.

### 3. Sphere10 Framework DApp Framework (`Sphere10.Framework.DApp.*`)

**Blockchain and decentralized application specific functionality**, built atop both Sphere10 Framework Framework and Helium.

**Components**:
- **Sphere10.Framework.DApp.Core**: Block/transaction validation, wallet management, persistence, plugin system
- **Sphere10.Framework.DApp.Node**: Full blockchain node with consensus, P2P networking, JSON APIs, terminal UI
- **Sphere10.Framework.DApp.Host**: Host process for running nodes as services with automatic upgrade support
- **Sphere10.Framework.DApp.Presentation**: Blazor-based GUI framework for DApp interfaces

### Sub-Framework Table

| Framework | Naming Convention | Purpose |
|-----------|------------------|---------|
| **Sphere10 Framework Framework** | `Sphere10.Framework.*` | General-purpose modules usable across all application tiers |
| **Helium** | `Sphere10.Helium.*` | Enterprise service bus for inter-application messaging and coordination |
| **Sphere10 Framework DApp** | `Sphere10.Framework.DApp.*` | Blockchain, consensus, nodes, wallets, and P2P functionality |

### Framework Dependencies

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚      Sphere10 Framework DApp Framework                   â”‚
â”‚  (Blockchain & Decentralized Applications)     â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  Core  â”‚  Node  â”‚  Host  â”‚  Presentation       â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”˜
               â”‚                          â”‚
        â”Œâ”€â”€â”€â”€â”€â”€â–¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–¼â”€â”€â”€â”€â”
        â”‚  Helium Framework                    â”‚
        â”‚  (ESB/Messaging/Distribution)        â”‚
        â””â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”˜
               â”‚                     â”‚
        â”Œâ”€â”€â”€â”€â”€â”€â–¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–¼â”€â”€â”€â”€â”
        â”‚  Sphere10 Framework Framework              â”‚
        â”‚  (Core Libraries & Utilities)    â”‚
        â”‚  - Collections, Crypto, Data     â”‚
        â”‚  - Networking, Serialization     â”‚
        â”‚  - Caching, Threading, UI        â”‚
        â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

---

## Key Features & Capabilities

### Foundation Layer (Sphere10 Framework Framework)

#### Advanced Data Structures
- **Collections**: B-trees, merkle trees, paged collections for memory efficiency
- **Streams**: Bit streams, blocking streams, bounded streams, stream pipelines
- **Caching**: Multi-strategy policies, batch operations, session caching
- **Specialized**: Bloom filters, transactional collections, observable collections

#### Cryptography & Security (100+ Algorithms)
- **Hashing**: MD5, SHA (all variants), BLAKE2, CRC, Whirlpool, RIPEMD, Tiger, Groestl
- **Digital Signatures**: ECDSA, EdDSA, RSA, GOST R 34.10
- **Post-Quantum**: Lattice-based algorithms, hash-based signatures
- **Bitcoin**: SECP256k1, Bitcoin-specific implementations
- **Encryption**: AES, ChaCha20, symmetric and asymmetric schemes
- **Key Derivation**: Scrypt, PBKDF2, Argon2, bcrypt

#### Data Persistence
- **Multi-Database**: SQL Server, SQLite, Firebird, NHibernate ORM
- **Connection Pooling**: Optimized connection management
- **Query Building**: Fluent API for complex SQL without raw strings
- **Import/Export**: CSV, file-based storage options

#### Networking & Communication
- **Protocols**: TCP/IP, UDP, WebSockets, HTTP, RPC
- **IPC**: Anonymous pipes, named pipes for process communication
- **P2P**: Foundation for peer-to-peer protocol development
- **Advanced**: Configurable sockets, protocol adapters

#### Rich Serialization
- **Formats**: Custom binary, JSON, XML with versioning
- **Streaming**: Large object serialization via streaming interfaces
- **Type Handling**: Custom type serialization and polymorphism

### Application Framework (Sphere10.Framework.Application)

- **Lifecycle Management**: Standardized startup, configuration, shutdown
- **Dependency Injection**: IoC container with component registration
- **CLI Framework**: Command-line parsing and interface building
- **Configuration**: Environment-based with override support
- **Component Lifecycle**: Hooks for initialization, startup, shutdown

### Desktop UI (Sphere10.Framework.Windows.Forms)

- **Components**: Grids, trees, editors, validators, themed controls
- **Data Binding**: Sophisticated binding between UI and data
- **Validation**: Automatic constraint checking with error display
- **Plugins**: Dynamic UI plugin system
- **Windows Integration**: Registry, services, event logging

### Web & Blazor (Sphere10.Framework.Web.AspNetCore)

- **Components**: Pre-built Blazor components (grids, modals, wizards)
- **Widgets**: Autocomplete, responsive layouts, real-time updates
- **Middleware**: Auth, logging, error handling
- **Routing**: Advanced routing for complex SPAs
- **Forms**: Validation, submission, error recovery

### Cross-Platform Deployment

- **iOS** (Xamarin.iOS): Native iOS apps in C#
- **Android** (Xamarin.Android): Native Android apps in C#
- **macOS** (Xamarin.macOS): Native macOS apps in C#
- **Graphics**: Cross-platform drawing abstractions

---

## Architecture Overview

### 5-Tier Architecture

Sphere10 Framework implements a multi-tier architectural pattern for logical code organization:

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚      Presentation Tier                 â”‚
â”‚  (UI, APIs, Console, Web Applications) â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                â”‚
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚      Communications Tier               â”‚
â”‚  (Networking, IPC, RPC, Messaging)     â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                â”‚
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚      Processing Tier                   â”‚
â”‚  (Business Logic, Algorithms, Rules)   â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                â”‚
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚      Data Tier                         â”‚
â”‚  (Persistence, Databases, Storage)     â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜

â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚      System Tier (Foundational)        â”‚
â”‚  (Collections, Crypto, Utilities)      â”‚
â”‚  Used by all tiers - no dependencies   â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### Domains

Code is organized into **domains** - logical vertical slices spanning multiple tiers:

- **Wallet Domain**: UI + business logic + persistence
- **Consensus Domain**: Rules + validation + storage
- **Mining Domain**: UI + algorithms + networking

See [Domains.md](Domains.md) for complete reference of 30+ framework domains.

### Deployment Model Diagram

![Sphere10 Framework Deployment Model](resources/Sphere10 Framework-Deployment-Host-AppPackage.png)

---

## Deployment Model

Sphere10 Framework applications support **in-protocol upgrades** without requiring user-installed application updates.

### Key Components

1. **Host**: Thin wrapper installed by users (never upgraded)
2. **HAP**: Versioned Sphere10 Framework Application Package bundle
3. **Consensus Databases**: Blockchain and state data (persisted across upgrades)
4. **Node**: Blockchain engine with consensus rules
5. **GUI**: Blazor-based user interface

### Upgrade Flow

```
Initial Setup
  â†“
Host Installs & Runs HAP v1
  â†“
Node Syncs Blockchain, GUI Launches
  â†“
Blockchain Signals Upgrade to HAP v2
  â†“
Host Archives v1, Deploys v2
  â†“
Node v2 Starts, Applies Consensus Updates
  â†“
GUI v2 Launches
```

**Advantage**: Everything upgradeableâ€”consensus rules, data formats, UI, cryptographyâ€”through blockchain governance.

See [Runtime.md](Runtime.md) for complete deployment and runtime details.

---

## When to Use Sphere10 Framework

### Ideal For:

âœ… **Blockchain & DApps**
- Custom layer-1 blockchain systems
- DApps requiring cross-platform deployment  
- Systems needing in-protocol upgrades

âœ… **Distributed Systems**
- P2P applications with multiple nodes
- Microservices requiring inter-process communication
- Backend services with caching and pooling

âœ… **Enterprise Multi-Tier Applications**
- Desktop apps with multiple data sources
- Web apps with complex business logic
- Mixed-platform applications (Windows/Web/Mobile)

âœ… **High-Performance Systems**
- Advanced caching strategies
- Large dataset handling
- Performance-critical cryptography

âœ… **Plugin-Based Applications**
- Extensible desktop applications
- Modular web applications
- Dynamic feature loading

### May Not Be Ideal For:

âŒ **Simple CRUD Applications** - Likely over-engineered

âŒ **Low-Latency Real-Time Systems** - Managed .NET may not meet sub-millisecond requirements

âŒ **Severely Resource-Constrained** - Requires significant memory/storage

---

## Quick Start Guide

### 1. Installation

```bash
dotnet add package Sphere10 Framework
dotnet add package Sphere10.Framework.Application
```

### 2. Basic Application

```csharp
using Sphere10 Framework;
using Sphere10.Framework.Application;

class Program {
    static void Main(string[] args) {
        var app = new Sphere10Application();
        app.Register<IMyService, MyService>();
        app.Initialize();
        app.Run();
    }
}

public interface IMyService {
    void Execute();
}

public class MyService : IMyService {
    public void Execute() => Console.WriteLine("Hello from Sphere10 Framework!");
}
```

### 3. Using Collections

```csharp
// B-Tree for sorted data
var bTree = new BTree<int, string>();
bTree[1] = "First";
bTree[2] = "Second";

// Merkle tree for verified data
var merkleTree = new MerkleTree(hashAlgorithm);
merkleTree.AddLeaf(data);
byte[] root = merkleTree.GetRoot();
```

### 4. Cryptographic Operations

```csharp
// Hashing
var hasher = HashAlgorithmFactory.CreateAlgorithm("SHA256");
byte[] hash = hasher.ComputeHash(data);

// Digital signatures
var signer = new ECDSASigner();
byte[] signature = signer.Sign(data, privateKey);
bool valid = signer.Verify(data, signature, publicKey);

// Key derivation
var kdf = new Scrypt();
byte[] key = kdf.DeriveKey(password, salt, iterations);
```

### 5. Data Access

```csharp
var accessor = new SqlDataAccessor(connectionString);

var query = new QueryBuilder()
    .Select("*")
    .From("Users")
    .Where("Age > @age")
    .OrderBy("Name");

var results = accessor.ExecuteQuery(query.Build());
```

### 6. Networking

```csharp
// TCP Server
var server = new TcpServer();
server.OnClientConnected += HandleClient;
server.Start(IPAddress.Loopback, 8080);

// WebSocket
var client = new WebSocketClient();
await client.ConnectAsync(new Uri("ws://localhost:8080"));
await client.SendAsync("Hello");
```

### 7. Building a DApp

```csharp
// dotnet add package Sphere10.Framework.DApp.Core
// dotnet add package Sphere10.Framework.DApp.Node

public class MyBlockchain : BlockchainNode {
    public override void InitializeConsensusRules() {
        // Custom consensus logic
    }
    
    public override void ValidateBlock(Block block) {
        // Custom validation
    }
}
```

See [DApp Development Guide](../DApp-Guide.md) for comprehensive DApp building instructions.

---

## Related Documentation

### Core Architecture
- **[Domains.md](Domains.md)** - 30+ framework domains reference
- **[Runtime.md](Runtime.md)** - Runtime model, HAP lifecycle, deployment

### Design & Guidelines
- **[3-tier Architecture](../Guidelines/3-tier-Architecture.md)** - Architectural tier definitions
- **[Code Styling](../Guidelines/Code-Styling.md)** - Coding standards and conventions

### Education & Learning
- **[Blockchain Fundamentals](../Education/What-is-Blockchain.md)** - Core blockchain concepts
- **[DApp Development Guide](../DApp-Guide.md)** - Building applications

### Distributed Systems
- **[Helium Framework](../Helium/README.md)** - Enterprise service bus
- **[Helium Architecture](../Helium/Architecture.md)** - Messaging patterns

### User Interface
- **[Presentation Requirements](../PresentationLayer/Sphere10 Framework-Requirements.md)** - UI specifications
- **[Design System](../PresentationLayer/Design/Sphere10 Framework%20Presentation.md)** - Component library

---

## Copyright & Attribution

Copyright Â© 2018-Present Herman Schoenfeld & Sphere 10 Software

All rights reserved. Sphere10 Framework Framework represents years of architectural development for building sophisticated distributed systems.

Each sub-framework addresses specific [domains](Domains.md) relevant to applications. Many domains primarily support other domains, while some are specifically intended for framework consumers. All domains are available for consumption if needed.

### Hydroge Deployment Model

![Sphere10 Framework Deployment Model](resources/Sphere10 Framework-Deployment-Host-AppPackage.png)

### Sphere10 Framework 3-tier Architecture

![Sphere10 Framework 3-tier architecture](../guidelines/resources/Framework-75pct.png)

### Links
1. [Sphere10 Framework Framework Domains](Domains.md#Sphere-10-Framework-Domains)
2. [Helium Domains](Domains.md#Sphere10 Framework-Domains)
3. [Sphere10 Framework Domains](Domains.md#Sphere10 Framework-domains)
4. [Runtime Model](Runtime.md)
5. [3-tier architecture](../Guidelines/3-tier-Architecture.md)
6. [Code Styling Guidelines](../Guidelines/Code-Styling.md)



