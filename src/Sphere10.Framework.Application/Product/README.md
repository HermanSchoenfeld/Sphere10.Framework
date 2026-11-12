# Product information and usage

Product identity read from assembly attributes or supplied in code, usage counters, running-instance counts, string-token resolvers and the records of support requests.

`IProductInformationProvider` supplies a `ProductInformation` object. By default `AssemblyAttributesProductInformationProvider` reads it once from the entry assembly's [product attributes](../Attributes/README.md). `Sphere10FrameworkBuilder.UseAssemblyInfo` and `WithProductInformation` select another assembly or a supplied object through `ProductServicesServiceCollectionExtensions`. `IProductServicesProvider` combines product information with `IProductUsageServices` and `IProductInstancesCounter`, and its default implementation, `ProductServicesProvider`, is in [Core application services](../Core/README.md).

`ProductUsageServices` keeps first-run dates and run counts for the current user and for the machine in two `UsageSettings` records, and it saves both on each increment. It also exposes per-user and per-machine dictionaries that are stored as an encrypted JSON string, so they need an application secret, as [Settings](../Settings/README.md#encryption) describes. The three token resolvers let `StringFormatter` and `Tools.Text.FormatEx` expand tokens such as `{ProductName}`, `{UserDataDir}` and `{NumberOfUsesByUser}`. The module registers them when the framework starts. `BugReport` and `ProductComment` are records for the support transport in [Product support requests](../Support/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ApplicationTokenResolver`](ApplicationTokenResolver.cs) | class | Resolves `UserDataDir`, `SystemDataDir`, `StartPath`, `CurrentYear` and the product, company and URL tokens |
| [`AssemblyAttributesProductInformationProvider`](AssemblyAttributesProductInformationProvider.cs) | class | Reads `ProductInformation` once from an assembly's product attributes, defaulting to the entry assembly |
| [`AssemblyInfoProductServicesProvider`](AssemblyInfoProductServicesProvider.cs) | class | `ProductServicesProvider` whose product information comes from a chosen assembly's attributes |
| [`BugReport`](BugReport.cs) | class | Support request carrying the bug description, the exception's diagnostic text and log entries |
| [`ClientRequest`](ClientRequest.cs) | abstract class | Base support request with the sender, `UserType`, description, sender product information, service version and at most `MaxAttachments` (2) attachments |
| [`ClientRequestAttachment`](ClientRequestAttachment.cs) | class | A file attached to a support request, such as a screenshot: its `FileName` and at most `MaxBytes` (1 MiB) of `Data` |
| [`HelpType`](HelpType.cs) | enum | Help resource format: `None`, `PDF`, `CHM`, `RTF` or `URL` |
| [`IProductInformationProvider`](IProductInformationProvider.cs) | interface | Supplies the application's `ProductInformation` |
| [`IProductInstancesCounter`](IProductInstancesCounter.cs) | interface | Counts the running instances of the product |
| [`IProductServicesProvider`](IProductServicesProvider.cs) | interface | Combines product information, usage services and the instance counter in one service |
| [`IProductUsageServices`](IProductUsageServices.cs) | interface | Usage statistics, `IncrementUsageByOne` and encrypted per-user and per-machine property dictionaries |
| [`ProductComment`](ProductComment.cs) | class | Support request carrying a user's comment about the product |
| [`ProductInformation`](ProductInformation.cs) | class | `SettingsObject` with company, product, version, URL, author, copyright and help-resource details |
| [`ProductInformationTokenResolver`](ProductInformationTokenResolver.cs) | class | Resolves the company, product, version, copyright and URL tokens from `ProductInformation` |
| [`ProductUsageInformation`](ProductUsageInformation.cs) | class | First-use dates, days used and run counts for the machine and for the current user |
| [`ProductUsageInformationTokenResolver`](ProductUsageInformationTokenResolver.cs) | class | Resolves the usage tokens, such as `DaysUsedByUser`, through the framework's `IProductUsageServices` |
| [`ProductUsageServices`](ProductUsageServices.cs) | class | `IProductUsageServices` that stores usage counters and encrypted properties in user and global settings |
| [`StandardProductInstancesCounter`](StandardProductInstanceCounter.cs) | class | Counts processes with the same name as the current process |
| [`UserType`](UserType.cs) | enum | Kind of requester: `System`, `HomeUser`, `SmallBusiness`, `MediumBusiness` or `Corporation` |

### Extensions

| Type | Kind | Purpose |
|---|---|---|
| [`ProductServicesServiceCollectionExtensions`](Extensions/ProductServicesServiceCollectionExtensions.cs) | static class | `AddProductServices` and `AddAssemblyInfoProductServices`, which replace the product-services and product-information registrations |
