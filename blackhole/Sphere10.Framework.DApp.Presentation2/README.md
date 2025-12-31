<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.DApp.Presentation2

Alternative presentation layer implementation for Sphere10 Framework DApps with advanced UI patterns and features.

## 📋 Overview

`Sphere10.Framework.DApp.Presentation2` is an experimental/alternative implementation of the DApp presentation layer, exploring advanced UI patterns, wizard frameworks, and interactive components for blockchain applications.

## 🏗️ Architecture

The library includes advanced implementations of:

- **UI Components**: Extended component library
- **Wizard Framework**: Advanced multi-step form builder (DefaultWizardBuilder)
- **Modal System**: Enhanced dialog and modal management
- **Grid Controls**: Advanced data grid implementations (BlazorGrid)
- **Application Screens**: Application screen lifecycle management
- **Services**: Enhanced UI services and helpers

## 🚀 Key Features

- **Advanced Wizard Builder**: Fluent API for complex multi-step workflows
- **Specialized Controls**: BlazorGrid and other specialized data controls
- **Modal Framework**: Flexible modal and dialog management
- **Service Integration**: Dependency injection for UI services
- **Event System**: Robust event handling for component communication
- **Responsive Design**: Mobile-first responsive layouts

## 🔧 Example: Wizard Implementation

```csharp
var wizard = new DefaultWizardBuilder<NewWalletModel>()
    .NewWizard("Create Wallet")
    .WithModel(new NewWalletModel())
    .AddStep<WalletNameStep>()
    .AddStep<WalletTypeStep>()
    .AddStep<SummaryStep>()
    .OnFinished(async model => {
        await walletService.CreateAsync(model);
        return Result.Success;
    })
    .Build();
```

## 📦 Dependencies

- **Sphere10.Framework.DApp.Presentation**: Base presentation components
- **Microsoft.AspNetCore.Components**: Blazor framework
- **Microsoft.JSInterop**: JavaScript interop

## 📄 Related Projects

- [Sphere10.Framework.DApp.Presentation](./Sphere10.Framework.DApp.Presentation) - Base presentation library
- [Sphere10.Framework.DApp.Presentation.Loader](./Sphere10.Framework.DApp.Presentation.Loader) - WebAssembly host
- [Sphere10.Framework.DApp.Presentation.WidgetGallery](./Sphere10.Framework.DApp.Presentation.WidgetGallery) - Component showcase

## 📖 Status

This project explores advanced presentation patterns. See the main Sphere10.Framework.DApp.Presentation project for the stable API.


