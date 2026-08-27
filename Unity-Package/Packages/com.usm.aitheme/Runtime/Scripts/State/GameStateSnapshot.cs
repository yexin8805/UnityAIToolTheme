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
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.usm.AITheme.Runtime.State
{
    /// <summary>
    /// Builds a text snapshot of the live game state for the AI to reason over:
    /// loaded scenes, root GameObjects (name, active, component count), UGUI
    /// Canvas trees with interactable states, and every <see cref="IGameStateProvider"/>
    /// found in loaded scenes. Immutable by convention — produce, serialize, discard.
    /// </summary>
    public static class GameStateSnapshot
    {
        /// <summary>Captures the current state of all loaded scenes.</summary>
        public static string Capture(int maxRootObjectsPerScene = 200, int maxChildrenDepth = 4)
        {
            var sb = new StringBuilder(4096);

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                AppendScene(sb, scene, maxRootObjectsPerScene, maxChildrenDepth);
            }

            AppendGameProviders(sb);
            return sb.ToString();
        }

        static void AppendScene(StringBuilder sb, Scene scene, int maxRoot, int maxDepth)
        {
            sb.AppendLine($"## scene '{scene.name}' loaded={scene.isLoaded} path={scene.path}");

            if (!scene.isLoaded)
                return;

            var roots = s_RootBuffer;
            scene.GetRootGameObjects(roots);

            var shown = Mathf.Min(roots.Count, maxRoot);
            if (roots.Count > maxRoot)
                sb.AppendLine($"(showing {shown} of {roots.Count} root objects)");

            for (var i = 0; i < shown; i++)
                AppendObject(sb, roots[i].transform, 0, maxDepth);
        }

        static void AppendObject(StringBuilder sb, Transform t, int depth, int maxDepth)
        {
            if (depth > maxDepth)
                return;

            var indent = new string(' ', depth * 2);
            var components = t.GetComponents<Component>();
            var componentNames = DescribeComponents(components);

            sb.AppendLine($"{indent}- {t.name} {(t.gameObject.activeSelf ? "" : "[inactive]")} [{componentNames}]");

            // UGUI detail: interactability matters to the AI deciding what it can click.
            var selectable = t.GetComponent<UnityEngine.UI.Selectable>();
            if (selectable != null)
                sb.AppendLine($"{indent}  ui: {selectable.GetType().Name} interactable={selectable.interactable}");

            var text = t.GetComponent<UnityEngine.UI.Text>();
            if (text != null)
                sb.AppendLine($"{indent}  text: \"{Truncate(text.text, 80)}\"");

            var tmpText = t.GetComponent("TMPro.TextMeshProUGUI");
            if (tmpText != null)
            {
                var textProp = tmpText.GetType().GetProperty("text");
                var value = textProp?.GetValue(tmpText) as string;
                if (value != null)
                    sb.AppendLine($"{indent}  text: \"{Truncate(value, 80)}\"");
            }

            for (var c = 0; c < t.childCount; c++)
                AppendObject(sb, t.GetChild(c), depth + 1, maxDepth);
        }

        static string DescribeComponents(Component[] components)
        {
            if (components.Length == 0)
                return "no components";

            var names = s_NameBuffer;
            names.Clear();
            const int maxNames = 8;
            for (var i = 0; i < components.Length && names.Count < maxNames; i++)
            {
                var c = components[i];
                if (c == null)
                    continue; // missing script
                var typeName = c.GetType().Name;
                // Strip the generic suffix noise from common UI handlers.
                if (typeName.Length > 0 && !names.Contains(typeName))
                    names.Add(typeName);
            }

            var extra = components.Length - names.Count;
            var joined = string.Join(", ", names);
            return extra > 0 ? $"{joined}, +{extra} more" : joined;
        }

        static void AppendGameProviders(StringBuilder sb)
        {
            var providers = Object.FindObjectsOfType<MonoBehaviour>(includeInactive: true);
            var found = 0;

            sb.AppendLine("## game-state-providers");
            foreach (var mb in providers)
            {
                if (mb is not IGameStateProvider provider)
                    continue;

                found++;
                var entries = provider.CaptureState();
                sb.AppendLine($"- {provider.StateId} ({mb.gameObject.name}):");
                if (entries.Count == 0)
                {
                    sb.AppendLine("  (no entries)");
                    continue;
                }
                foreach (var (key, value) in entries)
                    sb.AppendLine($"  {key} = {Truncate(value, 200)}");
            }

            if (found == 0)
                sb.AppendLine("(none — implement IGameStateProvider on game components to expose domain state)");
        }

        static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max) + "…";

        static readonly List<GameObject> s_RootBuffer = new List<GameObject>(256);
        static readonly List<string> s_NameBuffer = new List<string>(16);
    }
}
