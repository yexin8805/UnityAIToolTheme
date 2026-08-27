/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: yexin (https://github.com/yexin8805)                    │
│  Repository: GitHub (https://github.com/yexin8805/UnityAIToolTheme) │
│  Copyright (c) 2026 yexin                                        │
│  Licensed under the MIT License.                                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘*/

#nullable enable
using System;

namespace com.usm.AITheme.Runtime.Input
{
    /// <summary>Kind of device a simulated input event targets.</summary>
    public enum SimInputDevice
    {
        Keyboard,
        Mouse,
        Touch
    }

    /// <summary>What happens to the key/button when the event is replayed.</summary>
    public enum SimInputPhase
    {
        Press,
        Hold,
        Release
    }

    /// <summary>
    /// One immutable simulated-input instruction, queued into <see cref="InputSimulator"/>
    /// by the MCP tool and replayed by <see cref="GameInputAgent"/> on the frames it
    /// targets. Old Input Manager only — no Input System package dependency.
    /// </summary>
    public readonly struct SimulatedInputEvent : IEquatable<SimulatedInputEvent>
    {
        public SimInputDevice Device { get; }
        public SimInputPhase Phase { get; }

        /// <summary>KeyCode for Keyboard; button index (0=left,1=right,2=middle) for Mouse; finger id for Touch.</summary>
        public int Code { get; }

        /// <summary>Delta/scroll or analog value. Mouse wheel uses y; touch drag uses x/y.</summary>
        public float X { get; }
        public float Y { get; }

        /// <summary>Normalized screen position for Mouse/Touch events.</summary>
        public float ScreenX { get; }
        public float ScreenY { get; }

        /// <summary>Frame at which the event fires. 0 = next Update.</summary>
        public int FrameOffset { get; }

        /// <summary>Seconds from queue time until the event fires. Ignored when FrameOffset &gt; 0.</summary>
        public float DelaySeconds { get; }

        public SimulatedInputEvent(
            SimInputDevice device,
            SimInputPhase phase,
            int code,
            float x = 0f,
            float y = 0f,
            float screenX = 0.5f,
            float screenY = 0.5f,
            int frameOffset = 0,
            float delaySeconds = 0f)
        {
            Device = device;
            Phase = phase;
            Code = code;
            X = x;
            Y = y;
            ScreenX = screenX;
            ScreenY = screenY;
            FrameOffset = frameOffset;
            DelaySeconds = delaySeconds;
        }

        public bool Equals(SimulatedInputEvent other) =>
            Device == other.Device && Phase == other.Phase && Code == other.Code &&
            X.Equals(other.X) && Y.Equals(other.Y) &&
            ScreenX.Equals(other.ScreenX) && ScreenY.Equals(other.ScreenY) &&
            FrameOffset == other.FrameOffset && DelaySeconds.Equals(other.DelaySeconds);

        public override bool Equals(object? obj) => obj is SimulatedInputEvent other && Equals(other);
        public override int GetHashCode()
        {
            // netstandard2.1 HashCode.Combine tops out at 8 args — fold manually.
            var hash = new HashCode();
            hash.Add(Device);
            hash.Add(Phase);
            hash.Add(Code);
            hash.Add(X);
            hash.Add(Y);
            hash.Add(ScreenX);
            hash.Add(ScreenY);
            hash.Add(FrameOffset);
            hash.Add(DelaySeconds);
            return hash.ToHashCode();
        }
        public override string ToString() =>
            $"{Device}.{Phase} code={Code} pos=({ScreenX:F2},{ScreenY:F2}) delta=({X:F2},{Y:F2}) frame+{FrameOffset} delay={DelaySeconds:F2}s";
    }
}
