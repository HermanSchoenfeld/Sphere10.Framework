# Framework modules

The module configuration contract through which a package initializes and finalizes its Framework services.

A package that must run code when the Framework starts, such as registering hash functions or signature schemes, provides a class that implements `ICoreModuleConfiguration`, usually by deriving from `CoreModuleConfigurationBase`. The application adds modules with `Sphere10FrameworkBuilder.AddModule` in [Sphere10.Framework.Application](../../Sphere10.Framework.Application/README.md), which calls `OnInitialize` in descending `Priority` order at start and `OnFinalize` at shutdown. The module of `Sphere10.Framework.CryptoEx` is an example that registers its signers and hashers in [Crypto](../Crypto/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CoreModuleConfigurationBase`](CoreModuleConfigurationBase.cs) | abstract class | Default module configuration with priority zero and empty initialize and finalize hooks. |
| [`ICoreModuleConfiguration`](ICoreModuleConfiguration.cs) | interface | Module contract with a `Priority` and `OnInitialize` and `OnFinalize` hooks that receive the service provider. |
