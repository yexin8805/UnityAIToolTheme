# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

Template repo for creating new Unity-MCP extension packages (custom MCP tools
for the [Unity-MCP / AI Game Developer](https://github.com/IvanMurzak/Unity-MCP)
plugin). Boilerplate gets customized by `commands/init.ps1`. Changes here
propagate to every future extension created from it.

**The governing constraint:** placeholder tokens are replaced verbatim by
`init.ps1` — keep them consistent in any file you add or edit:

- `YOUR_PACKAGE_ID`, `YOUR_PACKAGE_ID_LOWERCASE` (also used in asmdef and namespace names, e.g. `YOUR_PACKAGE_ID.Editor`)
- `YOUR_PACKAGE_NAME`, `YOUR_PACKAGE_NAME_INSTALLER`, `YOUR_PACKAGE_NAME_INSTALLER_FILE`
- `YOUR_GITHUB_USERNAME_REPOSITORY`

## Commands

```powershell
# Initialize a new package from the template (destructive — replaces placeholders repo-wide)
./commands/init.ps1 -PackageId "com.company.package" -PackageName "My Package" -GitHubRepository "user/repo"

# Open both Unity projects (Installer + Unity-Package) so .meta files are generated
./commands/open-all-projects-windows.ps1   # or open-all-projects-unix.sh

# Bump the package version in lock-step (package.json + Installer.cs version constant)
./commands/bump-version.ps1 -NewVersion "1.0.1"   # add -WhatIf to preview
./commands/get-version.ps1

# Update the core dependency (com.ivanmurzak.unity.mcp) to its latest GitHub release,
# rewriting package.json + every Unity-Tests packages-lock.json in lock-step
./commands/update-ai-game-developer.ps1   # add -WhatIf to preview

# Version-consistency gate (the same check CI runs); exit 0 clean / 1 violations
python commands/check-versions.py .
```

There is no local C# build/test loop — Unity compiles the code and runs the
tests (NUnit + Unity Test Framework, EditMode + PlayMode) inside the Unity
Editor or in CI.

## Architecture

Four Unity projects in one repo:

- `Unity-Package/` — package source. **Only `Packages/YOUR_PACKAGE_ID_LOWERCASE/` ships**;
  everything else in the repo exists for testing, CI, and showcase.
- `Installer/` — standalone Unity project whose `PackageExporter.ExportPackage`
  builds the `.unitypackage` that gets attached to GitHub Releases.
- `Unity-Tests/{2022.3.62f3,2023.2.22f1,6000.3.1f1}` — multi-version test rigs. Each
  references the package via a `file:` dependency
  (`file:./../../../Unity-Package/Packages/YOUR_PACKAGE_ID_LOWERCASE`), so rig tests
  exercise the live package source.
- `commands/` + `.github/workflows/` — automation.

MCP tools are static classes marked `[McpPluginToolType]` with one
`[McpPluginTool]`-attributed operation per method (partial classes, one
operation per file). Unity API calls must go through
`MainThread.Instance.RunAsync(...)`. Editor tools (Editor API access, not in
builds) go under the package's `Editor/`; Runtime tools (no Editor API, ship in
builds) under `Runtime/`.

C# convention: every file starts with the copyright box comment and `#nullable enable`.

## Version invariants (CI-enforced by `check-versions.py`)

- The core pin (`com.ivanmurzak.unity.mcp` in the package's `package.json`) must
  equal the core entry version and every transitive core requirement in each
  `packages-lock.json`. Never bump one file alone — use
  `update-ai-game-developer.ps1`.
- Registry core entries in locks must be `source: "registry"` with the OpenUPM
  URL (supply-chain guard). The `file:`/local entries in this template are the
  exempt scaffold reference.
- Every `Unity-Tests/<version>` folder must appear in the checker's known-good
  list (`KNOWN_GOOD_UNITY_VERSIONS` in `check-versions.py`).
- This template runs only the text-level subset (C1–C4 + C7); the vendored-DLL
  checks (C5/C6) apply to real extensions, not here.

## CI

- `release.yml-sample` / `test_pull_request.yml-sample` are inert until renamed
  (drop `-sample`).
- The `consistency` job in `test_unity_plugin.yml` is deliberately secretless
  and must never be moved under `pull_request_target`.

## Find detail in

- `docs/claude/architecture.md` — layout, tech stack, Editor-vs-Runtime decision, MCP tool pattern, init flow, coding rules
- `docs/claude/release.md` — `bump-version.ps1` mechanics, `bump_version.yml`, release pipeline outputs
- `docs/claude/ci.md` — workflow files, required GitHub secrets, test matrix, PR safety guards
- `README.md` — user-facing setup walkthrough
- `docs/Manual-Package-Rename.md` — manual alternative to `init.ps1`
- `docs/Deploy-OpenUPM.md`, `docs/Deploy-GitHub.md`, `docs/Deploy-npmjs.md` — registry deploy guides
