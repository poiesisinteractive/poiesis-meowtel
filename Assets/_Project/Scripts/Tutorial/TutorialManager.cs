using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using CatHotel.Cats;
using CatHotel.Hotel;
using CatHotel.Input;
using CatHotel.Services;

namespace CatHotel.Tutorial
{
    /// <summary>
    /// Orchestrates the tutorial sequence. Drives NarrationUI, listens for game-event
    /// triggers, disables irrelevant UI, and persists the current step index.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TutorialSequenceData _sequence;
        [SerializeField] private NarrationUI _narrationUI;
        [SerializeField] private HotelManager _hotel;
        [SerializeField] private CameraFocus _cameraFocus;
        [SerializeField] private CatHotel.Grid.GridRenderer _gridRenderer;

        private int _currentStep;
        private bool _running;
        private bool _waitingForAction;
        private CatEntity _lastSpawnedCat;
        private float _activeSeconds; // time spent in the tutorial this session (analytics)

        // UI elements disabled during the tutorial (restored when done)
        private readonly List<GameObject> _disabledUI = new();

        public static TutorialManager Instance { get; private set; }

        /// <summary>True while the tutorial hasn't finished all steps.</summary>
        public bool IsActive => _running;

        /// <summary>Current step index (for save persistence).</summary>
        public int CurrentStepIndex => _currentStep;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (_running) _activeSeconds += Time.deltaTime;
        }

        /// <summary>
        /// Called by HotelManager after the scene is ready (grid built, breeds loaded).
        /// Resumes from the saved step index or starts at 0.
        /// </summary>
        public void Begin(int resumeFromStep = 0)
        {
            // Auto-find missing refs
            if (_sequence == null)
            {
                _sequence = Resources.Load<TutorialSequenceData>("TutorialFirstTime");
                if (_sequence == null)
                {
                    var all = Resources.FindObjectsOfTypeAll<TutorialSequenceData>();
                    if (all.Length > 0) _sequence = all[0];
                }
            }
            if (_narrationUI == null)
                _narrationUI = FindAnyObjectByType<NarrationUI>(FindObjectsInactive.Include);
            if (_hotel == null)
                _hotel = FindAnyObjectByType<HotelManager>();
            if (_cameraFocus == null)
                _cameraFocus = FindAnyObjectByType<CameraFocus>();
            if (_gridRenderer == null)
                _gridRenderer = FindAnyObjectByType<CatHotel.Grid.GridRenderer>();

            if (_sequence == null || _sequence.StepCount == 0)
            {
                Debug.LogWarning("[Tutorial] No sequence found. Run Cat Hotel > Build Tutorial Assets, then Setup Proto Scene.");
                return;
            }
            if (_narrationUI == null)
            {
                Debug.LogWarning("[Tutorial] NarrationUI not found in scene. Run Cat Hotel > Build Tutorial Assets, then Setup Proto Scene.");
                return;
            }

            _currentStep = Mathf.Clamp(resumeFromStep, 0, _sequence.StepCount);
            if (_currentStep >= _sequence.StepCount) return; // already completed

            _activeSeconds = 0f;
            _running = true;
            Debug.Log($"[Tutorial] Starting from step {_currentStep}/{_sequence.StepCount}");
            DisableGameUI();
            SubscribeEvents();

            // Listen for the Skip-tutorial button click on the NarrationUI
            _narrationUI.OnSkipConfirmed -= SkipTutorial;
            _narrationUI.OnSkipConfirmed += SkipTutorial;

            StartCoroutine(RunStep());
        }

        /// <summary>Signal the tutorial that a game event happened (cat selected, object placed, etc.).</summary>
        public void NotifyEvent(TutorialTrigger trigger, object context = null)
        {
            if (!_running || !_waitingForAction) return;
            if (_currentStep >= _sequence.StepCount) return;

            var step = _sequence.steps[_currentStep];
            if (step.trigger != trigger) return;

            // For parameterized triggers, validate context
            if (trigger == TutorialTrigger.WaitForObjectPlaced && context is Core.ObjectCategory cat)
            {
                if (cat != step.requiredCategory) return;
            }

            _waitingForAction = false;
        }

        private IEnumerator RunStep()
        {
            while (_currentStep < _sequence.StepCount)
            {
                var step = _sequence.steps[_currentStep];
                GameAnalytics.TutorialStep(_currentStep, step.AnalyticsId);

                // Execute start action (may spawn cat, lock camera, etc.)
                ExecuteAction(step.actionOnStart, step);

                // --- Pure Action step (no dialogue, no wait) ---
                if (!step.HasDialogue && step.trigger == TutorialTrigger.Action)
                {
                    ExecuteAction(step.actionOnComplete, step);
                    AdvanceStep();
                    yield return null;
                    continue;
                }

                // --- Silent delay step ---
                if (!step.HasDialogue && step.trigger == TutorialTrigger.WaitForDelay)
                {
                    yield return new WaitForSeconds(step.delaySeconds);
                    ExecuteAction(step.actionOnComplete, step);
                    AdvanceStep();
                    continue;
                }

                // --- Dialogue step ---
                if (step.HasDialogue)
                {
                    // Show "Skip tutorial" link only on the very first step
                    bool showSkip = _currentStep == 0;
                    _narrationUI.Show(step.speakerLabelKey, step.speakerPortrait, step.textKey,
                        showSkip, step.bubbleOnRight);
                }

                // --- Wait for trigger ---
                if (step.trigger == TutorialTrigger.WaitForTap)
                {
                    bool tapped = false;
                    void OnTap() => tapped = true;
                    _narrationUI.OnTapAdvance += OnTap;
                    yield return new WaitUntil(() => tapped);
                    _narrationUI.OnTapAdvance -= OnTap;
                }
                else if (step.trigger == TutorialTrigger.WaitForDelay)
                {
                    // Dialogue + delay: show text, wait delay, auto-advance
                    yield return new WaitForSeconds(step.delaySeconds);
                }
                else if (step.trigger != TutorialTrigger.Action)
                {
                    // Game-event trigger (selection, placement, service used, etc.)
                    _waitingForAction = true;
                    EnableUIForStep(step);
                    yield return new WaitUntil(() => !_waitingForAction);
                    DisableGameUI();
                }

                if (step.HasDialogue)
                    _narrationUI.Hide();

                // Execute complete action
                ExecuteAction(step.actionOnComplete, step);

                AdvanceStep();

                // Small pause between steps for visual breathing
                yield return new WaitForSecondsRealtime(0.3f);
            }

            // Tutorial complete
            _running = false;
            GameAnalytics.TutorialComplete(Mathf.RoundToInt(_activeSeconds));
            RestoreGameUI();
            ClearShopFilter();
            UnfreezeAllCats();
            RestoreDimmedHud();
            StopPulse();
            UnsubscribeEvents();
            if (_narrationUI != null) _narrationUI.OnSkipConfirmed -= SkipTutorial;
            MarkComplete();
            Debug.Log("[Tutorial] Complete!");
        }

        private void MarkComplete()
        {
            if (CloudSaveManager.Instance == null) return;
            CloudSaveManager.Instance.Progression.tutorialComplete = true;
            // Immediate sync write — async save would race against the player going back to menu.
            CloudSaveManager.Instance.SaveProgressionImmediate();
        }

        private void AdvanceStep()
        {
            _currentStep++;
            SaveStepIndex();
        }

        /// <summary>
        /// Skip the entire tutorial. Called from the skip confirmation UI.
        /// Marks the tutorial as complete, restores game UI, clears filters,
        /// unfreezes cats, and destroys tutorial-specific cats if still present.
        /// </summary>
        public void SkipTutorial()
        {
            if (!_running) return;

            Debug.Log("[Tutorial] Skipped by player");
            GameAnalytics.TutorialSkip(_currentStep); // before _currentStep is overwritten

            // Mark as complete
            _currentStep = _sequence != null ? _sequence.StepCount : 0;
            _running = false;
            _waitingForAction = false;

            // Stop any running coroutine (RunStep)
            StopAllCoroutines();

            // Hide narration + any confirmation popup, and unsubscribe
            if (_narrationUI != null)
            {
                _narrationUI.OnSkipConfirmed -= SkipTutorial;
                _narrationUI.Hide();
            }

            // Release camera focus if any
            if (_cameraFocus != null) _cameraFocus.Release();

            // Restore game state
            RestoreGameUI();
            ClearShopFilter();
            UnfreezeAllCats();
            UnsubscribeEvents();

            // Clean up tutorial-spawned cats that may still be frozen/idle
            if (_firstCat != null && _hotel != null)
            {
                _hotel.RemoveCat(_firstCat);
                _firstCat = null;
            }
            if (_refugeCat != null && _hotel != null)
            {
                _hotel.RemoveCat(_refugeCat);
                _refugeCat = null;
            }

            // If the hotel is now empty, spawn a cat right away so the player isn't left waiting
            if (_hotel != null && _hotel.Cats.Count == 0)
                _hotel.SpawnCatNow();

            // Persist state so tutorial won't restart next launch
            SaveStepIndex();
            MarkComplete();
        }

        private void SaveStepIndex()
        {
            if (CloudSaveManager.Instance == null) return;
            CloudSaveManager.Instance.Progression.tutorialStepIndex = _currentStep;
            CloudSaveManager.Instance.SaveProgression();
        }

        // ---- Actions ----

        private CatEntity _firstCat;  // pension cat
        private CatEntity _refugeCat; // refuge cat

        private void ExecuteAction(TutorialAction action, TutorialStep step = null)
        {
            switch (action)
            {
                case TutorialAction.SpawnFirstCat:
                    _lastSpawnedCat = _hotel != null ? _hotel.SpawnTutorialCat() : null;
                    _firstCat = _lastSpawnedCat;
                    if (_firstCat != null)
                    {
                        var firstNeeds = _firstCat.GetComponent<CatHotel.Cats.CatNeeds>();
                        if (firstNeeds != null) firstNeeds.FreezeDecay = true;
                    }
                    break;

                case TutorialAction.SpawnRefugeCatHungry:
                    _refugeCat = _hotel != null ? _hotel.SpawnTutorialRefugeCat() : null;
                    _lastSpawnedCat = _refugeCat;
                    if (_refugeCat != null)
                    {
                        var needs = _refugeCat.GetComponent<CatHotel.Cats.CatNeeds>();
                        if (needs != null)
                        {
                            needs.FreezeDecay = true; // prevent natural decay during tutorial
                            needs.FromArray(new float[] { 10f, 90f, 90f, 90f, 90f });
                        }
                    }
                    break;

                case TutorialAction.SetRefugeCatThirsty:
                    SetRefugeCatNeeds(90f, 10f, 90f, 90f, 90f);
                    break;
                case TutorialAction.SetRefugeCatSleepy:
                    SetRefugeCatNeeds(90f, 90f, 10f, 90f, 90f);
                    break;
                case TutorialAction.SetRefugeCatDirty:
                    SetRefugeCatNeeds(90f, 90f, 90f, 90f, 10f);
                    break;
                case TutorialAction.SetRefugeCatBored:
                    SetRefugeCatNeeds(90f, 90f, 90f, 10f, 90f);
                    break;

                case TutorialAction.FocusCameraOnLastSpawnedCat:
                    if (_cameraFocus != null && _lastSpawnedCat != null)
                        _cameraFocus.Focus(_lastSpawnedCat.transform);
                    break;
                case TutorialAction.FocusCameraOnPensionEntrance:
                    FocusOnGridPosition(_gridRenderer?.PensionEntrance ?? default);
                    break;
                case TutorialAction.FocusCameraOnRefugeEntrance:
                    FocusOnGridPosition(_gridRenderer?.RefugeEntrance ?? default);
                    break;
                case TutorialAction.FocusCameraOnUnhappyExit:
                    FocusOnGridPosition(_gridRenderer?.UnhappyExit ?? default);
                    break;
                case TutorialAction.ReleaseCameraFocus:
                    if (_cameraFocus != null) _cameraFocus.Release();
                    break;

                case TutorialAction.EnableShopFood:
                    ApplyShopFilter(CatHotel.UI.ShopCategory.Croquettes);
                    break;
                case TutorialAction.EnableShopWater:
                    AddShopFilter(CatHotel.UI.ShopCategory.Water);
                    break;
                case TutorialAction.EnableShopSleep:
                    AddShopFilter(CatHotel.UI.ShopCategory.Beds, CatHotel.UI.ShopCategory.Pillows);
                    break;
                case TutorialAction.EnableShopClean:
                    AddShopFilter(CatHotel.UI.ShopCategory.Litters);
                    break;
                case TutorialAction.EnableShopBalls:
                    AddShopFilter(CatHotel.UI.ShopCategory.Balls);
                    break;
                case TutorialAction.EnableFullShop:
                    ClearShopFilter();
                    break;

                case TutorialAction.HighlightGlobalPex:
                    DimHudExcept("GlobalPex");
                    StartPulseOn("GlobalPex");
                    break;

                case TutorialAction.HighlightUI:
                {
                    string target = step != null ? step.highlightTarget : null;
                    if (!string.IsNullOrEmpty(target))
                    {
                        // If the target was disabled by DisableGameUI, re-enable it temporarily
                        // so the player can actually see it during the highlight.
                        var targetGo = FindByName(target);
                        if (targetGo != null && !targetGo.activeSelf)
                        {
                            _temporarilyEnabledForHighlight.Add(targetGo);
                            targetGo.SetActive(true);
                        }
                        DimHudExcept(target);
                        StartPulseOn(target);
                    }
                    break;
                }

                case TutorialAction.RestoreHighlight:
                    RestoreDimmedHud();
                    StopPulse();
                    // Re-disable elements we temporarily activated only for the highlight.
                    foreach (var go in _temporarilyEnabledForHighlight)
                        if (go != null) go.SetActive(false);
                    _temporarilyEnabledForHighlight.Clear();
                    break;

                case TutorialAction.DespawnFirstCat:
                    if (_firstCat != null && _hotel != null)
                    {
                        _hotel.RemoveCat(_firstCat);
                        _firstCat = null;
                    }
                    break;
            }
        }

        private void SetRefugeCatNeeds(float hunger, float thirst, float sleep, float play, float clean)
        {
            if (_refugeCat == null) return;
            var needs = _refugeCat.GetComponent<CatHotel.Cats.CatNeeds>();
            needs?.FromArray(new[] { hunger, thirst, sleep, play, clean });
        }

        private void FocusOnGridPosition(Vector2Int gridPos)
        {
            if (_cameraFocus == null) return;
            _cameraFocus.FocusOnPosition(new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0f));
        }

        // ---- Shop filter ----

        private CatHotel.UI.ShopPanel _shopPanel;
        private readonly HashSet<CatHotel.UI.ShopCategory> _shopFilter = new();

        private CatHotel.UI.ShopPanel GetShop()
        {
            if (_shopPanel == null)
                _shopPanel = FindAnyObjectByType<CatHotel.UI.ShopPanel>();
            return _shopPanel;
        }

        private void ApplyShopFilter(params CatHotel.UI.ShopCategory[] cats)
        {
            _shopFilter.Clear();
            foreach (var c in cats) _shopFilter.Add(c);
            GetShop()?.SetTutorialFilter(_shopFilter);
        }

        private void AddShopFilter(params CatHotel.UI.ShopCategory[] cats)
        {
            foreach (var c in cats) _shopFilter.Add(c);
            GetShop()?.SetTutorialFilter(_shopFilter);
        }

        private void ClearShopFilter()
        {
            _shopFilter.Clear();
            GetShop()?.ClearTutorialFilter();
        }

        // ---- HUD dim + GlobalPex pulse ----

        private readonly System.Collections.Generic.Dictionary<UnityEngine.CanvasGroup, float> _dimmedGroups
            = new System.Collections.Generic.Dictionary<UnityEngine.CanvasGroup, float>();
        private readonly System.Collections.Generic.List<GameObject> _temporarilyEnabledForHighlight = new();
        private Tween _globalPexPulse;
        private Vector3 _globalPexBaseScale;
        private Transform _globalPexTransform;

        /// <summary>
        /// Dim every HUD-level GameObject so only the named one stands out.
        /// Adds CanvasGroups when missing and remembers original alpha for restore.
        /// </summary>
        private void DimHudExcept(string keepName)
        {
            // Dim a known set of HUD elements + the "HUD" / "HUDPanel" parent if found.
            // Only top-level HUD panels — never the inner labels, otherwise CanvasGroup
            // alphas multiply and the visible result becomes near-invisible (0.3 × 0.3 = 0.09).
            string[] candidates =
            {
                "GlobalPex",
                "Catcoins", "Puurls",
                "Capacity", "Comfort",
                "Floors",
                "System",
                "CollectAllAction", "AddBoost",
                "NextCat",
                "ShopAction", "ParameterAction",
                "FloorUpAction", "FloorDownAction",
            };

            // Resolve the kept element's transform once so we can skip self + descendants + ancestors.
            var keptGo = FindByName(keepName);
            var keptT = keptGo != null ? keptGo.transform : null;

            foreach (var name in candidates)
            {
                var go = FindByName(name);
                if (go == null) continue;
                if (go.name == keepName) continue;

                // Skip if candidate is an ancestor of kept (dimming it would dim kept's siblings, fine,
                // but it could also affect kept itself). Skip if candidate is a descendant of kept.
                if (keptT != null)
                {
                    var t = go.transform;
                    if (t.IsChildOf(keptT)) continue;
                    if (keptT.IsChildOf(t)) continue;
                }

                var cg = go.GetComponent<UnityEngine.CanvasGroup>();
                if (cg == null) cg = go.AddComponent<UnityEngine.CanvasGroup>();
                if (!_dimmedGroups.ContainsKey(cg))
                {
                    _dimmedGroups[cg] = cg.alpha;
                    cg.alpha = 0.3f;
                    cg.blocksRaycasts = false;
                    cg.interactable = false;
                }
            }

            // Make sure the kept element fully shows + raycasts even if its parent is dimmed
            var kept = FindByName(keepName);
            if (kept != null)
            {
                var cg = kept.GetComponent<UnityEngine.CanvasGroup>();
                if (cg == null) cg = kept.AddComponent<UnityEngine.CanvasGroup>();
                cg.ignoreParentGroups = true;
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;
            }
        }

        private void RestoreDimmedHud()
        {
            foreach (var kvp in _dimmedGroups)
            {
                if (kvp.Key == null) continue;
                kvp.Key.alpha = kvp.Value;
                kvp.Key.blocksRaycasts = true;
                kvp.Key.interactable = true;
            }
            _dimmedGroups.Clear();
        }

        private void StartPulseOn(string targetName)
        {
            var go = FindByName(targetName);
            if (go == null) return;
            _globalPexTransform = go.transform;
            _globalPexBaseScale = _globalPexTransform.localScale;
            _globalPexPulse?.Kill();
            _globalPexPulse = _globalPexTransform
                .DOScale(_globalPexBaseScale * 1.06f, 0.55f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void StopPulse()
        {
            _globalPexPulse?.Kill();
            _globalPexPulse = null;
            if (_globalPexTransform != null && _globalPexBaseScale != Vector3.zero)
                _globalPexTransform.localScale = _globalPexBaseScale;
        }

        private void UnfreezeAllCats()
        {
            if (_hotel == null) return;
            foreach (var cat in _hotel.Cats)
            {
                if (cat.Needs != null) cat.Needs.FreezeDecay = false;
            }
        }

        // ---- Events from game systems ----

        private void SubscribeEvents()
        {
            // We use a polling approach via NotifyEvent (called by game systems)
            // rather than wiring every event here. Game systems call
            // TutorialManager.Instance?.NotifyEvent(...) at the right moments.
        }

        private void UnsubscribeEvents() { }

        // ---- UI gating ----

        private static readonly string[] ManagedUINames =
        {
            "ShopPanel", "ParametersPanel", "FloorUpAction", "FloorDownAction",
            "ShopAction", "ParameterAction",
        };

        private void DisableGameUI()
        {
            foreach (var name in ManagedUINames)
            {
                var go = FindByName(name);
                if (go != null && go.activeSelf)
                {
                    go.SetActive(false);
                    if (!_disabledUI.Contains(go))
                        _disabledUI.Add(go);
                }
            }
        }

        private void RestoreGameUI()
        {
            foreach (var go in _disabledUI)
                if (go != null) go.SetActive(true);
            _disabledUI.Clear();
        }

        /// <summary>Selectively re-enable UI relevant to the current step.</summary>
        private void EnableUIForStep(TutorialStep step)
        {
            switch (step.trigger)
            {
                case TutorialTrigger.WaitForShopOpened:
                case TutorialTrigger.WaitForObjectPlaced:
                    EnableByName("ShopAction");
                    break;
                case TutorialTrigger.WaitForFloorChanged:
                    EnableByName("FloorUpAction");
                    EnableByName("FloorDownAction");
                    break;
                case TutorialTrigger.WaitForLevelPanelOpened:
                    // GlobalPex stays alive via the HighlightGlobalPex action;
                    // nothing else to enable here.
                    break;
                // Cat selection/petting/coin/service don't need UI buttons — direct in-world.
            }
        }

        private void EnableByName(string name)
        {
            var go = FindByName(name);
            if (go != null) go.SetActive(true);
        }

        // ---- Helpers ----

        private static GameObject FindByName(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) return go;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    var found = FindDeep(root.transform, name);
                    if (found != null) return found.gameObject;
                }
            }
            return null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindDeep(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
