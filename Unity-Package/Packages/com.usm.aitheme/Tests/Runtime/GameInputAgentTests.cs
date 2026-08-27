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
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace com.usm.AITheme.Runtime.Tests
{
    /// <summary>
    /// End-to-end PlayMode tests: simulated clicks must reach real UGUI buttons
    /// through the GameInputAgent → EventSystem path. Each test builds a minimal
    /// Canvas + Button, queues input, and asserts the button's onClick fired.
    /// </summary>
    public class GameInputAgentTests
    {
        GameObject? canvasRoot;
        int clickCount;
        Button? button;

        [SetUp]
        public void SetUp()
        {
            clickCount = 0;
            InputSimulator.Reset();
            BuildUi();
        }

        [TearDown]
        public void TearDown()
        {
            InputSimulator.Reset();
            if (canvasRoot != null)
                Object.DestroyImmediate(canvasRoot);
        }

        void BuildUi()
        {
            canvasRoot = new GameObject("TestCanvas");
            var canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasRoot.AddComponent<GraphicRaycaster>();

            // EventSystem is required for UGUI input; reuse or create.
            if (EventSystem.current == null)
            {
                var es = new GameObject("TestEventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var buttonGo = new GameObject("TestButton", typeof(RectTransform));
            buttonGo.transform.SetParent(canvasRoot.transform, false);
            var rt = (RectTransform)buttonGo.transform;
            rt.sizeDelta = new Vector2(400f, 300f); // big target, center of screen

            var image = buttonGo.AddComponent<Image>();
            image.color = Color.white; // raycast target must be active

            button = buttonGo.AddComponent<Button>();
            button.onClick.AddListener(() => clickCount++);

            // Button sits at screen center → normalized (0.5, 0.5).
        }

        [UnityTest]
        public IEnumerator EnsureInstance_CreatesSingletonAgent()
        {
            var agent = GameInputAgent.EnsureInstance();

            Assert.IsNotNull(agent);
            Assert.IsNotNull(GameInputAgent.Instance, "Instance property must track the created agent.");
            Assert.AreSame(agent, GameInputAgent.EnsureInstance(),
                "Second EnsureInstance call must return the same agent (singleton).");

            yield return null;

            // Agent survives scene logic via DontDestroyOnLoad; clean up manually.
            if (agent != null)
                Object.Destroy(agent.gameObject);
        }

        [UnityTest]
        public IEnumerator SimulatedClick_FiresUguiButton()
        {
            var agent = GameInputAgent.EnsureInstance();
            yield return null; // let agent Update run once so state settles

            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Mouse, SimInputPhase.Press, 0, screenX: 0.5f, screenY: 0.5f));
            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Mouse, SimInputPhase.Release, 0, screenX: 0.5f, screenY: 0.5f,
                delaySeconds: 0.05f));

            // Press frame + release frame + settle.
            yield return new WaitForSeconds(0.3f);

            Assert.GreaterOrEqual(clickCount, 1,
                "Simulated click at screen center must fire the UGUI Button's onClick.");

            if (agent != null)
                Object.Destroy(agent.gameObject);
        }

        [UnityTest]
        public IEnumerator SimulatedClick_OffButton_DoesNotFire()
        {
            var agent = GameInputAgent.EnsureInstance();
            yield return null;

            // Far corner — outside the 400x300 center button.
            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Mouse, SimInputPhase.Press, 0, screenX: 0.01f, screenY: 0.01f));
            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Mouse, SimInputPhase.Release, 0, screenX: 0.01f, screenY: 0.01f,
                delaySeconds: 0.05f));

            yield return new WaitForSeconds(0.3f);

            Assert.AreEqual(0, clickCount,
                "Click outside the button rect must NOT fire onClick.");

            if (agent != null)
                Object.Destroy(agent.gameObject);
        }

        [UnityTest]
        public IEnumerator AgentOnDestroy_ReleasesAllInput()
        {
            var agent = GameInputAgent.EnsureInstance();
            yield return null;

            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Keyboard, SimInputPhase.Hold, (int)KeyCode.W));
            InputSimulator.Tick();
            InputSimulator.State.ClearEdges();
            Assert.IsTrue(InputSimulator.State.HeldKeys.Contains((int)KeyCode.W),
                "Precondition: key held before agent destruction.");

            if (agent != null)
                Object.Destroy(agent.gameObject);
            yield return null; // OnDestroy runs

            Assert.IsFalse(InputSimulator.State.HeldKeys.Contains((int)KeyCode.W),
                "Agent destruction must reset the input simulator (no stuck keys).");
        }

        [UnityTest]
        public IEnumerator CursorPixels_MapNormalizedToScreen()
        {
            var agent = GameInputAgent.EnsureInstance();
            yield return null;

            InputSimulator.Enqueue(new SimulatedInputEvent(
                SimInputDevice.Mouse, SimInputPhase.Hold, -1, screenX: 0.25f, screenY: 0.75f));
            yield return new WaitForSeconds(0.1f);

            var px = agent.CursorPixels;
            Assert.AreEqual(Screen.width * 0.25f, px.x, 1f,
                "CursorPixels.x must map normalized 0.25 to a quarter of screen width.");
            Assert.AreEqual(Screen.height * 0.75f, px.y, 1f,
                "CursorPixels.y must map normalized 0.75 to three quarters of screen height.");

            if (agent != null)
                Object.Destroy(agent.gameObject);
        }
    }
}
