// Copyright CodeGamified 2025-2026
// MIT License — Tanks: Code Your Tank
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CodeGamified.TUI;
using CodeGamified.Settings;
using Tanks.Game;
using Tanks.AI;
using Tanks.Scripting;

namespace Tanks.UI
{
    /// <summary>
    /// TUI Manager for Tanks — two draggable debugger windows + 3-column status bar.
    /// Powered by .engine's CodeDebuggerWindow + TUIEdgeDragger.
    ///
    /// Layout (all edges draggable):
    ///   ┌─────────────────────┬─────────────────────┐
    ///   │ YOUR TANK CODE      │ AI TANK CODE        │
    ///   │ PY │ MACHINE │ REGS │ PY │ MACHINE │ INFO │
    ///   │    │         │      │    │         │      │
    ///   ├──────────┬──────────┬──────────────────────┤
    ///   │ SCRIPTS  │ ██ TANKS │ CONTROLS            │
    ///   │ YOU / AI │ W:3 L:1  │ [1-4] AI difficulty  │
    ///   │ HP/Ammo  │ D:0      │ [SPACE] pause ...    │
    ///   └──────────┴──────────┴──────────────────────┘
    ///
    /// Mirrors PongTUIManager architecture exactly.
    /// </summary>
    public class TankTUIManager : MonoBehaviour, ISettingsListener
    {
        // Dependencies
        private TankMatchManager _match;
        private TankProgram _playerProgram;
        private TankAIController _ai;
        private TankBody _playerTank;
        private TankBody _aiTank;

        // Canvas
        private Canvas _canvas;
        private RectTransform _canvasRect;

        // Panels
        private TankCodeDebugger _playerDebugger;
        private TankCodeDebugger _aiDebugger;
        private TankStatusBar _statusBar;

        // Panel rects
        private RectTransform _leftPanelRect;
        private RectTransform _rightPanelRect;
        private RectTransform _statusBarRect;

        // Font
        private TMP_FontAsset _font;
        private float _fontSize;

        public void Initialize(TankMatchManager match, TankProgram playerProgram,
                               TankAIController ai, TankBody playerTank, TankBody aiTank)
        {
            _match = match;
            _playerProgram = playerProgram;
            _ai = ai;
            _playerTank = playerTank;
            _aiTank = aiTank;
            _fontSize = SettingsBridge.FontSize;

            BuildCanvas();
            BuildPanels();
        }

        private void OnEnable()  => SettingsBridge.Register(this);
        private void OnDisable() => SettingsBridge.Unregister(this);

        public void OnSettingsChanged(SettingsSnapshot settings, SettingsCategory changed)
        {
            if (changed != SettingsCategory.Display) return;
            if (Mathf.Approximately(settings.FontSize, _fontSize)) return;

            _fontSize = settings.FontSize;
            RebuildPanels();
        }

        private void RebuildPanels()
        {
            if (_leftPanelRect != null) Destroy(_leftPanelRect.gameObject);
            if (_rightPanelRect != null) Destroy(_rightPanelRect.gameObject);
            if (_statusBarRect != null) Destroy(_statusBarRect.gameObject);
            _playerDebugger = null;
            _aiDebugger = null;
            _statusBar = null;

            BuildPanels();
        }

        // ═══════════════════════════════════════════════════════════════
        // CANVAS
        // ═══════════════════════════════════════════════════════════════

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("TankTUI_Canvas");
            canvasGO.transform.SetParent(transform, false);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            _canvasRect = canvasGO.GetComponent<RectTransform>();

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PANELS
        // ═══════════════════════════════════════════════════════════════

        private void BuildPanels()
        {
            // ── Left panel: Player's code debugger ──
            _leftPanelRect = CreatePanel("PlayerPanel",
                new Vector2(0f, 0.25f),
                new Vector2(0.25f, 1f));

            _playerDebugger = _leftPanelRect.gameObject.AddComponent<TankCodeDebugger>();
            AddPanelBackground(_leftPanelRect);
            _playerDebugger.InitializeProgrammatic(GetFont(), _fontSize,
                _leftPanelRect.GetComponent<Image>());
            _playerDebugger.SetTitle("YOUR TANK");
            _playerDebugger.Bind(_playerProgram);

            TUIEdgeDragger.Create(_leftPanelRect, _canvasRect, TUIEdgeDragger.Edge.Right);
            var leftBottom = TUIEdgeDragger.Create(_leftPanelRect, _canvasRect, TUIEdgeDragger.Edge.Bottom);

            // ── Right panel: AI code debugger ──
            _rightPanelRect = CreatePanel("AIPanel",
                new Vector2(0.75f, 0.25f),
                new Vector2(1f, 1f));

            _aiDebugger = _rightPanelRect.gameObject.AddComponent<TankCodeDebugger>();
            AddPanelBackground(_rightPanelRect);
            _aiDebugger.InitializeProgrammatic(GetFont(), _fontSize,
                _rightPanelRect.GetComponent<Image>());
            _aiDebugger.SetTitle("AI TANK");
            _aiDebugger.Bind(_ai.Program);

            TUIEdgeDragger.Create(_rightPanelRect, _canvasRect, TUIEdgeDragger.Edge.Left);
            var rightBottom = TUIEdgeDragger.Create(_rightPanelRect, _canvasRect, TUIEdgeDragger.Edge.Bottom);

            // ── Status bar (bottom 25%) ──
            _statusBarRect = CreatePanel("StatusBar",
                new Vector2(0f, 0f),
                new Vector2(1f, 0.25f));

            _statusBar = _statusBarRect.gameObject.AddComponent<TankStatusBar>();
            AddPanelBackground(_statusBarRect);
            _statusBar.InitializeProgrammatic(GetFont(), _fontSize - 1f,
                _statusBarRect.GetComponent<Image>());
            _statusBar.Bind(_match, _ai, _playerProgram, _playerTank, _aiTank);

            var statusTop = TUIEdgeDragger.Create(_statusBarRect, _canvasRect, TUIEdgeDragger.Edge.Top);

            // Link edges
            statusTop.LinkEdge(_leftPanelRect, TUIEdgeDragger.Edge.Bottom)
                     .LinkEdge(_rightPanelRect, TUIEdgeDragger.Edge.Bottom);
            leftBottom.LinkEdge(_statusBarRect, TUIEdgeDragger.Edge.Top)
                      .LinkEdge(_rightPanelRect, TUIEdgeDragger.Edge.Bottom);
            rightBottom.LinkEdge(_statusBarRect, TUIEdgeDragger.Edge.Top)
                       .LinkEdge(_leftPanelRect, TUIEdgeDragger.Edge.Bottom);
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private RectTransform CreatePanel(string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_canvasRect, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return rt;
        }

        private void AddPanelBackground(RectTransform panel)
        {
            var img = panel.gameObject.GetComponent<Image>();
            if (img == null)
                img = panel.gameObject.AddComponent<Image>();
            img.color = new Color(0.01f, 0.03f, 0.06f, 0.92f);
            img.raycastTarget = true;
        }

        private TMP_FontAsset GetFont()
        {
            if (_font != null) return _font;
            _font = Resources.Load<TMP_FontAsset>("Fonts/Unifont SDF");
            if (_font == null)
                _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return _font;
        }
    }
}
