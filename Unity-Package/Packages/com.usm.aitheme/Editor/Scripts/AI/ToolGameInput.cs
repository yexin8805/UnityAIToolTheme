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
    /// Partial tool class: simulated game input. One operation per file, matching
    /// the Unity-MCP plugin convention. All operations enqueue immutable events
    /// into the Runtime <c>InputSimulator</c>, which the <c>GameInputAgent</c>
    /// replays over the next frames.
    /// </summary>
    [AiToolType]
    public partial class Tool_GameInput
    {
        public const string PressKeyToolId = "aitest-input-press-key";

        [AiTool(
            PressKeyToolId,
            Title = "AI Test / Input / Press Key",
            ReadOnlyHint = false,
            IdempotentHint = false)]
        [Description("Simulates a keyboard key press (press-and-hold for the given duration, then release). " +
                     "Use during Play Mode so the AI can operate the game like a player. " +
                     "The key stays held for holdSeconds (default 0.1), then releases automatically.")]
        public string PressKey(
            [Description("Unity KeyCode name, e.g. 'W', 'Space', 'Return', 'Escape', 'UpArrow'.")]
            string keyCode,
            [Description("Seconds the key stays held before auto-release. Default 0.1 (a tap).")]
            float holdSeconds = 0.1f)
        {
            return MainThread.Instance.Run(() =>
            {
                if (!UnityEngine.Application.isPlaying)
                    return "Error: not in Play Mode. Enter Play Mode first (the game must be running to receive input).";

                if (!TryParseKeyCode(keyCode, out var code))
                    return $"Error: unknown KeyCode '{keyCode}'. Use a UnityEngine.KeyCode name like 'W', 'Space', 'Return'.";

                Runtime.Input.GameInputAgent.EnsureInstance();
                Runtime.Input.InputSimulator.Enqueue(new Runtime.Input.SimulatedInputEvent(
                    Runtime.Input.SimInputDevice.Keyboard, Runtime.Input.SimInputPhase.Press, (int)code));
                Runtime.Input.InputSimulator.Enqueue(new Runtime.Input.SimulatedInputEvent(
                    Runtime.Input.SimInputDevice.Keyboard, Runtime.Input.SimInputPhase.Release, (int)code,
                    delaySeconds: holdSeconds));

                return $"Queued key '{keyCode}' press+release (hold {holdSeconds:F2}s).";
            });
        }

        internal static bool TryParseKeyCode(string name, out UnityEngine.KeyCode code)
        {
            if (int.TryParse(name, out var numeric))
            {
                // Accept raw KeyCode numbers for exotic keys.
                if (System.Enum.IsDefined(typeof(UnityEngine.KeyCode), numeric))
                {
                    code = (UnityEngine.KeyCode)numeric;
                    return true;
                }
            }
            return System.Enum.TryParse(name, true, out code);
        }
    }
}
