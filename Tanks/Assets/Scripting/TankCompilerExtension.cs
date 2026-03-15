// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using System.Collections.Generic;
using CodeGamified.Engine;
using CodeGamified.Engine.Compiler;

namespace Tanks.Scripting
{
    /// <summary>
    /// Tank-specific opcodes mapped to CUSTOM_0..CUSTOM_N.
    /// Mirrors PongOpCode — these are the I/O operations available to tank scripts.
    /// </summary>
    public enum TankOpCode
    {
        // Queries (read game state into R0)
        GET_MY_X             = 0,   // CUSTOM_0
        GET_MY_Y             = 1,   // CUSTOM_1
        GET_MY_HEADING       = 2,   // CUSTOM_2
        GET_MY_TURRET        = 3,   // CUSTOM_3
        GET_MY_HP            = 4,   // CUSTOM_4
        GET_MY_AMMO          = 5,   // CUSTOM_5
        GET_NEAREST_ENEMY_X  = 6,   // CUSTOM_6
        GET_NEAREST_ENEMY_Y  = 7,   // CUSTOM_7
        GET_NEAREST_ENEMY_DIST = 8, // CUSTOM_8
        GET_ARENA_W          = 9,   // CUSTOM_9
        GET_ARENA_H          = 10,  // CUSTOM_10

        // Orders (write to game state from R0)
        SET_DRIVE            = 11,  // CUSTOM_11
        SET_TURN             = 12,  // CUSTOM_12
        SET_TURRET           = 13,  // CUSTOM_13
        FIRE                 = 14,  // CUSTOM_14

        // Fog of war
        SCAN                 = 15,  // CUSTOM_15  — refresh visibility
        IS_VISIBLE           = 16,  // CUSTOM_16  — is_visible(x,y) → 0/1

        // Obstacle awareness
        GET_NEAREST_OBSTACLE_X    = 17,  // CUSTOM_17
        GET_NEAREST_OBSTACLE_Y    = 18,  // CUSTOM_18
        GET_NEAREST_OBSTACLE_DIST = 19,  // CUSTOM_19

        // Inter-computer shared data bus
        SEND                      = 20,  // CUSTOM_20  — send(channel, value)
        RECV                      = 21,  // CUSTOM_21  — recv(channel) → R0
    }

    /// <summary>
    /// Compiler extension for Tanks — registers builtins like get_my_x(), fire().
    /// Mirrors PongCompilerExtension architecture.
    /// </summary>
    public class TankCompilerExtension : ICompilerExtension
    {
        public void RegisterBuiltins(CompilerContext ctx)
        {
            // No special types for Tanks (yet)
        }

        public bool TryCompileCall(string functionName, List<AstNodes.ExprNode> args,
                                   CompilerContext ctx, int sourceLine)
        {
            switch (functionName)
            {
                // ── Queries: result stored in R0 ──
                case "get_my_x":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_MY_X, 0, 0, 0, sourceLine, "get_my_x → R0");
                    return true;
                case "get_my_y":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_MY_Y, 0, 0, 0, sourceLine, "get_my_y → R0");
                    return true;
                case "get_my_heading":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_MY_HEADING, 0, 0, 0, sourceLine, "get_my_heading → R0");
                    return true;
                case "get_my_turret":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_MY_TURRET, 0, 0, 0, sourceLine, "get_my_turret → R0");
                    return true;
                case "get_my_hp":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_MY_HP, 0, 0, 0, sourceLine, "get_my_hp → R0");
                    return true;
                case "get_my_ammo":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_MY_AMMO, 0, 0, 0, sourceLine, "get_my_ammo → R0");
                    return true;
                case "get_enemy_x":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_NEAREST_ENEMY_X, 0, 0, 0, sourceLine, "get_enemy_x → R0");
                    return true;
                case "get_enemy_y":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_NEAREST_ENEMY_Y, 0, 0, 0, sourceLine, "get_enemy_y → R0");
                    return true;
                case "get_enemy_dist":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_NEAREST_ENEMY_DIST, 0, 0, 0, sourceLine, "get_enemy_dist → R0");
                    return true;
                case "get_arena_w":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_ARENA_W, 0, 0, 0, sourceLine, "get_arena_w → R0");
                    return true;
                case "get_arena_h":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_ARENA_H, 0, 0, 0, sourceLine, "get_arena_h → R0");
                    return true;

                // ── Orders: arg from R0 ──
                case "set_drive":
                    if (args != null && args.Count > 0) args[0].Compile(ctx);
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.SET_DRIVE, 0, 0, 0, sourceLine, "set_drive(R0)");
                    return true;
                case "set_turn":
                    if (args != null && args.Count > 0) args[0].Compile(ctx);
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.SET_TURN, 0, 0, 0, sourceLine, "set_turn(R0)");
                    return true;
                case "set_turret":
                    if (args != null && args.Count > 0) args[0].Compile(ctx);
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.SET_TURRET, 0, 0, 0, sourceLine, "set_turret(R0)");
                    return true;
                case "fire":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.FIRE, 0, 0, 0, sourceLine, "fire()");
                    return true;

                // ── Fog of War ──
                case "scan":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.SCAN, 0, 0, 0, sourceLine, "scan()");
                    return true;
                case "is_visible":
                    if (args != null && args.Count > 0) args[0].Compile(ctx);
                    if (args != null && args.Count > 1)
                    {
                        ctx.Emit(OpCode.PUSH, 0, 0, 0, sourceLine, "push R0 (x)");
                        args[1].Compile(ctx);
                        ctx.Emit(OpCode.MOV, 1, 0, 0, sourceLine, "R1 ← R0 (y)");
                        ctx.Emit(OpCode.POP, 0, 0, 0, sourceLine, "pop R0 (x)");
                    }
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.IS_VISIBLE, 0, 0, 0, sourceLine, "is_visible(R0,R1) → R0");
                    return true;

                // ── Obstacle awareness ──
                case "get_obstacle_x":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_NEAREST_OBSTACLE_X, 0, 0, 0, sourceLine, "get_obstacle_x → R0");
                    return true;
                case "get_obstacle_y":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_NEAREST_OBSTACLE_Y, 0, 0, 0, sourceLine, "get_obstacle_y → R0");
                    return true;
                case "get_obstacle_dist":
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.GET_NEAREST_OBSTACLE_DIST, 0, 0, 0, sourceLine, "get_obstacle_dist → R0");
                    return true;

                // ── Inter-computer data bus ──
                case "send":
                    if (args != null && args.Count > 0) args[0].Compile(ctx);
                    if (args != null && args.Count > 1)
                    {
                        ctx.Emit(OpCode.PUSH, 0, 0, 0, sourceLine, "push R0 (channel)");
                        args[1].Compile(ctx);
                        ctx.Emit(OpCode.MOV, 1, 0, 0, sourceLine, "R1 ← R0 (value)");
                        ctx.Emit(OpCode.POP, 0, 0, 0, sourceLine, "pop R0 (channel)");
                    }
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.SEND, 0, 0, 0, sourceLine, "send(R0,R1)");
                    return true;
                case "recv":
                    if (args != null && args.Count > 0) args[0].Compile(ctx);
                    ctx.Emit(OpCode.CUSTOM_0 + (int)TankOpCode.RECV, 0, 0, 0, sourceLine, "recv(R0) → R0");
                    return true;

                default:
                    return false;
            }
        }

        public bool TryCompileMethodCall(string objectName, string methodName,
                                         List<AstNodes.ExprNode> args,
                                         CompilerContext ctx, int sourceLine)
        {
            return false;
        }

        public bool TryCompileObjectDecl(string typeName, string varName,
                                         List<AstNodes.ExprNode> constructorArgs,
                                         CompilerContext ctx, int sourceLine)
        {
            return false;
        }
    }
}
