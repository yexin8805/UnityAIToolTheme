/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: yexin (https://github.com/yexin8805)                    │
│  Repository: GitHub (https://github.com/yexin8805/UnityAIToolTheme) │
│  Copyright (c) 2026 yexin                                        │
│  Licensed under the MIT License.                                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘*/

#nullable enable
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.usm.AITheme.Editor.AI
{
    /// <summary>
    /// Partial tool class: runtime game-state inspection. Lets the AI read what
    /// the game currently looks like (scenes, objects, UI, custom domain state)
    /// and base the next input step on that state.
    /// </summary>
    [AiToolType]
    public partial class Tool_GameState
    {
        public const string GetToolId = "aitest-state-get";

        [AiTool(
            GetToolId,
            Title = "AI Test / State / Get",
            ReadOnlyHint = true,
            IdempotentHint = true)]
        [Description("Captures the live game state as readable text: loaded scenes, root objects with components, " +
                     "UGUI interactables and visible texts, plus custom domain state from any IGameStateProvider. " +
                     "Use after each input step to decide the next one — this is the AI's 'eyes' on the game.")]
        public string Get(
            [Description("Max root objects listed per scene. Default 200.")]
            int maxRootObjects = 200,
            [Description("Max hierarchy depth traversed. Default 4.")]
            int maxDepth = 4)
        {
            return MainThread.Instance.Run(() =>
            {
                if (!UnityEngine.Application.isPlaying)
                    return "Note: not in Play Mode — showing edit-mode scene state.";

                if (maxRootObjects <= 0 || maxDepth <= 0)
                    return "Error: maxRootObjects and maxDepth must be greater than zero.";

                return Runtime.State.GameStateSnapshot.Capture(maxRootObjects, maxDepth);
            });
        }
    }
}
