<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Windows.LevelDB

**LevelDB integration for Windows** providing high-performance embedded key-value storage for blockchain data, indices, and persistent caches.

## 📋 Overview

`Sphere10.Framework.Windows.LevelDB` integrates Google's LevelDB into Sphere10 Framework applications, providing high-performance key-value storage suitable for blockchain ledgers, indices, and persistent caches on Windows platforms.

## 🚀 Key Features

- **LevelDB Integration**: Embedded key-value database
- **Fast Access**: Optimized for read-heavy workloads
- **Compression**: Snappy compression support for reduced storage
- **Batch Operations**: Atomic batch writes
- **Iteration**: Efficient key range iteration with LINQ support
- **Snapshots**: Consistent point-in-time snapshots for concurrent reads
- **Repair & Destroy**: Database maintenance utilities

## 🔧 Usage

### Basic CRUD Operations

Store and retrieve key-value data:

```csharp
using Sphere10.Framework.Windows.LevelDB;

// Create/open database
using (var db = new DB("mydb", new Options { CreateIfMissing = true })) {
	// Write data
	db.Put("Tampa", "green");
	db.Put("London", "red");
	db.Put("New York", "blue");
    
	// Read data
	var value = db.Get("Tampa"); // Returns "green"
    
	// Delete data
	db.Delete("New York");
    
	// Check if exists
	if (db.Get("New York") == null) {
		// Key was deleted
	}
}
```

### Iterating Over Data

Iterate over all key-value pairs (automatically sorted by key):

```csharp
using (var db = new DB("mydb", new Options { CreateIfMissing = true })) {
	// Populate data
	db.Put("Tampa", "green");
	db.Put("London", "red");
	db.Put("New York", "blue");
    
	// Iterate from first key
	using (var iterator = db.CreateIterator(new ReadOptions())) {
		iterator.SeekToFirst();
		while (iterator.IsValid()) {
			var key = iterator.GetStringKey();
			var value = iterator.GetStringValue();
			Console.WriteLine($"{key}: {value}");
			iterator.Next();
		}
	}
    
	// Or use LINQ enumerable (convenient but iterates all keys)
	var allKeys = (from kv in db as IEnumerable<KeyValuePair<string, string>>
				   select kv.Key).ToList();
}
```

### Point-in-Time Snapshots

Create snapshots for consistent reads while data is being modified:

```csharp
using (var db = new DB("mydb", new Options { CreateIfMissing = true })) {
	db.Put("Tampa", "green");
	db.Put("London", "red");
    
	// Create snapshot before making changes
	using (var snapshot = db.CreateSnapshot()) {
		var readOptions = new ReadOptions { Snapshot = snapshot };
        
		// Update data in main database
		db.Put("New York", "blue");
		db.Delete("London");
        
		// Snapshot still sees the old state
		ClassicAssert.AreEqual(db.Get("New York", readOptions), null); // Not yet in snapshot
		ClassicAssert.AreEqual(db.Get("London", readOptions), "red");   // Still visible in snapshot
	}
    
	// Outside snapshot scope, we see the updated data
	ClassicAssert.AreEqual(db.Get("Tampa"), "yellow");      // Visible now
	ClassicAssert.IsNull(db.Get("London"));                  // Deleted
}
```

### Database Maintenance

Repair or destroy databases:

```csharp
// Repair a potentially corrupted database
DB.Repair("mydb", new Options());

// Completely destroy/delete a database
DB.Destroy("mydb", new Options { CreateIfMissing = true });
```

## 📦 Dependencies

- **Sphere10 Framework**: Core framework library
- **LevelDB**: Native LevelDB database engine (BSD License)
- **Snappy**: Compression library

## 📜 License Attribution

This package includes or uses the following third-party components:

### LevelDB
- **License**: BSD 3-Clause License
- **Source**: https://github.com/google/leveldb
- **Notice**: LevelDB is distributed under the BSD 3-Clause License. Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met.

For complete license details, see: https://github.com/google/leveldb/blob/master/LICENSE

## 💡 Use Cases

- **Blockchain State**: Store blockchain blocks, transactions, and account state
- **Indices**: Create fast lookup indices for transaction history
- **Caching**: Persistent cache layer for frequently accessed data
- **Logging**: High-throughput event and transaction logging

## ⚠️ Important Notes

- Keys are stored in **sorted order** (lexicographic by default)
- **Snapshots don't require locking** - they're copy-on-write
- Always use `using` statements to ensure proper database cleanup
- **Single writer** - only one writer at a time, but multiple concurrent readers are supported
- **LevelDB License Compliance**: This package embeds LevelDB native binaries. Redistribution complies with LevelDB's BSD 3-Clause License - see License Attribution section above

## 📄 Related Projects

- [Sphere10.Framework.Data](../Sphere10.Framework.Data) - Data access abstraction
- [Sphere10.Framework.DApp.Core](../Sphere10.Framework.DApp.Core) - Blockchain using storage


