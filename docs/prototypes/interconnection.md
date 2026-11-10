# Interconnection

Bloom’s Interconnection view displays supported structural content from its tab’s
canonical workspace model. Z.Blazor.Diagrams supplies the canvas; Bloom owns canonical identity,
permissions and presentation state. The renderer does not provide persistent model storage.

## Run and use

Install the repository’s .NET 10 SDK and restore with its NuGet configuration:

```sh
dotnet run --project Mycelium.Bloom --no-launch-profile --urls http://localhost:5099
```

Open `/workspace/modeling`. A fresh three-panel workspace retains its original
Browser and middle editor and opens one **Interconnection** in panel 3. Restored
layouts and deliberately closed tabs are not replaced.

Quantities contains no supported Part Usages. Its Interconnection therefore shows
**No structural parts to display**, with a diagram-local **Open model** action.
Choose an already-open compatible model, a self-contained SysML v2 JSON file, or
**Example interconnection model**, then **Open for this session**. The example is
never loaded automatically. A populated view uses **Open another model** locally.

Every deliberate **+ → Interconnection** creates an independent view in that
panel. Opening a model creates that view’s associated Browser in the existing
Browser region; reopening within the same view reuses its pair. The original
Quantities Browser stays usable. Identical content reuses its canonical model
context, while different content with overlapping element IDs stays isolated.
Diagram layout, bends, selection, viewport and Browser search/expansion/scroll
belong to each pair. Details follows the most recently selected canonical object.

Opening is anonymous and session-local. It does not create a Fabric project,
commit, or persistent import. Uploads are streamed with a server-enforced 64 MiB
limit, self-contained identity/reference validation, cycle/depth checks and safe
temporary-file cleanup. Candidates assemble privately before acceptance; failure,
cancellation and abandoned requests preserve existing models and tabs. Closing
the final consumer releases the owned model context. Circuit/session closure
ends this state; it is not durable persistence.

## Supported content and rendering

The projection supports Part Usages, owned Port Usages, nested parts and supported
binary Connection Usages with resolvable endpoints. Unsupported or unresolved
relationships are not invented. Part-only models render without requiring ports
or connections. Occurrence identities remain distinct from canonical element IDs.

Viewport rendering retains full presentation metadata while mounting regional
content. Normal large-file coverage uses 10,000 distinct canonical Part Usages;
it is separate from historical scenes repeating seven semantic parts. The 50%
minimum zoom and formatted-detail cache sizing are configurable settings requiring product
review. Rendering virtualization does not imply backend lazy fetching or bounded
total SDK memory.

SVG/PNG downloads, import/export controls, engine choices and stress tools are
absent from the normal interface. Internal SVG rendering and presentation
serialization remain intact. Ordinary .NET build, publish and Docker workflows
stage the pinned design package and regenerate application CSS with the verified
standalone Tailwind CLI, without invoking Node/npm.

## Remaining integration and release dependencies

Production file loading is eager. Partial fetching requires an operational,
revision-scoped backend source and a supported metadata/dependency adapter.
Live semantic updates require a supported model-change pipeline. Canonical SDK
objects remain owned by their model context; the bounded formatted-detail cache does
not bound those graphs. Inter/JetBrains Mono delivery remains unresolved and
fallback typography must not be described as exact Figma parity.

Rendering, upload validation and editor isolation have regression coverage in the
application test suites. Ordinary CI collects application coverage; local
performance experiments and investigation evidence are not shipped. Required asset generation is documented in `Mycelium.Bloom/Styles/README.md`.

Review the pull request's current CI, Sonar and image-security results before
release. Functional acceptance is not a claim of multi-user capacity or
unrestricted deployment readiness.
