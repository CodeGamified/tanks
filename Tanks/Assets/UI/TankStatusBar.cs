// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CodeGamified.TUI;
using CodeGamified.Time;
using CodeGamified.Settings;
using CodeGamified.Quality;
using Tanks.Game;
using Tanks.AI;
using Tanks.Scripting;

namespace Tanks.UI
{
    /// <summary>
    /// Tanks status bar — 3-column TerminalWindow.
    ///
    /// Collapsed (2 rows): single triple-column status line.
    /// Expanded (drag up): full 3-column layout:
    ///   LEFT:   Script controls (YOU top, AI bottom)
    ///   CENTER: "TANKS" ASCII art + battle stats
    ///   RIGHT:  Settings / controls / keybinds
    ///
    /// Mirrors PongStatusBar architecture.
    /// </summary>
    public class TankStatusBar : TerminalWindow
    {
        private TankMatchManager _match;
        private TankAIController _ai;
        private TankProgram _playerProgram;
        private TankBody _playerTank;
        private TankBody _aiTank;

        // Track sample loaded
        private AIDifficulty? _playerScriptTier;

        // Expand detection
        private bool IsExpanded => totalRows > 3;

        protected override void Awake()
        {
            base.Awake();
            windowTitle = "TANKS";
            totalRows = 40;
        }

        protected override void OnLayoutReady()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null || rows.Count == 0) return;
            float h = rt.rect.height;
            float rowH = rows[0].RowHeight;
            if (rowH <= 0) return;
            int fitRows = Mathf.Max(2, Mathf.FloorToInt(h / rowH));
            if (fitRows != totalRows)
            {
                for (int i = 0; i < rows.Count; i++)
                    rows[i].gameObject.SetActive(i < fitRows);
                totalRows = fitRows;
            }
        }

        public void Bind(TankMatchManager match, TankAIController ai,
                         TankProgram playerProgram, TankBody playerTank, TankBody aiTank)
        {
            _match = match;
            _ai = ai;
            _playerProgram = playerProgram;
            _playerTank = playerTank;
            _aiTank = aiTank;
        }

        protected override void Update()
        {
            base.Update();
            if (!rowsReady) return;
            if (!IsExpanded) return;
            HandleMenuInput();
        }

        private void HandleMenuInput()
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                if (shift) LoadPlayerSample(AIDifficulty.Easy);
                else SetAIDifficulty(AIDifficulty.Easy);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                if (shift) LoadPlayerSample(AIDifficulty.Medium);
                else SetAIDifficulty(AIDifficulty.Medium);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                if (shift) LoadPlayerSample(AIDifficulty.Hard);
                else SetAIDifficulty(AIDifficulty.Hard);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            {
                if (shift) LoadPlayerSample(AIDifficulty.Expert);
                else SetAIDifficulty(AIDifficulty.Expert);
            }

            if (Input.GetKeyDown(KeyCode.R) && _playerProgram != null)
            {
                _playerProgram.UploadCode(null);
                _playerScriptTier = null;
                Debug.Log("[Menu] Player script reset to starter code");
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                int next = (SettingsBridge.QualityLevel + 1) % 4;
                SettingsBridge.SetQualityLevel(next);
            }

            if (Input.GetKeyDown(KeyCode.RightBracket))
                SettingsBridge.SetFontSize(SettingsBridge.FontSize + 1f);
            if (Input.GetKeyDown(KeyCode.LeftBracket))
                SettingsBridge.SetFontSize(SettingsBridge.FontSize - 1f);
        }

        private void LoadPlayerSample(AIDifficulty diff)
        {
            if (_playerProgram == null) return;
            string code = TankAIController.GetSampleCode(diff);
            _playerProgram.UploadCode(code);
            _playerScriptTier = diff;
            Debug.Log($"[Menu] Player script → {diff} sample");
        }

        private void SetAIDifficulty(AIDifficulty diff)
        {
            if (_ai == null) return;
            _ai.SetDifficulty(diff);
            Debug.Log($"[Menu] AI difficulty → {diff}");
        }

        // ═══════════════════════════════════════════════════════════════
        // RENDER
        // ═══════════════════════════════════════════════════════════════

        protected override void Render()
        {
            ClearAllRows();

            if (rows.Count > 0)
            {
                rows[0].SetTripleColumnMode(true, 10f);
                rows[0].SetTripleTexts(
                    BuildCollapsedLeft(),
                    BuildCollapsedCenter(),
                    BuildCollapsedRight());
            }

            if (!IsExpanded) return;
            SetRow(1, Separator());
            RenderExpandedLayout();
        }

        // ═══════════════════════════════════════════════════════════════
        // COLLAPSED
        // ═══════════════════════════════════════════════════════════════

        private string BuildCollapsedLeft()
        {
            if (_match == null) return " TANKS";

            string pHP = _playerTank != null ? $"HP:{_playerTank.hp}" : "";
            string aHP = _aiTank != null ? $"HP:{_aiTank.hp}" : "";

            string pComp = "";
            if (_playerProgram?.Computers != null && _playerProgram.Computers.Count > 1)
                pComp = TUIColors.Dimmed($"×{_playerProgram.Computers.Count}");

            string you = TUIColors.Fg(TUIColors.BrightGreen, $"YOU {pHP}");
            string aiLabel = _ai != null ? $"AI({_ai.Difficulty})" : "AI";
            string them = TUIColors.Fg(TUIColors.Red, $"{aiLabel} {aHP}");
            return $" {you}{pComp}  {TUIGlyphs.BoxV}  {them}";
        }

        private string BuildCollapsedCenter()
        {
            if (_match == null) return "";
            return $"M:{_match.MatchesPlayed} W:{_match.PlayerWins} L:{_match.AIWins} D:{_match.Draws}";
        }

        private string BuildCollapsedRight()
        {
            var sim = SimulationTime.Instance;
            if (sim == null) return "";
            string expand = IsExpanded ? "" : $" {TUIGlyphs.ArrowU} MENU";
            return $"{sim.GetFormattedTimeScale()} {TUIGlyphs.BoxV} +/- speed {TUIGlyphs.BoxV} SPACE pause{expand} ";
        }

        // ═══════════════════════════════════════════════════════════════
        // EXPANDED — 3-column layout
        // ═══════════════════════════════════════════════════════════════

        private void RenderExpandedLayout()
        {
            var left = BuildLeftColumn();
            var center = BuildCenterColumn();
            var right = BuildRightColumn();

            int maxLines = Mathf.Max(left.Length, Mathf.Max(center.Length, right.Length));

            for (int i = 0; i < maxLines; i++)
            {
                int r = i + 2;
                if (r >= totalRows) break;

                rows[r].SetTripleColumnMode(true, 6f);

                string l = i < left.Length   ? left[i]   : "";
                string c = i < center.Length ? center[i] : "";
                string rt = i < right.Length  ? right[i]  : "";

                rows[r].SetTripleTexts(l, c, rt);
            }
        }

        // ── LEFT COLUMN: Script controls ────────────────────────

        private string[] BuildLeftColumn()
        {
            var lines = new List<string>();

            lines.Add($" {TUIColors.Fg(TUIColors.BrightGreen, TUIGlyphs.DiamondFilled)} {TUIColors.Bold("YOUR TANK")}");

            if (_playerProgram != null)
            {
                string name = _playerProgram.ProgramName ?? "TankAI";
                int inst = _playerProgram.Program?.Instructions?.Length ?? 0;
                string status = _playerProgram.IsRunning
                    ? TUIColors.Fg(TUIColors.BrightGreen, "RUN")
                    : TUIColors.Dimmed("STOP");
                string tier = _playerScriptTier.HasValue
                    ? TUIColors.Fg(TUIColors.BrightYellow, $"({_playerScriptTier.Value})")
                    : TUIColors.Dimmed("(custom)");
                lines.Add($"  {name} {status} {tier}");
                lines.Add($"  {TUIColors.Dimmed($"{inst} instructions")}");

                // Multi-computer info
                if (_playerProgram.Computers != null && _playerProgram.Computers.Count > 1)
                {
                    int compCount = _playerProgram.Computers.Count;
                    lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, $"{compCount} computers")} {TUIColors.Dimmed("[TAB] cycle")}");
                }
            }
            else
            {
                lines.Add(TUIColors.Dimmed("  No program"));
            }

            // Tank status
            if (_playerTank != null)
            {
                lines.Add($"  HP: {TUIColors.Fg(TUIColors.BrightGreen, $"{_playerTank.hp}/{_playerTank.maxHP}")}  " +
                          $"Ammo: {TUIColors.Fg(TUIColors.BrightCyan, $"{_playerTank.ammo}/{_playerTank.maxAmmo}")}");
            }

            // Sample loader
            var diffs = new[] { AIDifficulty.Easy, AIDifficulty.Medium, AIDifficulty.Hard, AIDifficulty.Expert };
            lines.Add($"  {TUIColors.Dimmed("Load sample:")}");
            for (int i = 0; i < diffs.Length; i++)
            {
                bool active = _playerScriptTier.HasValue && _playerScriptTier.Value == diffs[i];
                string key = TUIColors.Fg(TUIColors.BrightCyan, $"[S+{i + 1}]");
                string label = active
                    ? TUIColors.Fg(TUIColors.BrightGreen, $"{diffs[i]}{TUIGlyphs.ArrowL}")
                    : TUIColors.Dimmed($"{diffs[i]}");
                lines.Add($"  {key} {label}");
            }

            lines.Add("");

            // AI section
            lines.Add($" {TUIColors.Fg(TUIColors.Red, TUIGlyphs.DiamondFilled)} {TUIColors.Bold("AI OPPONENT")}");
            string aiDiff = _ai != null ? _ai.Difficulty.ToString() : "?";
            lines.Add($"  Difficulty: {TUIColors.Fg(TUIColors.BrightYellow, aiDiff)}");

            if (_aiTank != null)
            {
                lines.Add($"  HP: {TUIColors.Fg(TUIColors.Red, $"{_aiTank.hp}/{_aiTank.maxHP}")}  " +
                          $"Ammo: {TUIColors.Fg(TUIColors.BrightCyan, $"{_aiTank.ammo}/{_aiTank.maxAmmo}")}");
            }

            lines.Add($"  {TUIColors.Dimmed("Set AI:")}");
            var names = new[] { "Spinner", "Tracker", "Flanker", "Tactician" };
            for (int i = 0; i < diffs.Length; i++)
            {
                bool active = _ai != null && _ai.Difficulty == diffs[i];
                string key = TUIColors.Fg(TUIColors.BrightCyan, $"[{i + 1}]");
                string label = active
                    ? TUIColors.Fg(TUIColors.BrightGreen, $"{diffs[i]}{TUIGlyphs.ArrowL}")
                    : TUIColors.Dimmed($"{diffs[i]}");
                string desc = TUIColors.Dimmed(names[i]);
                lines.Add($"  {key} {label} {desc}");
            }

            return lines.ToArray();
        }

        // ── CENTER COLUMN: Title + battle stats ─────────────────

        private string[] BuildCenterColumn()
        {
            var lines = new List<string>();

            // ASCII art title
            lines.AddRange(BuildAsciiTitle());
            lines.Add("");

            // Battle status
            if (_match != null)
            {
                string pStat = _playerTank != null && _playerTank.IsAlive
                    ? TUIColors.Fg(TUIColors.BrightGreen, "ALIVE")
                    : TUIColors.Fg(TUIColors.Red, "DEAD");
                string aStat = _aiTank != null && _aiTank.IsAlive
                    ? TUIColors.Fg(TUIColors.Red, "ALIVE")
                    : TUIColors.Dimmed("DEAD");

                lines.Add($"  YOU: {pStat}    AI: {aStat}");
                lines.Add("");
                lines.Add($"  Matches: {_match.MatchesPlayed}");
                lines.Add($"  {TUIColors.Fg(TUIColors.BrightGreen, $"W:{_match.PlayerWins}")}  " +
                          $"{TUIColors.Fg(TUIColors.Red, $"L:{_match.AIWins}")}  " +
                          $"{TUIColors.Dimmed($"D:{_match.Draws}")}");
            }

            return lines.ToArray();
        }

        // ── ASCII art ───────────────────────────────────────────

        private static readonly string[] TitleTop =
        {
            "████████  ████████  ██      ██ ██    ██ █████████",
            "   ██     ██    ██  ████    ██ ██  ██   ██       ",
            "   ██     ████████  ██  ██  ██ ████     █████████",
            "   ██     ██    ██  ██    ████ ██  ██          ██",
            "   ██     ██    ██  ██      ██ ██    ██ █████████",
        };

        private string[] BuildAsciiTitle()
        {
            var lines = new string[TitleTop.Length + 2];
            int w = TitleTop[0].Length;

            lines[0] = TUIColors.Fg(TUIColors.BrightGreen, $"  ╔{new string('═', w)}╗");
            for (int i = 0; i < TitleTop.Length; i++)
            {
                float t = (float)i / (TitleTop.Length - 1);
                Color32 c = Color32.Lerp(TUIColors.BrightGreen, TUIColors.Red, t);
                lines[i + 1] = $"  {TUIColors.Fg(TUIColors.BrightGreen, "║")}" +
                               $"{TUIColors.Fg(c, TitleTop[i])}" +
                               $"{TUIColors.Fg(TUIColors.Red, "║")}";
            }
            lines[TitleTop.Length + 1] = TUIColors.Fg(TUIColors.Red, $"  ╚{new string('═', w)}╝");

            return lines;
        }

        // ── RIGHT COLUMN: Settings / Controls ───────────────────

        private string[] BuildRightColumn()
        {
            var lines = new List<string>();
            var sim = SimulationTime.Instance;
            string speed = sim != null ? sim.GetFormattedTimeScale() : "1x";
            string paused = (sim != null && sim.isPaused)
                ? TUIColors.Fg(TUIColors.BrightYellow, " PAUSED")
                : "";

            lines.Add($" {TUIColors.Bold("CONTROLS")}{paused}");
            lines.Add($"  Speed: {TUIColors.Fg(TUIColors.BrightGreen, speed)}");
            lines.Add("");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[1-4]")}     AI difficulty");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[S+1-4]")}   Load sample");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[R]")}       Reset code");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[+/-]")}     Time scale");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[SPACE]")}   Pause");
            lines.Add("");

            string tierName = ((QualityTier)SettingsBridge.QualityLevel).ToString();
            lines.Add($" {TUIColors.Bold("QUALITY")}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightGreen, tierName)}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[Q]")}       Cycle quality");
            lines.Add("");

            lines.Add($" {TUIColors.Bold("DISPLAY")}");
            lines.Add($"  Font: {TUIColors.Fg(TUIColors.BrightGreen, $"{SettingsBridge.FontSize:F0}pt")}");
            lines.Add($"  {TUIColors.Fg(TUIColors.BrightCyan, "[]]")}  Font+   {TUIColors.Fg(TUIColors.BrightCyan, "[[]")}  Font-");

            return lines.ToArray();
        }
    }
}
