# Changelog

## [1.0.0] - 2026-08-27
### Added
- Initial release of UsmAITheme — AI-powered automated game testing tools for Unity-MCP (AI Game Developer).
- Input simulation (old Input Manager based): keyboard press/hold/release, mouse move/click/double-click/drag/scroll, touch begin/move/end.
- MCP tools: `aitest-input-press-key`, `aitest-input-hold-key`, `aitest-input-pointer`, `aitest-input-release-all`.
- Game state inspection: `aitest-state-get` captures loaded scenes, object hierarchy with components, UGUI interactables and visible texts.
- `IGameStateProvider` interface for games to expose custom domain state to the AI.
- `GameInputAgent` scene component forwards simulated pointer input into the UGUI EventSystem and can ship in standalone builds.
