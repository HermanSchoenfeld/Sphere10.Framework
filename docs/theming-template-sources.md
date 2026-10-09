# Blazor theme sources

The framework contains original CSS adapters for the locally owned templates reviewed under `D:/Business/Resources/Web Templates`. The adapters translate colors, typography preferences, chrome, spacing, corner treatments and shadows into Sphere10 semantic theme variables. They use the existing Blazor application shell, menus, tabs, grid, dialogs and controls. They do not embed a template's HTML, JavaScript, Bootstrap distribution, icons, photographs or compiled stylesheet.

Each template family has one stylesheet under `src/Sphere10.Framework.Web.AspNetCore.Blazor/wwwroot/css/themes`. Related color or light/dark variants live in that family's file. Each file imports the common `themes.css` control rules. A host can include the family it uses without including the others. The existing Light, Dark, Blue and Classic blue themes remain available separately.

## Reviewed collections

Paths in this table are relative to `D:/Business/Resources/Web Templates`.

| Theme family and available variants | Source evidence | Source design and adaptation |
|---|---|---|
| Artificial Reason, Artificial Reason Green, Artificial Reason Orange, Artificial Reason Violet | `Artificial-Reason-1.4.6/artificial-reason-gulp/src/less/style.less`, `reason.less`, and `colors/{blue,green,orange,violet}.less`; equivalent Grunt and compiled distributions were also identified | Original accents `#0099da`, `#21ba91`, `#ffb644`, `#9f2cc0`; Open Sans body and light Lato headings; silver gradient header, pale navigation, 2px controls. Bright accents remain navigation selection colors; action text uses darker shades where necessary. |
| Boost | `boost-theme-v140/boost-theme-v140/boost-theme/src/assets/scss/custom/_variables.scss` and `components/{_buttons,_cards,_navbar}.scss`; package version 1.4.0 | `#006aff`, muted `#656e79`, pale `#f8f9fa`; Merriweather Sans body and Merriweather headings; .375rem controls, .5rem panels and soft shadows. |
| HUD, HUD Light | `hud_asp_v2.0/template_asp/HudAsp/src/scss/_variables.scss`, `_variables-dark.scss`, `app/{_app-header,_app-sidebar}.scss`, `ui/_card.scss`; startup template has the same source family | Dark teal `#3cd2a5` over `#1d2835`; light source teal `#249d79`; Chakra Petch, .75px letter spacing, fine outlines, compact 4px controls. Light accent text is darkened to `#167456`. Opaque surfaces and CSS gradients replace texture assets. |
| Landkit | `Landkit-2.2.0/src/scss/theme/_variables.scss`, compared with `Landkit-2.1.0/src/scss/theme/_variables.scss`; `Landkit-2.2.0.zip` is the corresponding archived distribution | `#335eea`, near-white `#f9fbfd`, navy text `#161c2d`, blue-gray borders; HK Grotesk Pro preference, .375rem controls, .5rem panels and generous spacing. Versions 2.1 and 2.2 share this palette and constitute one family. |
| MacAdmin | `MacAdmin-5.3/macadmin/theme/css/style.css`, `widgets.css`, `bootstrap.css`, and `index.html` | Silver header gradients, pale-gray sidebar, compact 3px controls, Helvetica Neue/Arial, link blue `#026b9c`, selection gradient `#1aaef3` to `#0993d3`. Background textures are omitted. |
| MacBeath | `MacAdmin-5.3/macbeath/theme/index.html`, `css/style.css`, `css/red.css` | The bundle contains a separate template titled **MacBeath**. Default red is `#ff5d5e`, hover `#df4c4d`; Arial body, Open Sans headings, white square panels and subtly raised 3px controls. Text accents use `#c6363c`. |
| MacKart | `MacAdmin-5.3/mackart/theme/index.html`, `css/style.css`, `css/red.css` | Separate bundled template titled **MacKart**. Default red navigation `#f25758`, hover `#df4c4d`; Arial/Open Sans, flat compact controls and square panels. Text accents use `#b72f34`; navigation retains the brighter source red. |
| Raleway, Raleway Purple, Raleway Green | `Raleway 2.2/css/style.css`, `css/colors/{purple,green}.css`, with the supplied `Raleway 2.2 Documentation` | Original cyan `#00b7eb`, purple `#9b59b6`, green `#2ecc71`; Open Sans body, Raleway headings, 2px controls, square navigation and light shadows. The bright source colors remain selection surfaces; action text uses `#007fa3`, `#86419f` and `#187d42`. |
| AppStack, AppStack Dark | `appstack-3-0-0-beta1-2.zip` entries `bootstrap5/src/scss/1-variables/_app.scss` and `_dark.scss`; package identifies version 3.0.0-beta1; Bootstrap 4 sources also present | Poppins, blue `#3f80ea`, slate sidebar `#293042`, light background `#f7f9fc`; dark base `#293042`, dark page approximately `#202634`; .2rem controls, .25rem panels, restrained shadows. The source accent fill is `#3f80ea`; action text and active navigation use `#2868c8` to improve text contrast. |

`bootstrap-5.2.0-dist` is the Bootstrap library distribution rather than another template. Raleway's documentation folder is documentation, not a second theme. Duplicate Grunt/Gulp, startup/full, Bootstrap 4/5 and archived/extracted copies do not become duplicate picker entries.

## Typography and assets

The CSS names the original fonts first, followed by local fallback fonts. It does not load Google Fonts or another external resource. Most collections reference their body fonts through Google Fonts and only bundle icon fonts. Their complete typography is available when the host application supplies those fonts under its own font registration and applicable license.

Landkit includes HK Grotesk Pro font files, but its `LICENSE.md` explicitly says the font source cannot be redistributed. Those files are therefore not copied into the framework. The Landkit adapter falls back to Segoe UI and Arial. Boost falls back to Georgia for headings, HUD to Bahnschrift/Segoe UI, and Raleway headings to Trebuchet MS. These fallbacks keep each theme usable offline.

No purchased template assets or licenses are replaced by the framework's MIT notice. The new files are independently authored semantic adapters with the above provenance. The template names identify their source inspiration; the adapters are not complete ports of those products.

## Readability and framework integration

Original bright palette colors are retained as semantic fills as well as selection, chrome and focus colors. Dedicated `-text` tokens retain readable action and validation colors; each fill has a `-contrast` foreground for buttons, badges and other solid surfaces. Muted text is adjusted where the source's decorative colors would be too faint for an application grid or menu. Basic foreground/background pairings were checked during adaptation; browser verification must also cover hover, focus, selection, disabled controls, nested theme providers and light/dark transitions.

Theme files change visual properties only. They share the same application configuration, menu merging, screen lifetimes, navigation guards and grid editing behavior. A new template belongs in its own family stylesheet and the existing theme catalogue, not in a separate application layout or copied control implementation.


## Complete contextual palettes

The second pass inspected each template's contextual button colors, form definitions, card/panel styles and table rules. It extends the same family files rather than adding a second styling system. Bootstrap's primary role maps to `accent`; the remaining roles are `secondary`, `success`, `info`, `warning`, `danger`, `light` and `dark`. Every role has a contrasting foreground. Text and validation use `-text` values separately from those source fills.

| Family | Secondary | Success | Info | Warning | Danger | Light / dark |
|---|---|---|---|---|---|---|
| Artificial Reason | `#cdff76` | `#02c66c` | `#54c8eb` | `#f0ad4e` | `#d9534f` | `#f5f5f5` / `#333333` |
| Boost | `#656e79` | `#0b8447` | `#007faa` | `#ff8c00` | `#e22c2f` | `#f8f9fa` / `#343a40` |
| HUD | `#798e98` | `#3cd2a5` | `#30beff` | `#ff9f0c` | `#ff6060` | `#c0cacf` / `#35434a` |
| HUD Light | `#798e98` | `#249d79` | `#009be3` | `#ff9f0c` | `#e00000` | `#c0cacf` / `#35434a` |
| Landkit | `#506690` | `#42ba96` | `#7c69ef` | `#fad776` | `#df4759` | `#f9fbfd` / `#1b2a4e` |
| MacAdmin, MacBeath, MacKart | `#777777` | `#5cb85c` | `#5bc0de` | `#f0ad4e` | `#d9534f` | `#f5f5f5` / `#333333` |
| Raleway | `#616161` | `#5cb85c` | `#5bc0de` | `#f0ad4e` | `#d9534f` | `#fafafa` / `#232323` |
| AppStack | `#495057` | `#4bbf73` | `#1f9bcf` | `#e5a54b` | `#d9534f` | `#eff2f6` / `#293042` |
| AppStack Dark | `#7f838e` | `#4bbf73` | `#1f9bcf` | `#cc8b37` | `#d9534f` | `#343a4b` / `#eaeaec` |
| Blue and Classic blue | `#858796` | `#1cc88a` | `#36b9cc` | `#f6c23e` | `#e74a3b` | `#f8f9fc` / `#5a5c69` |

Artificial Reason Green, Orange and Violet also retain their source secondary colors: `#ffea76`, `#9dffcd` and `#7eff83`, respectively. Raleway's variants share the contextual colors and vary the accent. The old Bootstrap 3 palettes in the Mac templates and Raleway provide contextual success/info/warning/danger fills; secondary/light/dark are mapped from their existing neutral chrome because Bootstrap 3 did not name those roles. HUD and AppStack Sass mix/lighten expressions are materialized into CSS colors.

Blue and Classic blue use the original repository's `utils/Sphere10.Framework.Utils.BlazorTester/Loader/wwwroot/css/sb-admin-2.css` contextual palette and component dimensions. Light and Dark are the framework's existing native themes; their added contextual colors extend their existing neutral, blue and validation palette rather than claiming a purchased-template origin.

The foregrounds intentionally differ from old template labels where needed for readability. For example, some source buttons placed white text on bright orange, cyan or green. The framework keeps the source fill and chooses a dark foreground instead. Checked semantic fill/foreground pairs meet a 4.5:1 contrast ratio; general component states and compositions are verified separately in browser tests.

## Component treatments absorbed

| Family | Inputs and buttons | Cards and tables | Additional source evidence |
|---|---|---|---|
| Artificial Reason | Inputs have 6px/12px padding, 4px corners and a subtle inset shadow; buttons retain 2px corners. | Light panel headers and subtle panel shadows; neutral striped/hover rows. | `src/less/style.less` form-control, btn-ar and panel rules; `src/less/colors/*.less` contextual colors. |
| Boost | Inputs use .875rem/1rem padding, 2px borders and no shadow. Buttons use .875rem/1.5rem padding, 2px borders, uppercase text, 1px tracking and a 0 7px 16px shadow. | Borderless .5rem cards with the source small shadow; transparent card headers; neutral table surfaces. | `custom/_variables.scss`, `custom/components/_buttons.scss`, `_cards.scss`, `_tables.scss`. |
| HUD | Compact .375rem/.75rem controls with 1px outlines; distinct subtle input and button shadows. | Fine technical panel outlines and low-opacity header/table surfaces. The source's extra card-arrow HTML decoration is not required by the framework controls. | `_variables.scss`, `_variables-dark.scss`, `ui/_card.scss`. |
| Landkit | .8125rem/1.25rem spacing; inputs have no shadow; buttons use weight 700. | Borderless cards, clear headers and very pale `#f9fbfd` striped/hover rows. | `theme/_variables.scss` input/button, card and table sections. |
| MacAdmin | 6px/12px input spacing, inset input shadow and flat buttons. | Silver gradient card headers and restrained panel shadows; pale-gray table states. | `macadmin/theme/css/bootstrap.css`, `style.css` widget/widget-head rules. |
| MacBeath / MacKart | Compact inherited Bootstrap 3 control metrics; MacBeath keeps the raised button treatment and MacKart its flat treatment. | Square panels with subtle inherited panel shadows and light headers. | Each template's `css/bootstrap.css`, `css/style.css` and selected `css/red.css`. |
| Raleway | Square inset inputs and 2px buttons, preserving their different shapes. | 2px panels, a very light shadow, `#fafafa` headers and neutral table states. | `css/style.css`, contextual rules from `css/bootstrap.min.css`. |
| AppStack | Compact .25rem/.7rem controls and .2rem corners. | Borderless .25rem cards, small card shadows and variant-specific table stripes. | ZIP Bootstrap 5 `_app.scss` and `_dark.scss`. |
| Blue / Classic blue | .375rem/.75rem controls and .35rem corners. | Existing .35rem cards, `#f8f9fc` headers and neutral table states. | Existing Loader `sb-admin-2.css`. |

The common bridge consumes these differences through `control-padding-x/y`, `control-border-width`, `control-shadow`, `input-radius`, `button-padding-x/y`, `button-shadow`, `button-text-transform`, `button-letter-spacing`, `button-font-weight`, `card-header-background`, `card-shadow`, `card-border-width`, `table-striped-background` and `table-hover-background` tokens. Family files set only meaningful differences; common defaults handle the rest. Shape, typography and semantic roles remain scoped to the provider, including nested providers and independently themed component previews.
