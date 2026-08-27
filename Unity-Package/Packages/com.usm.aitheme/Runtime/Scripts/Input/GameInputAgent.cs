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
using UnityEngine;
using UnityEngine.EventSystems;

namespace com.usm.AITheme.Runtime.Input
{
    /// <summary>
    /// Scene component that drives <see cref="InputSimulator"/> each frame and
    /// forwards simulated pointer/touch input into the UGUI <see cref="EventSystem"/>
    /// so UI reacts exactly as it would to real input. Attach one instance to any
    /// persistent GameObject when the MCP input tools are active.
    /// </summary>
    [AddComponentMenu("AI Test/Input Agent (Simulated)")]
    [DisallowMultipleComponent]
    public sealed class GameInputAgent : MonoBehaviour
    {
        /// <summary>Automatically spawned agent instance name.</summary>
        public const string AutoObjectName = "~AI-Test-InputAgent";

        static GameInputAgent? instance;

        /// <summary>Live instance, if a agent exists in the loaded scene(s).</summary>
        public static GameInputAgent? Instance => instance;

        /// <summary>Normalized cursor position this frame (0..1).</summary>
        public Vector2 CursorNormalized => InputSimulator.State.CursorPosition;

        /// <summary>Pixel-space cursor derived from the normalized position.</summary>
        public Vector2 CursorPixels => new Vector2(
            InputSimulator.State.CursorPosition.x * Screen.width,
            InputSimulator.State.CursorPosition.y * Screen.height);

        /// <summary>Ensures exactly one agent exists and returns it. Creates a hidden DontDestroyOnLoad object when missing.</summary>
        public static GameInputAgent EnsureInstance()
        {
            if (instance != null)
                return instance;

            var existing = FindObjectOfType<GameInputAgent>();
            if (existing != null)
            {
                instance = existing;
                return existing;
            }

            var go = new GameObject(AutoObjectName);
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GameInputAgent>();
            return instance;
        }

        void Awake() => instance = this;
        void OnDestroy()
        {
            if (instance == this)
            {
                InputSimulator.Reset();
                instance = null;
            }
        }

        void Update()
        {
            var state = InputSimulator.State;

            // 1. Advance the simulator (fires due events, updates held sets).
            InputSimulator.Tick();

            // 2. Forward to UGUI EventSystem if one is present.
            if (EventSystem.current != null && (state.PressedMouseButtons.Count > 0 ||
                                                state.HeldMouseButtons.Count > 0 ||
                                                state.ReleasedMouseButtons.Count > 0 ||
                                                state.BeganTouches.Count > 0 ||
                                                state.ActiveTouches.Count > 0 ||
                                                state.EndedTouches.Count > 0))
                PumpPointerData(state);

            // 3. Clear per-frame edges for the next frame.
            state.ClearEdges();
        }

        /// <summary>Builds PointerEventData for mouse/touch state and dispatches it into the EventSystem.</summary>
        static void PumpPointerData(SimulatedInputState state)
        {
            var es = EventSystem.current;
            if (es == null)
                return;

            var mousePos = new Vector2(
                state.CursorPosition.x * Screen.width,
                state.CursorPosition.y * Screen.height);

            var pointer = new PointerEventData(es)
            {
                position = mousePos,
                delta = Vector2.zero,
                scrollDelta = new Vector2(0f, state.ScrollDelta),
                button = PointerEventData.InputButton.Left,
            };

            es.RaycastAll(pointer, m_RaycastResults);
            if (m_RaycastResults.Count > 0)
                pointer.pointerCurrentRaycast = m_RaycastResults[0];

            // UGUI public API needs hover/press bookkeeping through ExecuteEvents;
            // synthesize enter/exit against the current raycast target.
            var newHover = pointer.pointerCurrentRaycast.gameObject;
            if (!ReferenceEquals(currentHover, newHover))
            {
                if (currentHover != null)
                    ExecuteEvents.ExecuteHierarchy(currentHover, pointer, ExecuteEvents.pointerExitHandler);
                currentHover = newHover;
                if (currentHover != null)
                    ExecuteEvents.ExecuteHierarchy(currentHover, pointer, ExecuteEvents.pointerEnterHandler);
            }

            var pressedThisFrame = state.PressedMouseButtons.Contains(0) ||
                                   state.BeganTouches.Count > 0;
            var releasedThisFrame = state.ReleasedMouseButtons.Contains(0) ||
                                    state.EndedTouches.Count > 0;

            if (pressedThisFrame && currentHover != null)
            {
                pointer.pressPosition = pointer.position;
                pointer.pointerPressRaycast = pointer.pointerCurrentRaycast;
                ExecuteEvents.ExecuteHierarchy(currentHover, pointer, ExecuteEvents.pointerDownHandler);
            }

            if (releasedThisFrame)
            {
                if (currentHover != null)
                {
                    ExecuteEvents.ExecuteHierarchy(currentHover, pointer, ExecuteEvents.pointerUpHandler);
                    if (pressedThisFrame || lastFramePressed)
                        ExecuteEvents.ExecuteHierarchy(currentHover, pointer, ExecuteEvents.pointerClickHandler);
                }
                lastFramePressed = false;
            }
            else if (pressedThisFrame)
            {
                lastFramePressed = true;
            }

            if (Mathf.Abs(state.ScrollDelta) > Mathf.Epsilon && currentHover != null)
                ExecuteEvents.ExecuteHierarchy(currentHover, pointer, ExecuteEvents.scrollHandler);

            // Touch fallback: begin/end drag notifications for held touches.
            if (state.ActiveTouches.Count > 0 && currentHover != null && lastFramePressed)
                ExecuteEvents.ExecuteHierarchy(currentHover, pointer, ExecuteEvents.dragHandler);
        }

        static readonly List<RaycastResult> m_RaycastResults = new List<RaycastResult>(16);
        static GameObject? currentHover;
        static bool lastFramePressed;
    }
}
