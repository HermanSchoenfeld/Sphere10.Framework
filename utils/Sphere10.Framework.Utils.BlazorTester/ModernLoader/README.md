# Second-generation loader migration reference

The second-generation loader was consolidated into [Sphere10.Framework.Utils.BlazorTester](../README.md). Its application shell is in `Sphere10.Framework.Web.AspNetCore.Blazor.UI.MainFrame`; its interactive demonstration is `../Modern/UI/Index.razor` at `/modern`.

The active host uses .NET 10 Interactive Server rendering and an in-memory grid source. The old hardcoded WebSocket transport was replaced by the framework's current `IDataSource<T>` API. The original theme, artwork and font assets are preserved in the tester's `wwwroot/modern` folder. Files remaining here are superseded WebAssembly scaffolding pending cleanup approval and are not active solution projects.
