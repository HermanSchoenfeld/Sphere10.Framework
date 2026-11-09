# Product and screen attributes

Assembly attributes that declare a product's identity, links, distribution, help resources and settings secret, and class attributes that declare screen settings and user-editable values.

Apply the assembly attributes in the entry assembly's `AssemblyInfo.cs`. `AssemblyAttributesProductInformationProvider` in [Product information and usage](../Product/README.md) reads them, together with the standard .NET `AssemblyProduct`, `AssemblyCompany`, `AssemblyCopyright` and `AssemblyDescription` attributes, into a `ProductInformation` object. Text values pass through `StringFormatter.FormatEx`, so tokens such as `{CurrentYear}` resolve when the product information is read. Help resources come from the attributes that derive from `AssemblyProductHelpResourceAttribute`.

The Application module reads `AssemblyProductSecretAttribute` from the entry assembly during initialization and uses it as the secret for encrypted settings members, as [Settings](../Settings/README.md#encryption) describes. `SettingsAttribute` and its base `UseSettingsAttribute` declare a screen's settings type, and `UserEditableAttribute` enables the automatic settings editor, as the [Application guide](../README.md#screen-block-and-plugin-preferences) describes.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AssemblyAuthorAttribute`](AssemblyAuthorAttribute.cs) | class | Assembly author `Name` and optional `Email` |
| [`AssemblyCompanyLinkAttribute`](AssemblyCompanyLinkAttribute.cs) | class | Company website URL, read into `ProductInformation.CompanyUrl` |
| [`AssemblyCompanyNumberAttribute`](AssemblyCompanyNumberAttribute.cs) | class | Company registration number, read into `ProductInformation.CompanyNumber` |
| [`AssemblyProductCodeAttribute`](AssemblyProductCodeAttribute.cs) | class | Product identifier `Guid`, given as a `Guid` or a GUID string |
| [`AssemblyProductDistributionAttribute`](AssemblyProductDistributionAttribute.cs) | class | Product `ProductDistribution`, read into the product version's `Distribution` |
| [`AssemblyProductHelp`](AssemblyProductHelp.cs) | class | Assembly attribute with settable `HelpType` and `Path` that does not derive from `AssemblyProductHelpResourceAttribute`, so product information does not read it |
| [`AssemblyProductHelpCHMAttribute`](AssemblyProductHelpCHMAttribute.cs) | class | Declares a compiled HTML help file path as a `HelpType.CHM` resource |
| [`AssemblyProductHelpPDFAttribute`](AssemblyProductHelpPDFAttribute.cs) | class | Declares a PDF manual path as a `HelpType.PDF` resource |
| [`AssemblyProductHelpResourceAttribute`](AssemblyProductHelpResourceAttribute.cs) | abstract class | Base of the help-resource attributes, carrying a `HelpType` and a `Path` |
| [`AssemblyProductLinkAttribute`](AssemblyProductLinkAttribute.cs) | class | Product website URL, read into `ProductInformation.ProductUrl` |
| [`AssemblyProductPurchaseLinkAttribute`](AssemblyProductPurchaseLinkAttribute.cs) | class | Product purchase URL, read into `ProductInformation.ProductPurchaseUrl` |
| [`AssemblyProductSecretAttribute`](AssemblyProductSecretAttribute.cs) | class | Application secret that the module assigns to `EncryptedStringAttribute.ApplicationSharedSecret` |
| [`ContextHelpAttribute`](ContextHelpAttribute.cs) | class | Numeric context-help topic `ID`, declared in the `Sphere10.Framework.Application.Help` namespace |
| [`ProductDistribution`](ProductDistribution.cs) | enum | Release stage: `Alpha`, `Beta`, `ReleaseCandidate` or `InternalRelease`, serialized as `Alpha`, `Beta`, `RC` and `Internal` |
| [`SettingsAttribute`](SettingsAttribute.cs) | class | Declares the `SettingsObject` type of a screen without constructing the screen |
| [`UseSettingsAttribute`](UseSettingsAttribute.cs) | class | Original screen settings declaration, kept as the base of `SettingsAttribute` |
| [`UserEditableAttribute`](UserEditableAttribute.cs) | class | Enables automatic user editing of a class's or struct's public properties |

`Sphere10AssemblyAttributesHelper`, which reads these attributes, is internal.
