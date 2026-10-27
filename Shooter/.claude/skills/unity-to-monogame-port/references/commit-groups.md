# Commit groups

The agent **never stages or commits**. At the end of a phase, write the groups into `Tools/COMMIT-GROUPS.md` as `git add` lists and let the user stage and commit one at a time.

Convention:
- **One phase = two groups: code first, docs second** (`README.md`, `Tools/PORTING-PLAN.md`, `Tools/COMMIT-GROUPS.md`).
- Content changes (regenerated textures, AssetMap, baked scene) that are not code go in their own group, labelled (e.g. "Y2. Content").
- First-time setup splits by pipeline stage: A extraction tooling + reference shots; B static asset import; C skinned characters; D scene bake + loader; E renderer; F wiring + docs. Only the last group builds the whole tree when the Desktop csproj references everything above.
- Platform packaging: icon master + renderer; Desktop packaging; Android project + generated icons + solution entry; docs.
- Note shared files between groups (a file edited by two phases goes in the earlier group that needs it; say so in the later one).
- Leave scratch baselines out until the real reference replaces them.
- Use `git add -u <path>` to stage deletions; add paths explicitly, never `git add -A`.
- Group names are letters (A..Z, AA..) in order across the project.
