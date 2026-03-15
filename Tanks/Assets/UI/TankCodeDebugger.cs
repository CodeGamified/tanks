// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using System.Collections.Generic;
using UnityEngine;
using CodeGamified.Engine;
using CodeGamified.Engine.Runtime;
using CodeGamified.TUI;
using Tanks.Scripting;

namespace Tanks.UI
{
    /// <summary>
    /// Unified code debugger — works for ANY TankProgram (player OR AI).
    /// Three-panel live view: SOURCE CODE │ MACHINE CODE │ REGISTERS & STATE
    ///
    /// MULTI-COMPUTER AWARE:
    ///   When a TankProgram has multiple computers, the header shows tabs:
    ///     [main] [turret] [nav]
    ///   Tab key cycles the active computer. Each computer's source,
    ///   machine code, and registers are shown independently.
    ///   Shared bus channels are appended to the state column.
    ///
    /// Mirrors PongCodeDebugger architecture.
    /// </summary>
    public class TankCodeDebugger : CodeDebuggerWindow
    {
        private TankProgram _program;
        private int _activeComputerIndex;
        private string _baseTitle;

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "CODE";
        }

        public void SetTitle(string title)
        {
            _baseTitle = title;
            windowTitle = title;
        }

        public void Bind(TankProgram program)
        {
            _program = program;
            _activeComputerIndex = 0;
        }

        /// <summary>The currently viewed computer (cycles via Tab).</summary>
        private TankComputer ActiveComputer
        {
            get
            {
                if (_program == null || _program.Computers == null || _program.Computers.Count == 0)
                    return null;
                if (_activeComputerIndex >= _program.Computers.Count)
                    _activeComputerIndex = 0;
                return _program.Computers[_activeComputerIndex];
            }
        }

        private bool IsMultiComputer =>
            _program != null && _program.Computers != null && _program.Computers.Count > 1;

        protected override void Update()
        {
            base.Update();
            HandleTabCycling();
        }

        private void HandleTabCycling()
        {
            if (!IsMultiComputer) return;
            if (!Input.GetKeyDown(KeyCode.Tab)) return;

            // Shift+Tab = backward, Tab = forward
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            int count = _program.Computers.Count;
            if (shift)
                _activeComputerIndex = (_activeComputerIndex - 1 + count) % count;
            else
                _activeComputerIndex = (_activeComputerIndex + 1) % count;

            scrollOffset = 0;
        }

        protected override string[] GetSourceLines()
        {
            return ActiveComputer?.Program?.SourceLines;
        }

        protected override string GetProgramName()
        {
            if (!IsMultiComputer)
                return _baseTitle ?? _program?.ProgramName ?? "TankAI";

            // Build tab bar: [main] [turret] [nav]
            var sb = new System.Text.StringBuilder();
            sb.Append(_baseTitle ?? "TANK");
            sb.Append("  ");
            for (int i = 0; i < _program.Computers.Count; i++)
            {
                var comp = _program.Computers[i];
                bool active = (i == _activeComputerIndex);
                string name = comp.Name.ToUpper();
                if (active)
                    sb.Append(TUIColors.Fg(TUIColors.BrightGreen, $"[{name}]"));
                else
                    sb.Append(TUIColors.Dimmed($"[{name}]"));
                sb.Append(' ');
            }
            return sb.ToString();
        }

        protected override bool HasLiveProgram
        {
            get
            {
                var comp = ActiveComputer;
                return comp != null && comp.Executor != null && comp.Program != null
                    && comp.Program.Instructions != null && comp.Program.Instructions.Length > 0;
            }
        }

        protected override int GetPC() =>
            ActiveComputer?.State?.PC ?? 0;

        protected override long GetCycleCount() =>
            ActiveComputer?.State?.CycleCount ?? 0;

        protected override string GetStatusString()
        {
            var comp = ActiveComputer;
            if (comp == null || comp.Executor == null)
                return TUIColors.Dimmed("NO PROGRAM");

            var state = comp.State;
            if (state == null) return TUIColors.Dimmed("NO STATE");
            int instCount = comp.Program?.Instructions?.Length ?? 0;
            string opsLabel = $"{comp.OpsPerSecond:F0}ops/s";
            return TUIColors.Fg(TUIColors.BrightGreen, $"TICK {instCount} inst {opsLabel}");
        }

        protected override string GetIndexTag()
        {
            if (!IsMultiComputer) return "";
            return TUIColors.Dimmed($"[{_activeComputerIndex + 1}/{_program.Computers.Count}]");
        }

        protected override List<string> BuildSourceColumn(int pc)
        {
            var lines = new List<string>();
            var src = GetSourceLines();
            if (src == null) return lines;

            var comp = ActiveComputer;
            int activeLine = -1;
            if (HasLiveProgram && comp.Program.Instructions.Length > 0 && pc < comp.Program.Instructions.Length)
                activeLine = comp.Program.Instructions[pc].SourceLine - 1;

            for (int i = scrollOffset; i < src.Length && lines.Count < ContentRows; i++)
            {
                bool isActive = (i == activeLine);
                string prefix = isActive
                    ? TUIColors.Fg(TUIColors.BrightGreen, TUIGlyphs.ArrowR)
                    : " ";
                string num = TUIColors.Dimmed($"{i + 1,3}");
                string text = isActive
                    ? TUIColors.Fg(TUIColors.BrightGreen, src[i])
                    : src[i];
                lines.Add($"{prefix}{num} {text}");
            }
            return lines;
        }

        protected override List<string> BuildAsmColumn(int pc)
        {
            var lines = new List<string>();
            if (!HasLiveProgram) return lines;

            var comp = ActiveComputer;
            var instructions = comp.Program.Instructions;
            int start = Mathf.Max(0, pc - ContentRows / 3);

            for (int i = start; i < instructions.Length && lines.Count < ContentRows; i++)
            {
                var inst = instructions[i];
                bool isPC = (i == pc);
                string prefix = isPC
                    ? TUIColors.Fg(TUIColors.BrightCyan, TUIGlyphs.ArrowR)
                    : " ";
                string addr = TUIColors.Dimmed($"{i:D4}:");

                string opName = inst.Op.ToString();
#if UNITY_EDITOR || DEVELOPMENT_BUILD || CODEGAMIFIED_DEBUG
                string comment = inst.Comment ?? opName;
#else
                string comment = opName;
#endif
                string text = isPC
                    ? TUIColors.Fg(TUIColors.BrightCyan, comment)
                    : comment;
                lines.Add($"{prefix}{addr} {text}");
            }
            return lines;
        }

        protected override List<string> BuildStateColumn()
        {
            var lines = new List<string>();
            if (!HasLiveProgram) return lines;

            var comp = ActiveComputer;
            var state = comp.State;

            // Registers
            for (int r = 0; r < MachineState.REGISTER_COUNT; r++)
            {
                bool modified = (r == state.LastRegisterModified);
                string rName = $"R{r}:";
                string rVal = $"{state.Registers[r]:F2}";
                if (modified)
                    lines.Add(TUIColors.Fg(TUIColors.BrightGreen, $" {rName,-4} {rVal}"));
                else
                    lines.Add($" {TUIColors.Dimmed(rName),-4} {rVal}");
            }

            lines.Add(Separator(col3Start > 0 ? totalChars - col3Start - 2 : 16));

            // Flags
            lines.Add($" FLAGS: {state.Flags}");
            lines.Add($" PC: {state.PC}");
            lines.Add($" STACK [{state.Stack.Count}]");

            // Variables
            if (state.NameToAddress.Count > 0)
            {
                lines.Add(Separator(col3Start > 0 ? totalChars - col3Start - 2 : 16));
                lines.Add(TUIColors.Fg(TUIColors.BrightCyan, " VARIABLES"));
                foreach (var kvp in state.NameToAddress)
                {
                    string name = kvp.Key;
                    float val = 0;
                    if (state.Memory.ContainsKey(name))
                        val = state.Memory[name];
                    lines.Add($" {TUIColors.Dimmed(name + ":")} {val:F2}");
                }
            }

            // Shared bus — show when multi-computer
            if (IsMultiComputer && _program.SharedBus != null)
            {
                lines.Add(Separator(col3Start > 0 ? totalChars - col3Start - 2 : 16));
                lines.Add(TUIColors.Fg(TUIColors.BrightYellow, " DATA BUS"));
                for (int ch = 0; ch < _program.SharedBus.Length; ch++)
                {
                    float val = _program.SharedBus[ch];
                    if (val == 0f) continue; // only show non-zero channels
                    lines.Add($" {TUIColors.Dimmed($"ch{ch}:")} {val:F2}");
                }
            }

            // Tab hint
            if (IsMultiComputer)
            {
                lines.Add(Separator(col3Start > 0 ? totalChars - col3Start - 2 : 16));
                lines.Add(TUIColors.Dimmed(" [TAB] next computer"));
            }

            return lines;
        }
    }
}
