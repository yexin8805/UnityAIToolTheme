/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: yexin (https://github.com/yexin8805)                    │
│  Repository: GitHub (https://github.com/yexin8805/UnityAIToolTheme) │
│  Copyright (c) 2026 yexin                                        │
│  Licensed under the MIT License.                                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘*/

#nullable enable
using System.Collections.Generic;

namespace com.usm.AITheme.Runtime.Input
{
    /// <summary>
    /// Mutable per-frame replay state the <see cref="GameInputAgent"/> reads.
    /// Kept as a plain class because it is rebuilt every frame by the simulator
    /// (write-once-per-frame, read-many) and allocating a new dictionary per
    /// frame in Play Mode would be wasteful.
    /// </summary>
    public sealed class SimulatedInputState
    {
        /// <summary>Currently held keys (Keyboard).</summary>
        public HashSet<int> HeldKeys { get; } = new HashSet<int>();

        /// <summary>Keys that were pressed this frame.</summary>
        public HashSet<int> PressedKeys { get; } = new HashSet<int>();

        /// <summary>Keys that were released this frame.</summary>
        public HashSet<int> ReleasedKeys { get; } = new HashSet<int>();

        /// <summary>Currently held mouse buttons.</summary>
        public HashSet<int> HeldMouseButtons { get; } = new HashSet<int>();

        /// <summary>Mouse buttons pressed this frame.</summary>
        public HashSet<int> PressedMouseButtons { get; } = new HashSet<int>();

        /// <summary>Mouse buttons released this frame.</summary>
        public HashSet<int> ReleasedMouseButtons { get; } = new HashSet<int>();

        /// <summary>Normalized cursor position; start = screen center.</summary>
        public UnityEngine.Vector2 CursorPosition { get; internal set; } = new UnityEngine.Vector2(0.5f, 0.5f);

        /// <summary>Scroll delta accumulated this frame (wheel notches).</summary>
        public float ScrollDelta { get; internal set; }

        /// <summary>Active touches: finger id → normalized position.</summary>
        public Dictionary<int, UnityEngine.Vector2> ActiveTouches { get; } = new Dictionary<int, UnityEngine.Vector2>();

        /// <summary>Touches begun this frame: finger id → normalized position.</summary>
        public Dictionary<int, UnityEngine.Vector2> BeganTouches { get; } = new Dictionary<int, UnityEngine.Vector2>();

        /// <summary>Touches ended this frame: finger id → normalized position at release.</summary>
        public Dictionary<int, UnityEngine.Vector2> EndedTouches { get; } = new Dictionary<int, UnityEngine.Vector2>();

        /// <summary>Clears all per-frame edges (pressed/released/began/ended). Called at the end of each Update by the agent.</summary>
        public void ClearEdges()
        {
            PressedKeys.Clear();
            ReleasedKeys.Clear();
            PressedMouseButtons.Clear();
            ReleasedMouseButtons.Clear();
            ScrollDelta = 0f;
            BeganTouches.Clear();
            EndedTouches.Clear();
        }

        /// <summary>Resets everything — used when simulation stops.</summary>
        internal void Reset()
        {
            HeldKeys.Clear();
            HeldMouseButtons.Clear();
            ActiveTouches.Clear();
            ClearEdges();
            CursorPosition = new UnityEngine.Vector2(0.5f, 0.5f);
        }
    }
}
