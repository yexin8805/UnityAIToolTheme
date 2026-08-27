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
        public const string HoldKeyToolId = "aitest-input-hold-key";

        [AiTool(
            HoldKeyToolId,
            Title = "AI Test / Input / Hold Key",
            ReadOnlyHint = false,
            IdempotentHint = false)]
        [Description("Simulates holding a keyboard key down (or releasing a held one) WITHOUT auto-release. " +
                     "Use for continuous movement: hold 'W', wait, then release. " +
                     "Pair with aitest-input-release-all to reset.")]
        public string HoldKey(
            [Description("Unity KeyCode name, e.g. 'W', 'LeftShift', 'Space'.")]
            string keyCode,
            [Description("false = press and keep holding (default). true = release the held key.")]
            bool release = false)
        {
            return MainThread.Instance.Run(() =>
            {
                if (!UnityEngine.Application.isPlaying)
                    return "Error: not in Play Mode. Enter Play Mode first.";

                if (!TryParseKeyCode(keyCode, out var code))
                    return $"Error: unknown KeyCode '{keyCode}'.";

                Runtime.Input.GameInputAgent.EnsureInstance();
                var phase = release
                    ? Runtime.Input.SimInputPhase.Release
                    : Runtime.Input.SimInputPhase.Hold;
                Runtime.Input.InputSimulator.Enqueue(new Runtime.Input.SimulatedInputEvent(
                    Runtime.Input.SimInputDevice.Keyboard, phase, (int)code));

                return release
                    ? $"Queued release of '{keyCode}'."
                    : $"Queued hold of '{keyCode}' (call again with release=true to let go).";
            });
        }
    }
}
