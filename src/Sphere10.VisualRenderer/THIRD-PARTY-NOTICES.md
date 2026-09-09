# Sphere10.VisualRenderer third-party notices

This document accompanies the Sphere10.VisualRenderer binary distribution. Its
embedded browser libraries, fonts, images and separately restored dependencies
remain subject to their own licenses. The MIT License covering Sphere10.VisualRenderer
does not replace those licenses, grant rights held by third parties, or restrict
rights that their licenses grant. Preserve the applicable copyright, license
and attribution notices when redistributing those components or generated
output that includes them.

Paths beginning with `Themes/` identify the embedded resource paths in the
renderer project. The same resources may be supplied through a theme CDN or
exported into a rendered site's asset directory. A component's inclusion in this
list is not a representation that every proposed use or redistribution is
licensed. This document records identified components and known provenance gaps;
it is not an exhaustive legal-clearance certificate.

## Identified components and license notices

| Component | Bundled version or identifying information | Embedded path | License and attribution source |
| --- | --- | --- | --- |
| Bootstrap, including its bundled Popper implementation | Bootstrap 5.3.3, as recorded in the JS/CSS headers; Popper revision is not separately recorded in those headers | `Themes/default/resources/bootstrap/` | MIT. Copyright (c) 2011-2024 The Bootstrap Authors. Popper: Copyright (c) 2019 Federico Zivolo. [Bootstrap license](https://github.com/twbs/bootstrap/blob/v5.3.3/LICENSE), [Popper license](https://github.com/floating-ui/floating-ui/blob/v2.11.8/LICENSE.md). |
| jQuery | 3.7.1 | `Themes/default/resources/jquery/jquery-3.7.1.min.js` | MIT. Copyright OpenJS Foundation and other contributors, https://openjsf.org/. [License](https://github.com/jquery/jquery/blob/3.7.1/LICENSE.txt). |
| DataTables and Bootstrap 5 integration | 2.0.8, as recorded in the combined-file headers | `Themes/default/resources/datatables/` | MIT. Copyright SpryMedia Ltd. Preserve the individual headers in the combined files. [License](https://datatables.net/license/mit). |
| PrismJS and included language definitions | 1.29.0 | `Themes/default/resources/prism/prism.js` and `prism.css` | MIT. Copyright (c) 2012 Lea Verou. [Versioned license](https://github.com/PrismJS/prism/blob/v1.29.0/LICENSE). |
| imagesLoaded, including its packaged event-emitter code | 5.0.0, as recorded in the packaged-file header | `Themes/cms/resources/js/imagesloaded.pkgd.min.js` | MIT. Copyright (c) 2011-2022 David DeSandro and contributors. [Versioned license](https://github.com/desandro/imagesloaded/blob/v5.0.0/LICENSE.md). |
| particles.js | 2.0.0, as recorded in the file header | `Themes/cms/resources/js/particles.min.js` | MIT. Copyright (c) 2015, Vincent Garreau. [License](https://github.com/VincentGarreau/particles.js/blob/master/LICENSE.md). |
| Typed.js | The bundled UMD file is byte-for-byte identical to the files distributed in tags v2.0.16 and v2.1.0 | `Themes/cms/resources/js/typed.umd.js` | MIT under those historical releases. Copyright (c) 2023 Matt Boldt. [v2.1.0 license](https://github.com/mattboldt/typed.js/blob/v2.1.0/LICENSE.txt). Later upstream releases may use different terms; this notice does not apply their license retroactively. |
| MathJax and its bundled resources, including fonts | 3.2.2, as recorded by the embedded MathJax version marker | `Themes/default/resources/mathjax/` | Apache License 2.0. The complete supplied license is reproduced in the package at `third-party/MathJax/LICENSE` and remains embedded at `Themes/default/resources/mathjax/LICENSE`. Preserve notices in the supplied MathJax files. [Project](https://github.com/mathjax/MathJax/tree/3.2.2). |
| classnames code bundled in Feather | Exact classnames version is not recorded in its retained copyright comments | Within `Themes/cms/resources/js/feather.min.js` | MIT. The embedded notice states: Copyright (c) 2016 Jed Watson. [Project](https://github.com/JedWatson/classnames). |
| core-js code bundled in Feather | 3.1.3, as recorded in the embedded metadata | Within `Themes/cms/resources/js/feather.min.js` | MIT. Copyright (c) 2014-2019 Denis Pushkarev. Embedded metadata also states copyright 2019 Denis Pushkarev. [Versioned license](https://github.com/zloirock/core-js/blob/v3.1.3/LICENSE). |
| ES6 Promise code bundled in the X/Twitter widget script | v4.2.5+7f2b526d, as recorded in its embedded notice | Within `Themes/default/resources/x/widgets.js` | MIT for this component only. Copyright (c) 2014 Yehuda Katz, Tom Dale, Stefan Penner and contributors; conversion to ES6 API by Jake Archibald. [Versioned license](https://github.com/stefanpenner/es6-promise/blob/v4.2.5/LICENSE). This does not license the remainder of the X/Twitter bundle. |
| Microsoft.Extensions.FileProviders.Embedded | 10.0.11 is the renderer project's declared NuGet dependency; dependency resolution can supply compatible later versions | Restored as a separate NuGet dependency, not embedded theme source | MIT. Copyright (c) .NET Foundation and Contributors. All rights reserved. Preserve the Microsoft packages' own notices when bundling their binaries. [Package](https://www.nuget.org/packages/Microsoft.Extensions.FileProviders.Embedded/10.0.11), [.NET license](https://github.com/dotnet/runtime/blob/v10.0.0/LICENSE.TXT). |

The copyright notices in the table and the complete MIT permission and warranty
text below together form the MIT notices for the identified MIT components.
They apply separately to each component, with its own copyright holder, and do
not license Sphere10.VisualRenderer itself, which is MIT licensed.

### MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Additional browser components with incomplete version records

The following files identify their upstream projects by their filenames and
browser APIs, but contain no complete upstream version/license banner. Upstream
MIT notices are recorded here for attribution. They do not establish the exact
origin, modifications or complete dependency inventory of the bundled copies.

| Component | Embedded files | Upstream notice |
| --- | --- | --- |
| AOS (Animate On Scroll) | `Themes/cms/resources/js/aos.min.js`; `Themes/cms/resources/css/aos.css` | MIT. Copyright (c) 2015 Michał Sajnóg. [Upstream license](https://github.com/michalsnik/aos/blob/v2.3.4/LICENSE). The bundled JavaScript is not byte-identical to that tag's `dist/aos.js`; the tag is a license reference, not an asserted bundled version. |
| BigPicture | `Themes/cms/resources/js/BigPicture.min.js` | MIT. Copyright (c) 2019 Henry Dollman, https://henrygd.me. [Upstream license](https://github.com/henrygd/bigpicture/blob/master/LICENSE). Exact bundled version is unrecorded. |
| Feather icons | `Themes/cms/resources/js/feather.min.js` | MIT. Copyright (c) 2013-2023 Cole Bemis in the referenced upstream release. [Upstream license](https://github.com/feathericons/feather/blob/v4.29.2/LICENSE). Exact bundled Feather version is unrecorded; do not confuse core-js's embedded 3.1.3 version with the Feather version. |

The MIT permission and warranty text above accompanies these upstream copyright
notices. Retain the component-specific notices already present in their files.

## Isotope: separate GPL or commercial terms

File: `Themes/cms/resources/js/isotope.pkgd.min.js`.

The retained header identifies Isotope PACKAGED v3.0.6, Copyright 2010-2018
Metafizzy, and states that it is licensed under GPLv3 for open-source use or the
Isotope Commercial License for commercial use.

The MIT license for Sphere10.VisualRenderer grants no separate Isotope
commercial rights. Use and redistribution must satisfy an applicable Isotope
license, including any required source, copyright and license obligations if
relying on GPLv3. A commercial license must cover the actual redistribution
model and any rights required by downstream recipients; this document does not
represent that such rights have been obtained. Merely retaining this notice does
not resolve incompatible licensing conditions.

See [Isotope licensing](https://isotope.metafizzy.co/license.html), the
[upstream v3.0.6 distribution](https://github.com/metafizzy/isotope/tree/v3.0.6),
and the [GNU GPL version 3](https://www.gnu.org/licenses/gpl-3.0.html).

## Font Awesome Pro-labeled platform icons

Files:

- `Themes/link_detect_appstores/resources/img/windows.svg`
- `Themes/link_detect_appstores/resources/img/apple.svg`
- `Themes/link_detect_appstores/resources/img/linux.svg`

The retained SVG headers identify Font Awesome Pro 6.3.0, Copyright 2023
Fonticons, Inc., and refer to a Commercial License at
[Font Awesome licensing](https://fontawesome.com/license).

Those headers have been preserved. Appropriate permission for these copies and
their redistribution must be established under the applicable third-party
terms; the Sphere10.VisualRenderer license does not itself grant it. This notice
does not replace the headers with a different license or certify that a
commercial subscription covers downstream redistribution. Platform and product
names and logos remain their respective owners' trademarks.

## X/Twitter widget bundle

File: `Themes/default/resources/x/widgets.js`.

This browser bundle identifies the X/Twitter widget APIs and contains the build
marker `2615f7e52b7e0:1702314776716`. Its retained MIT notice applies to the
embedded ES6 Promise component identified above, not demonstrably to the entire
widget bundle. A complete redistribution license for the entire supplied copy
has not been established by its embedded notices.

The provider's applicable widget, developer and service terms govern their
components and services. Consult [X for Websites documentation](https://developer.x.com/en/docs/twitter-for-websites)
and the [X developer agreement and policy](https://developer.x.com/en/developer-terms/agreement-and-policy).
No permission to redistribute or relicense the complete bundle is inferred from
the MIT notice of an included dependency.

## Store badges and other images with unrecorded provenance

Files:

- `Themes/link_detect_appstores/resources/img/apple-store.svg`
- `Themes/link_detect_appstores/resources/img/google-play.svg`
- `Themes/link_detect_appstores/resources/img/microsoft-store.svg`
- `Themes/cms/resources/img/bx_loader.gif`

These copies do not contain a complete license/copyright notice establishing
their provenance and redistribution rights. The application-store badge names
identify Apple, Google Play and Microsoft Store branding, respectively; that
identification does not assert who created these particular SVG copies or grant
rights in the artwork. Provider badge and trademark conditions may apply.

See the providers' [Apple marketing resources](https://developer.apple.com/app-store/marketing/guidelines/),
[Google Play badge resources](https://play.google.com/intl/en_us/badges/), and
[Microsoft Store badge guidance](https://learn.microsoft.com/en-us/windows/apps/publish/app-marketing-guidelines).
No permissive license is asserted for these files. Their provenance and any
necessary permissions must be resolved before relying on them for a particular
redistribution.

## Redistribution and generated assets

Keep this notice, the renderer license and applicable third-party license files
with redistributed binaries. If generated HTML, themes or exported asset bundles
include third-party software or artwork, preserve the notices and satisfy the
terms for those included components. Rendering user content does not transfer
ownership of that content to the third-party software authors.

Serving an asset through a CDN does not change its license or remove applicable
attribution and permission requirements. A consuming application can use other
themes and lawfully obtained assets, subject to its own licenses and the renderer
license. Additional NuGet/runtime dependencies and application-supplied assets
have their own notices; the consuming distribution must retain those as well.
