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
    public partial class Tool_GameInput
    {
        public const string ReleaseAllToolId = "aitest-input-release-all";

        [AiTool(
            ReleaseAllToolId,
            Title = "AI Test / Input / Release All",
            ReadOnlyHint = false,
            IdempotentHint = true)]
        [Description("Drops every queued input event and releases all held keys, mouse buttons and touches. " +
                     "Call this between test steps or when a scripted input sequence went wrong. " +
                     "Pass confirm=true to actually perform the reset (guards against accidental calls).")]
        public string ReleaseAll(
            [Description("Set to true to perform the reset. Kept as an explicit parameter for MCP client compatibility.")]
            bool confirm = false)
        {
            return MainThread.Instance.Run(() =>
            {
                if (!confirm)
                    return "No-op: pass confirm=true to drop queued events and release all held keys/buttons/touches.";

                var held = Runtime.Input.InputSimulator.State.HeldKeys.Count +
                           Runtime.Input.InputSimulator.State.HeldMouseButtons.Count +
                           Runtime.Input.InputSimulator.State.ActiveTouches.Count;
                Runtime.Input.InputSimulator.Reset();
                return $"Input simulation reset: {held} held key/button/touch released, queue cleared.";
            });
        }
    }
}
