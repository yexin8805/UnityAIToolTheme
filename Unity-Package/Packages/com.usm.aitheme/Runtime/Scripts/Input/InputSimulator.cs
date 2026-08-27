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
using System.Collections.Generic;

namespace com.usm.AITheme.Runtime.Input
{
    /// <summary>
    /// Static queue of pending <see cref="SimulatedInputEvent"/>s and the live
    /// <see cref="State"/> the game reads. <see cref="GameInputAgent"/> drains the
    /// queue each Update; the MCP tool layer only ever enqueues immutable events,
    /// so no locking is needed (both sides run on the main thread).
    /// </summary>
    public static class InputSimulator
    {
        static readonly List<PendingEvent> pending = new List<PendingEvent>();

        sealed class PendingEvent
        {
            public SimulatedInputEvent Event;
            public float FireAtTime;
            public int FireAtFrame;
        }

        /// <summary>Live replay state — written by the simulator, read by game code.</summary>
        public static SimulatedInputState State { get; } = new SimulatedInputState();

        /// <summary>Number of events waiting to fire.</summary>
        public static int PendingCount => pending.Count;

        /// <summary>True when any events are queued or any key/button/touch is held.</summary>
        public static bool IsActive =>
            pending.Count > 0 ||
            State.HeldKeys.Count > 0 ||
            State.HeldMouseButtons.Count > 0 ||
            State.ActiveTouches.Count > 0;

        /// <summary>Queues an event for replay. Frame offsets win over delays when both are set.</summary>
        public static void Enqueue(in SimulatedInputEvent evt)
        {
            pending.Add(new PendingEvent
            {
                Event = evt,
                FireAtTime = UnityEngine.Time.unscaledTime + evt.DelaySeconds,
                FireAtFrame = UnityEngine.Time.frameCount + Math.Max(0, evt.FrameOffset),
            });
        }

        /// <summary>Queues a batch of events preserving order.</summary>
        public static void EnqueueRange(IEnumerable<SimulatedInputEvent> events)
        {
            foreach (var evt in events)
                Enqueue(evt);
        }

        /// <summary>Drops all queued events and releases every held key/button/touch.</summary>
        public static void Reset()
        {
            pending.Clear();
            State.Reset();
        }

        /// <summary>
        /// Called by <see cref="GameInputAgent"/> once per Update, before game code reads input.
        /// Applies every event whose time/frame has arrived, in queue order, mutating
        /// <see cref="State"/> accordingly. Public so PlayMode tests can drive frames manually.
        /// </summary>
        public static void Tick()
        {
            var now = UnityEngine.Time.unscaledTime;
            var frame = UnityEngine.Time.frameCount;

            for (var i = pending.Count - 1; i >= 0; i--)
            {
                var p = pending[i];
                var due = p.Event.FrameOffset > 0
                    ? frame >= p.FireAtFrame
                    : now >= p.FireAtTime;
                if (!due)
                    continue;

                Apply(p.Event);
                pending.RemoveAt(i);
            }
        }

        static void Apply(in SimulatedInputEvent evt)
        {
            switch (evt.Device)
            {
                case SimInputDevice.Keyboard:
                    ApplyKey(evt);
                    break;

                case SimInputDevice.Mouse:
                    ApplyMouse(evt);
                    break;

                case SimInputDevice.Touch:
                    ApplyTouch(evt);
                    break;
            }
        }

        static void ApplyKey(in SimulatedInputEvent evt)
        {
            switch (evt.Phase)
            {
                case SimInputPhase.Press:
                    if (State.HeldKeys.Add(evt.Code))
                        State.PressedKeys.Add(evt.Code);
                    break;
                case SimInputPhase.Release:
                    if (State.HeldKeys.Remove(evt.Code))
                        State.ReleasedKeys.Add(evt.Code);
                    break;
                case SimInputPhase.Hold:
                    State.HeldKeys.Add(evt.Code);
                    break;
            }
        }

        static void ApplyMouse(in SimulatedInputEvent evt)
        {
            State.CursorPosition = new UnityEngine.Vector2(evt.ScreenX, evt.ScreenY);

            if (Mathf_Approx(evt.X, 0f) || Mathf_Approx(evt.Y, 0f))
                State.ScrollDelta += evt.Y;

            switch (evt.Phase)
            {
                case SimInputPhase.Press:
                    if (State.HeldMouseButtons.Add(evt.Code))
                        State.PressedMouseButtons.Add(evt.Code);
                    break;
                case SimInputPhase.Release:
                    if (State.HeldMouseButtons.Remove(evt.Code))
                        State.ReleasedMouseButtons.Add(evt.Code);
                    break;
                case SimInputPhase.Hold:
                    State.HeldMouseButtons.Add(evt.Code);
                    break;
            }
        }

        static void ApplyTouch(in SimulatedInputEvent evt)
        {
            var pos = new UnityEngine.Vector2(evt.ScreenX, evt.ScreenY);
            switch (evt.Phase)
            {
                case SimInputPhase.Press:
                    State.ActiveTouches[evt.Code] = pos;
                    State.BeganTouches[evt.Code] = pos;
                    break;
                case SimInputPhase.Hold:
                    State.ActiveTouches[evt.Code] = pos;
                    break;
                case SimInputPhase.Release:
                    if (State.ActiveTouches.Remove(evt.Code))
                        State.EndedTouches[evt.Code] = pos;
                    break;
            }
        }

        static bool Mathf_Approx(float a, float b) => Math.Abs(a - b) < 1e-6f;
    }
}
