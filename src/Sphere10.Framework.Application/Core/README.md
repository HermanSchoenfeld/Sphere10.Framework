# Core application services

Running-instance detection, the automatic-start scope enumeration and the default product-services aggregate.

The Application module registers `StandardDuplicateProcessDetector` as the transient `IDuplicateProcessDetector` unless the host registers its own. It counts the processes that share the current process name, including the current process. `ProductServicesProvider` is the default `IProductServicesProvider`. It combines a product-information source with the registered `IProductUsageServices` and `IProductInstancesCounter`, and it resolves those two services only when a caller uses them. Its contracts and the other product services are in [Product information and usage](../Product/README.md). `AutoRunType` is the scope argument of `IAutoRunServices` in [Initialization and finalization tasks](../Lifecycle/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AutoRunType`](AutoRunType.cs) | enum | Automatic-start scope: `AllUsers`, `AllUsersRunOnce`, `CurrentUser` or `CurrentUserRunOnce` |
| [`IDuplicateProcessDetector`](IDuplicateProcessDetector.cs) | interface | Counts the running instances of the current application |
| [`ProductServicesProvider`](ProductServicesProvider.cs) | class | `IProductServicesProvider` that holds product information and resolves usage and instance-count services from the service provider on demand |
| [`StandardDuplicateProcessDetector`](StandardDuplicateProcessDetector.cs) | class | Counts processes with the same name as the current process |
