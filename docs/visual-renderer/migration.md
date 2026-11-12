# Framework integration

The renderer is now `Sphere10.Framework.Web.VisualRenderer`: its package ID,
assembly and public namespace use that name. Source is in
`src/Sphere10.Framework.Web.VisualRenderer`, tests in
`tests/Sphere10.Framework.Web.VisualRenderer.Tests`, and documentation here.
Both Framework solutions include the library and tests. The standard Framework
pack/publish workflow produces its NuGet and symbol packages with Framework's version.

The import originates from the MIT-licensed `Sphere10.VisualRenderer` repository,
commit `12a28e7eba683719047e9b4c13db414b73531932`, together with pending renderer
enhancements. Copyright and third-party notices remain intact. Embedded templates,
assets, default/CMS rendering and public model structure retain their ownership;
this move does not introduce a dependency on Notion or LocalNotion.

Consumers replace `using Sphere10.VisualRenderer` with
`using Sphere10.Framework.Web.VisualRenderer` and reference the new project or
package. LocalNotion now uses its existing `Sphere10Library` source/package
mechanism for this dependency. The separate renderer source pin and build switches
are retired. Its native CI and Docker source checkout must use a Framework commit
that contains this project; a pre-migration pin cannot build the updated application.

Existing generated CDN URLs remain unchanged. Moving the C# owner does not move
public asset URLs or invalidate deployed sites. Future asset updates originate
from this Framework project.
