# Listeners

Holds the NHibernate event listener behind the auditing extension and a checksum helper for business-object buffers.

The audit listener `BusinessObjectAuditor` and its property-index cache `ListenerHelper` are internal. Register the auditor with `AddBusinessObjectAuditor()` from [Extensions](../Extensions/README.md). `BusinessObjectSealer.cs` contains only commented-out code. The only public type is `BufferChecksumCalculator`, which no code in this project currently uses.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BufferChecksumCalculator`](BufferChecksumCalculator.cs) | class | Computes an `int` checksum of a byte buffer with MurmurHash3 and seed 37, meant to be customized per application |
