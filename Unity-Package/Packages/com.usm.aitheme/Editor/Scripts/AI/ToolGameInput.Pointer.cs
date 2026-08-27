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
        public const string PointerToolId = "aitest-input-pointer";

        [AiTool(
            PointerToolId,
            Title = "AI Test / Input / Pointer",
            ReadOnlyHint = false,
            IdempotentHint = false)]
        [Description("Simulates mouse actions: move the cursor, click, drag, or scroll. " +
                     "Position is normalized 0..1 (x right, y up from bottom-left); e.g. center = 0.5,0.5. " +
                     "Clicks are forwarded into the UGUI EventSystem, so UI buttons react exactly as with real input.")]
        public string Pointer(
            [Description("Normalized screen x, 0..1. Default 0.5 (center).")]
            float x = 0.5f,
            [Description("Normalized screen y, 0..1 (0 = bottom). Default 0.5 (center).")]
            float y = 0.5f,
            [Description("Action: 'move' (default), 'click', 'double-click', 'drag', 'release', 'scroll'.")]
            string action = "move",
            [Description("Mouse button: 0 = left (default), 1 = right, 2 = middle.")]
            int button = 0,
            [Description("Scroll notches for action='scroll'. Positive = up/away. Default 1.")]
            float scrollDelta = 1f,
            [Description("Seconds a 'drag' button stays held before release. Default 0.5.")]
            float holdSeconds = 0.5f)
        {
            return MainThread.Instance.Run(() =>
            {
                if (!UnityEngine.Application.isPlaying)
                    return "Error: not in Play Mode. Enter Play Mode first.";

                var pos = ClampNormalized(x, y);
                Runtime.Input.GameInputAgent.EnsureInstance();

                switch (action.ToLowerInvariant())
                {
                    case "move":
                        Runtime.Input.InputSimulator.Enqueue(new Runtime.Input.SimulatedInputEvent(
                            Runtime.Input.SimInputDevice.Mouse, Runtime.Input.SimInputPhase.Hold, -1,
                            screenX: pos.x, screenY: pos.y));
                        return $"Cursor moved to ({pos.x:F2}, {pos.y:F2}).";

                    case "click":
                        Runtime.Input.InputSimulator.Enqueue(new Runtime.Input.SimulatedInputEvent(
                            Runtime.Input.SimInputDevice.Mouse, Runtime.Input.SimInputPhase.Press, button,
                            screenX: pos.x, screenY: pos.y));
                        Runtime.Input.InputSimulator.Enqueue(new Runtime.Input.SimulatedInputEvent(
                            Runtime.Input.SimInputDevice.Mouse, Runtime.Input.SimInputPhase.Release, button,
                            screenX: pos.x, screenY: pos.y, delaySeconds: 0.05f));
                        return $"Queued click (button {button}) at ({pos.x:F2}, {pos.y:F2}).";

                    case "double-click":
                        for (var i = 0; i < 2; i++)
                        {
                            var offset = i * 0.12f;
                            Runtime.Input.InputSimulator.Enqueue(new Runtime.Input.SimulatedInputEvent(
                                Runtime.Input.SimInputDevice.Mouse, Runtime.Input.SimInputPhase.Press, button,
                                screenX: pos.x, screenY: pos.y, delaySeconds: offset));
                            Runtime.Input.InputSimulator.Enqueue(new Runtime.Input.SimulatedInputEvent(
                                Runtime.Input.SimInputDevice.Mouse, Runtime.Input.SimInputPhase.Release, button,
                                screenX: pos.x, screenY: pos.y, delaySeconds: offset + 0.05f));
                        }
                        return $"Queued double-click (button {button}) at ({pos.x:F2}, {pos.y:F2}).";

                    case "drag":
                        Runtime.Input.InputSimulator.Enqueue(new Runtime.Input.SimulatedInputEvent(
                            Runtime.Input.SimInputDevice.Mouse, Runtime.Input.SimInputPhase.Press, button,
                            screenX: pos.x, screenY: pos.y));
                        Runtime.Input.InputSimulator.Enqueue(new Runtime.Input.SimulatedInputEvent(
                            Runtime.Input.SimInputDevice.Mouse, Runtime.Input.SimInputPhase.Release, button,
                            screenX: pos.x, screenY: pos.y, delaySeconds: holdSeconds));
                        return $"Queued drag hold (button {button}) for {holdSeconds:F2}s at ({pos.x:F2}, {pos.y:F2}). " +
                               "Issue pointer 'move' actions between press and release to trace the drag path.";

                    case "release":
                        Runtime.Input.InputSimulator.Enqueue(new Runtime.Input.SimulatedInputEvent(
                            Runtime.Input.SimInputDevice.Mouse, Runtime.Input.SimInputPhase.Release, button,
                            screenX: pos.x, screenY: pos.y));
                        return $"Queued release of mouse button {button}.";

                    case "scroll":
                        Runtime.Input.InputSimulator.Enqueue(new Runtime.Input.SimulatedInputEvent(
                            Runtime.Input.SimInputDevice.Mouse, Runtime.Input.SimInputPhase.Hold, -1,
                            x: 0f, y: scrollDelta, screenX: pos.x, screenY: pos.y));
                        return $"Queued scroll {scrollDelta} notches at ({pos.x:F2}, {pos.y:F2}).";

                    default:
                        return $"Error: unknown action '{action}'. Use move/click/double-click/drag/release/scroll.";
                }
            });
        }

        static (float x, float y) ClampNormalized(float x, float y) => (
            UnityEngine.Mathf.Clamp01(x),
            UnityEngine.Mathf.Clamp01(y));
    }
}
