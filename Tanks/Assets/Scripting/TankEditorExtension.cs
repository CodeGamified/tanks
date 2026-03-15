// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using System.Collections.Generic;
using CodeGamified.Editor;

namespace Tanks.Scripting
{
    /// <summary>
    /// Editor extension for Tanks — provides game-specific options
    /// to CodeEditorWindow's option tree.
    /// Mirrors PongEditorExtension.
    /// </summary>
    public class TankEditorExtension : IEditorExtension
    {
        public List<EditorTypeInfo> GetAvailableTypes()
        {
            return new List<EditorTypeInfo>();
        }

        public List<EditorFuncInfo> GetAvailableFunctions()
        {
            return new List<EditorFuncInfo>
            {
                // Queries
                new EditorFuncInfo { Name = "get_my_x",       Hint = "tank X position",         ArgCount = 0 },
                new EditorFuncInfo { Name = "get_my_y",       Hint = "tank Y position",         ArgCount = 0 },
                new EditorFuncInfo { Name = "get_my_heading", Hint = "body heading (degrees)",   ArgCount = 0 },
                new EditorFuncInfo { Name = "get_my_turret",  Hint = "turret angle (degrees)",   ArgCount = 0 },
                new EditorFuncInfo { Name = "get_my_hp",      Hint = "remaining HP",            ArgCount = 0 },
                new EditorFuncInfo { Name = "get_my_ammo",    Hint = "remaining ammo",          ArgCount = 0 },
                new EditorFuncInfo { Name = "get_enemy_x",    Hint = "nearest enemy X",         ArgCount = 0 },
                new EditorFuncInfo { Name = "get_enemy_y",    Hint = "nearest enemy Y",         ArgCount = 0 },
                new EditorFuncInfo { Name = "get_enemy_dist", Hint = "distance to nearest enemy", ArgCount = 0 },
                new EditorFuncInfo { Name = "get_arena_w",    Hint = "arena width",              ArgCount = 0 },
                new EditorFuncInfo { Name = "get_arena_h",    Hint = "arena height",             ArgCount = 0 },

                // Orders
                new EditorFuncInfo { Name = "set_drive",      Hint = "drive (-1..+1)",          ArgCount = 1 },
                new EditorFuncInfo { Name = "set_turn",       Hint = "turn rate (-1..+1)",       ArgCount = 1 },
                new EditorFuncInfo { Name = "set_turret",     Hint = "aim turret (degrees)",     ArgCount = 1 },
                new EditorFuncInfo { Name = "fire",           Hint = "fire projectile",          ArgCount = 0 },

                // Fog of War
                new EditorFuncInfo { Name = "scan",           Hint = "refresh visibility (costs 1 op)", ArgCount = 0 },
                new EditorFuncInfo { Name = "is_visible",     Hint = "is cell visible? → 0/1",  ArgCount = 2 },

                // Obstacle awareness
                new EditorFuncInfo { Name = "get_obstacle_x",    Hint = "nearest obstacle X",     ArgCount = 0 },
                new EditorFuncInfo { Name = "get_obstacle_y",    Hint = "nearest obstacle Y",     ArgCount = 0 },
                new EditorFuncInfo { Name = "get_obstacle_dist", Hint = "distance to nearest obstacle", ArgCount = 0 },

                // Inter-computer data bus
                new EditorFuncInfo { Name = "send",              Hint = "send(channel, value) → shared bus", ArgCount = 2 },
                new EditorFuncInfo { Name = "recv",              Hint = "recv(channel) → value from bus",    ArgCount = 1 },
            };
        }

        public List<EditorMethodInfo> GetMethodsForType(string typeName)
        {
            return new List<EditorMethodInfo>();
        }

        public List<string> GetVariableNameSuggestions()
        {
            return new List<string>
            {
                "ex", "ey", "dx", "dy", "dist", "angle",
                "heading", "turret", "hp", "ammo", "target_angle",
                "visible", "ox", "oy", "odist", "ch", "bus_val"
            };
        }

        public List<string> GetStringLiteralSuggestions()
        {
            return new List<string>();
        }
    }
}
