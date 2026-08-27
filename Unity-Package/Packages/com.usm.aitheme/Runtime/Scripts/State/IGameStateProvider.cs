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

namespace com.usm.AITheme.Runtime.State
{
    /// <summary>
    /// Implement on any game component to expose domain state to the AI testing
    /// tools. Everything is string-keyed so game code stays decoupled from any
    /// serialization format; the snapshot layer turns entries into JSON-friendly
    /// pairs. Keep values primitive (numbers, bools, short strings) — the AI
    /// reads them, humans debug them.
    /// </summary>
    public interface IGameStateProvider
    {
        /// <summary>Stable identifier shown in snapshots, e.g. "PlayerHealth".</summary>
        string StateId { get; }

        /// <summary>Current entries: key → value. Return an empty map when idle.</summary>
        IReadOnlyDictionary<string, string> CaptureState();
    }
}
