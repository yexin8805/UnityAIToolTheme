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
using com.usm.AITheme.Runtime.State;
using NUnit.Framework;
using UnityEngine;

namespace com.usm.AITheme.Runtime.Tests
{
    /// <summary>
    /// PlayMode tests for GameStateSnapshot: scene/object capture and the
    /// IGameStateProvider discovery path.
    /// </summary>
    public class GameStateSnapshotTests
    {
        GameObject? testObject;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("SnapshotTarget");
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
                Object.DestroyImmediate(testObject);
        }

        [Test]
        public void Capture_IncludesSceneAndObjectNames()
        {
            var snapshot = GameStateSnapshot.Capture();

            Assert.IsNotNull(snapshot);
            StringAssert.Contains("scene", snapshot,
                "Snapshot must include a scene header.");
            StringAssert.Contains("SnapshotTarget", snapshot,
                "Snapshot must list root object names.");
            StringAssert.Contains("Transform", snapshot,
                "Snapshot must include component type names.");
        }

        [Test]
        public void Capture_IncludesGameStateProviderEntries()
        {
            var provider = testObject!.AddComponent<FakeStateProvider>();
            var snapshot = GameStateSnapshot.Capture();

            StringAssert.Contains("game-state-providers", snapshot,
                "Snapshot must have a game-state-providers section.");
            StringAssert.Contains(FakeStateProvider.Id, snapshot,
                "Snapshot must include the provider's StateId.");
            StringAssert.Contains("hp = 10", snapshot,
                "Snapshot must include provider key=value entries.");
        }

        [Test]
        public void Capture_TruncatesLongText()
        {
            var longNamed = new GameObject(new string('x', 500));
            try
            {
                // Object names are not truncated (only text values are), but this
                // guards against exceptions on pathological names.
                Assert.DoesNotThrow(() => GameStateSnapshot.Capture());
            }
            finally
            {
                Object.DestroyImmediate(longNamed);
            }
        }

        sealed class FakeStateProvider : MonoBehaviour, IGameStateProvider
        {
            public const string Id = "FakePlayerState";

            public string StateId => Id;

            public IReadOnlyDictionary<string, string> CaptureState()
                => new Dictionary<string, string>
                {
                    ["hp"] = "10",
                    ["alive"] = "true",
                };
        }
    }
}
