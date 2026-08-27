/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: yexin (https://github.com/yexin8805)                    │
│  Repository: GitHub (https://github.com/yexin8805/UnityAIToolTheme) │
│  Copyright (c) 2026 yexin                                        │
│  Licensed under the MIT License.                                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘*/

#nullable enable
using System.Collections;
using com.usm.AITheme.Runtime.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.usm.AITheme.Runtime.Tests
{
    /// <summary>
    /// PlayMode tests for the simulated-input pipeline:
    /// queue → InputSimulator.Tick → SimulatedInputState edges/held sets.
    /// These run without a GameInputAgent so they isolate the queue logic.
    /// </summary>
    public class InputSimulatorTests
    {
        [SetUp]
        public void SetUp() => InputSimulator.Reset();

        [TearDown]
        public void TearDown() => InputSimulator.Reset();

        [UnityTest]
        public IEnumerator KeyPress_Releases_AfterHoldDuration()
        {
            const KeyCode key = KeyCode.W;

            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Keyboard, SimInputPhase.Press, (int)key));
            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Keyboard, SimInputPhase.Release, (int)key,
                delaySeconds: 0.2f));

            // Frame 1: press edge + held.
            InputSimulator.Tick();
            Assert.IsTrue(InputSimulator.State.PressedKeys.Contains((int)key),
                "Key should register a Press edge on the frame it fires.");
            Assert.IsTrue(InputSimulator.State.HeldKeys.Contains((int)key),
                "Key should be Held after Press.");
            State_ClearEdges();

            // Still held partway through the delay.
            yield return new WaitForSeconds(0.05f);
            InputSimulator.Tick();
            Assert.IsTrue(InputSimulator.State.HeldKeys.Contains((int)key),
                "Key must stay held during holdSeconds.");
            State_ClearEdges();

            // After the delay: release edge, no longer held.
            yield return new WaitForSeconds(0.25f);
            InputSimulator.Tick();
            Assert.IsTrue(InputSimulator.State.ReleasedKeys.Contains((int)key),
                "Key should register a Release edge after holdSeconds.");
            Assert.IsFalse(InputSimulator.State.HeldKeys.Contains((int)key),
                "Key must not be held after release.");
            Assert.IsFalse(InputSimulator.IsActive, "Simulator should be idle once everything released.");
        }

        [UnityTest]
        public IEnumerator HoldKey_WithoutRelease_StaysHeld()
        {
            const KeyCode key = KeyCode.LeftShift;

            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Keyboard, SimInputPhase.Hold, (int)key));
            InputSimulator.Tick();

            yield return new WaitForSeconds(0.1f);
            InputSimulator.Tick();
            Assert.IsTrue(InputSimulator.State.HeldKeys.Contains((int)key),
                "Hold phase must keep the key down until an explicit Release event.");

            InputSimulator.Reset();
            Assert.IsFalse(InputSimulator.State.HeldKeys.Contains((int)key),
                "Reset must release held keys.");
            Assert.AreEqual(0, InputSimulator.PendingCount, "Reset must drop queued events.");
        }

        [UnityTest]
        public IEnumerator FrameOffset_Event_FiresOnExactFrame()
        {
            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Keyboard, SimInputPhase.Press, (int)KeyCode.A,
                frameOffset: 3));

            // Two real frames must pass without the event firing.
            yield return null;
            yield return null;
            InputSimulator.Tick();
            State_ClearEdges();
            Assert.IsFalse(InputSimulator.State.HeldKeys.Contains((int)KeyCode.A),
                "Event with frameOffset=3 must not fire early.");
            Assert.AreEqual(1, InputSimulator.PendingCount,
                "Pending event must survive until its target frame.");

            // Third frame: the offset arrives, event fires.
            yield return null;
            InputSimulator.Tick();
            Assert.IsTrue(InputSimulator.State.HeldKeys.Contains((int)KeyCode.A),
                "Event must fire on the offset frame.");
            Assert.AreEqual(0, InputSimulator.PendingCount);
        }

        [UnityTest]
        public IEnumerator MouseEvent_UpdatesCursorPosition()
        {
            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Mouse, SimInputPhase.Hold, -1,
                screenX: 0.25f, screenY: 0.75f));
            InputSimulator.Tick();

            Assert.AreEqual(0.25f, InputSimulator.State.CursorPosition.x, 0.001f);
            Assert.AreEqual(0.75f, InputSimulator.State.CursorPosition.y, 0.001f);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Touch_Lifecycle_BeganHoldEnded()
        {
            const int finger = 0;

            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Touch, SimInputPhase.Press, finger, screenX: 0.5f, screenY: 0.5f));
            InputSimulator.Tick();
            Assert.IsTrue(InputSimulator.State.BeganTouches.ContainsKey(finger), "Touch Press should begin a touch.");
            Assert.IsTrue(InputSimulator.State.ActiveTouches.ContainsKey(finger));
            State_ClearEdges();

            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Touch, SimInputPhase.Hold, finger, screenX: 0.6f, screenY: 0.5f));
            InputSimulator.Tick();
            Assert.AreEqual(0.6f, InputSimulator.State.ActiveTouches[finger].x, 0.001f,
                "Touch Hold should move the finger.");
            State_ClearEdges();

            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Touch, SimInputPhase.Release, finger));
            InputSimulator.Tick();
            Assert.IsTrue(InputSimulator.State.EndedTouches.ContainsKey(finger), "Touch Release should end the touch.");
            Assert.IsFalse(InputSimulator.State.ActiveTouches.ContainsKey(finger));
            yield return null;
        }

        /// <summary>Mirrors what GameInputAgent.Update does at the end of each frame.</summary>
        static void State_ClearEdges() => InputSimulator.State.ClearEdges();
    }
}
