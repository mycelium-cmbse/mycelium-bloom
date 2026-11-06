# Inter web fonts

Official project: <https://rsms.me/inter/> / <https://github.com/rsms/inter>

Release: **4.1**, tag `v4.1`.
Source archive: <https://github.com/rsms/inter/releases/download/v4.1/Inter-4.1.zip>
Archive SHA-256: `9883fdd4a49d4fb66bd8177ba6625ef9a64aa45899767dde3d36aa425756b11e`.

The two WOFF2 files are copied unchanged from the archive's `web/` directory;
`LICENSE.txt` is the complete upstream SIL Open Font License 1.1 from the archive root.

| File | SHA-256 |
| --- | --- |
| `InterVariable.woff2` | `693b77d4f32ee9b8bfc995589b5fad5e99adf2832738661f5402f9978429a8e3` |
| `InterVariable-Italic.woff2` | `e564f652916db6c139570fefb9524a77c4d48f30c92928de9db19b6b5c7a262a` |

Bloom's `inter.css` registers the upstream normal and italic variable faces under
the CSS family **Inter**, matching the existing semantic tokens. Each face supports
weights **100–900**, as declared in the release's `web/inter.css`. The CSS family alias
does not rename or modify the font binaries. No subsetting or font conversion is applied.
The browser's existing optical-sizing behavior is unchanged.

`App.razor` loads the stylesheet through the existing `Assets` resolver. Relative
font URLs resolve within this directory; `MapStaticAssets` serves them. The Web SDK
publishes this directory, including the license and this provenance file. There is
no runtime CDN dependency or `local()` source that could mask a missing web asset.
`font-display: swap` retains readable fallback text while the font downloads.

For a future deliberate update, download the official release archive, review its
license and weight/style declarations, copy the two web fonts and license, and
update these hashes and the hosting regression tests. Verify the published bytes,
requests and actual rendered fonts before updating visual evidence.
