# Product support requests

The optional transport contract for feature requests, comments, bug reports and crash reports, and the feature-request record.

An application that offers support dialogs registers its own `IProductSupportService`, and the framework supplies no implementation. Each call sends a request record and returns a `Result`. A request can carry up to two `ClientRequestAttachment` files of at most 1 MiB each, which the WinForms support dialogs fill from their attachment selectors. `FeatureRequest` is defined here, and the other records, `BugReport`, `ProductComment` and their base `ClientRequest`, are in [Product information and usage](../Product/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`FeatureRequest`](FeatureRequest.cs) | class | Support request describing a feature the user wants |
| [`IProductSupportService`](IProductSupportService.cs) | interface | Sends feature requests, comments, bug reports and crash reports asynchronously and returns a `Result` |
