using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CatHotel.Cats;
using CatHotel.Core;
using CatHotel.Economy;
using CatHotel.Grid;
using CatHotel.Services;
using CatHotel.UI;

namespace CatHotel.Hotel
{
    /// <summary>
    /// Central game orchestrator. Manages cat arrivals (pension/refuge),
    /// service-based revenue, departures, and ties all systems together.
    /// </summary>
    public class HotelManager : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private GameConfig _config;
        [SerializeField] private CatPersonalityConfig _personalityConfig;

        [Header("Breed Registry (lazy-loaded from Resources/Breeds/)")]
        [SerializeField] private BreedRegistry.BreedEntry[] _breedEntries = new[]
        {
            new BreedRegistry.BreedEntry { assetName = "Breed_Europeen", minReputation = 0 },
            new BreedRegistry.BreedEntry { assetName = "Breed_Europeen2", minReputation = 0 },
            new BreedRegistry.BreedEntry { assetName = "Breed_Europeen3", minReputation = 0 },
            new BreedRegistry.BreedEntry { assetName = "Breed_SiberienBlack", minReputation = 0 },
            new BreedRegistry.BreedEntry { assetName = "Breed_SiberienWhite", minReputation = 0 },
            new BreedRegistry.BreedEntry { assetName = "Breed_Ragdoll", minReputation = 1 },
            new BreedRegistry.BreedEntry { assetName = "Breed_Ragdoll2", minReputation = 1 },
            new BreedRegistry.BreedEntry { assetName = "Breed_Siamois", minReputation = 2 },
            new BreedRegistry.BreedEntry { assetName = "Breed_Chartreux", minReputation = 8 },
        };

        private BreedRegistry _breedRegistry;

        [Header("References")]
        [SerializeField] private GridRenderer _gridRenderer;
        public GridRenderer GridRenderer => _gridRenderer;
        [SerializeField] private EconomyManager _economy;
        [SerializeField] private ReputationManager _reputation;
        [SerializeField] private CatSpawner _catSpawner;
        [SerializeField] private FloatingCoinView _floatingCoinView;

        [Header("Mood Bubbles")]
        [SerializeField] private Sprite _moodHappy;
        [SerializeField] private Sprite _moodEcstatic;
        [SerializeField] private Sprite _moodDepressed;
        [SerializeField] private Sprite _moodAggressive;
        [SerializeField] private Sprite _moodAngry;

        [Header("Need Bubbles")]
        [SerializeField] private Sprite _needHungry;
        [SerializeField] private Sprite _needThirsty;
        [SerializeField] private Sprite _needTired;
        [SerializeField] private Sprite _needBored;
        [SerializeField] private Sprite _needDirty;

        private readonly List<CatInstance> _cats = new();
        private float _arrivalTimer;

        // Tick-based update for departures + pension (no need to check every frame)
        private const float CatTickInterval = 0.5f;
        private float _catTickTimer;

        // Periodic auto-save (survives brutal app kill on Android)
        private const float AutoSaveInterval = 30f;
        private float _autoSaveTimer;

        // Passive income for cats on hidden floors (they don't tick services there).
        private const float PassiveIncomeInterval = 10f; // every 10s
        private const float PassiveIncomePerCat = 1.5f;  // base coins per hidden cat per tick (scaled by happiness)
        private float _passiveIncomeTimer;

        public IReadOnlyList<CatInstance> Cats => _cats;
        public GameConfig Config => _config;
        public EconomyManager Economy => _economy;
        public ReputationManager Reputation => _reputation;

        // Active play time in the hotel (saved), for analytics
        private int _playTimeSeconds;
        private float _playTimeFraction;
        public int PlayTimeSeconds => _playTimeSeconds;
        public int CatCount => _cats.Count;
        public float ArrivalTimer => _arrivalTimer;

        public event Action<CatInstance> OnCatArrived;
        public event Action<CatInstance> OnCatDeparted;

        private void OnDestroy()
        {
            LocalizedStrings.OnLanguageChanged -= RefreshDescriptions;
            if (_reputation != null) _reputation.OnLevelChanged -= OnReputationLevelUp;
            _breedRegistry?.UnloadAll();
        }

        private void RefreshDescriptions()
        {
            if (_personalityConfig == null) return;
            foreach (var cat in _cats)
            {
                var (desc, _) = _personalityConfig.GeneratePersonality(cat.Breed, cat.CatName.GetHashCode());
                cat.Description = desc;
            }
        }

        private IEnumerator Start()
        {
            // Initialize breed registry (lazy-loads breeds via Addressables)
            _breedRegistry = new BreedRegistry(_breedEntries);

            LocalizedStrings.OnLanguageChanged += RefreshDescriptions;
            // Auth/Ads init is now handled by BootManager.
            // If running Proto directly (editor), fallback to local init.
            if (AuthManager.Instance == null)
            {
                Debug.Log("[Hotel] No BootManager — running standalone init");
                // Init Addressables if Boot didn't run
                yield return UnityEngine.AddressableAssets.Addressables.InitializeAsync();
                // Create a temporary AuthManager for editor testing
                var authGo = new GameObject("[AuthManager]");
                var auth = authGo.AddComponent<AuthManager>();
                var authTask = auth.InitializeAsync();
                float timeout = 5f;
                while (!authTask.IsCompleted && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
            }

            // Create CloudSaveManager if Boot didn't run (standalone Proto scene)
            if (CloudSaveManager.Instance == null)
            {
                var csGo = new GameObject("[CloudSaveManager]");
                csGo.AddComponent<CloudSaveManager>();
                bool isSignedIn = AuthManager.Instance != null && AuthManager.Instance.IsSignedIn;
                var loadTask = CloudSaveManager.Instance.LoadAllAsync(isSignedIn);
                float csTimeout = 5f;
                while (!loadTask.IsCompleted && csTimeout > 0f)
                {
                    csTimeout -= Time.deltaTime;
                    yield return null;
                }
                if (!loadTask.IsCompleted)
                    CloudSaveManager.Instance.LoadFromLocal();
            }

            // The previous session ended while the tutorial was still in progress (B-2).
            // Never wipe the save: the tutorial's transient state (frozen cats, shop filter,
            // highlights) can't be resumed mid-way, so either
            // - the player already built something → the tutorial is marked complete;
            // - otherwise → the tutorial restarts from the beginning, the save is kept.
            if (CloudSaveManager.Instance != null && CloudSaveManager.Instance.IsLoaded
                && CloudSaveManager.Instance.HasPersistedSave
                && !CloudSaveManager.Instance.Progression.tutorialComplete)
            {
                var prog = CloudSaveManager.Instance.Progression;
                bool hotelStarted = (prog.placedObjects != null && prog.placedObjects.Count > 0)
                    || prog.reputationLevel > 0;
                if (hotelStarted)
                {
                    Debug.Log("[Hotel] Tutorial was incomplete on quit, hotel already started — marking it complete.");
                    prog.tutorialComplete = true;
                    CloudSaveManager.Instance.SaveProgressionImmediate();
                }
                else
                {
                    // Nothing built yet: the only cats are the previous run's tutorial cats, which the
                    // tutorial spawns again — drop them so they aren't duplicated.
                    Debug.Log("[Hotel] Tutorial was incomplete on quit — restarting it (save kept).");
                    prog.tutorialStepIndex = 0;
                    prog.cats?.Clear();
                }
            }

            // Init economy + reputation from save if available, otherwise defaults
            var savedProg = CloudSaveManager.Instance != null && CloudSaveManager.Instance.IsLoaded
                && CloudSaveManager.Instance.HasPersistedSave
                ? CloudSaveManager.Instance.Progression : null;

            if (_economy != null && _config != null)
            {
                if (savedProg != null)
                    _economy.Init(_config, savedProg.coins, savedProg.gems);
                else
                    _economy.Init(_config);
            }

            if (_reputation != null)
            {
                if (savedProg != null)
                    _reputation.Init(savedProg.reputationLevel, savedProg.reputationXp);
                else
                    _reputation.Init(0, 0);
                // Init() never raises OnLevelChanged, so loading a save doesn't emit level_up
                _reputation.OnLevelChanged += OnReputationLevelUp;
            }
            _playTimeSeconds = savedProg != null ? Mathf.Max(0, savedProg.playTimeSeconds) : 0;

            // Hook into ads (already initialized by Boot, or init here as fallback)
            var adManager = AdManager.Instance ?? FindAnyObjectByType<AdManager>();
            if (adManager != null)
                adManager.InitializeAds(); // idempotent: fallback if Boot didn't run

            // Wait one frame for GridRenderer.Start() to build the room and entrances
            yield return null;

            // Load progression from cloud save (after grid is built)
            LoadProgression();

            // Preload only the breeds the player has unlocked (async, spread across frames)
            int currentRep = _reputation != null ? _reputation.Level : 0;
            yield return _breedRegistry.PreloadUnlockedAsync(currentRep);

            // Start or resume tutorial FIRST — it may suppress the initial cat spawn.
            var tutMgr = Tutorial.TutorialManager.Instance;
            if (tutMgr != null)
            {
                var save = CloudSaveManager.Instance;
                bool isNewGame = save == null || !save.IsLoaded || !save.HasPersistedSave;
                // Completed tutorial → past the last step, whatever the sequence length
                int resumeStep = isNewGame ? 0
                    : save.Progression.tutorialComplete ? int.MaxValue
                    : save.Progression.tutorialStepIndex;
                tutMgr.Begin(resumeStep);
            }
            else
            {
                Debug.LogWarning("[Hotel] TutorialManager.Instance is null — run Setup Proto Scene.");
            }

            // Spawn first cat only if no tutorial is running and no cats restored from save.
            if (_cats.Count == 0
                && (Tutorial.TutorialManager.Instance == null || !Tutorial.TutorialManager.Instance.IsActive))
            {
                TrySpawnCat();
            }
        }

        private void Update()
        {
            if (_config == null) return;

            // Scaled time: 0 while paused (timeScale 0), capped per frame, so background time is excluded
            _playTimeFraction += Time.deltaTime;
            if (_playTimeFraction >= 1f)
            {
                int whole = (int)_playTimeFraction;
                _playTimeSeconds += whole;
                _playTimeFraction -= whole;
            }

            UpdateArrivals();

            // Departures + pension merged into a single tick (0.5s is fine for these checks)
            _catTickTimer += Time.deltaTime;
            if (_catTickTimer >= CatTickInterval)
            {
                float dt = _catTickTimer;
                _catTickTimer = 0f;
                UpdateCats(dt);
            }

            // Periodic auto-save every 30s (protects against brutal app kill)
            _autoSaveTimer += Time.deltaTime;
            if (_autoSaveTimer >= AutoSaveInterval)
            {
                _autoSaveTimer = 0f;
                SaveProgression();
            }

            // Passive income for cats living on hidden floors (their AI is frozen
            // so they generate no service-based revenue; we compensate with a tick).
            _passiveIncomeTimer += Time.deltaTime;
            if (_passiveIncomeTimer >= PassiveIncomeInterval)
            {
                _passiveIncomeTimer = 0f;
                TickPassiveIncome();
            }
        }

        /// <summary>Rewarded-ad revenue boost (×2 while active) — applies to every coin income.</summary>
        private static float CurrentBoostMultiplier =>
            RevenueBoostManager.Instance != null ? RevenueBoostManager.Instance.BoostMultiplier : 1f;

        private void TickPassiveIncome()
        {
            if (_economy == null || _gridRenderer == null) return;
            int visibleFloor = _gridRenderer.CurrentFloor;
            float total = 0f;
            foreach (var cat in _cats)
            {
                if (cat.Entity == null || cat.Entity.FloorIndex == visibleFloor) continue;
                if (cat.State == CatState.Leaving || cat.State == CatState.Pickup
                    || cat.State == CatState.Adopted) continue;
                float happiness = cat.Happiness != null ? cat.Happiness.Value : 50f;
                // Happier hidden cats earn more; unhappy ones still trickle a small amount.
                total += PassiveIncomePerCat * (happiness / 100f);
            }
            int coins = Mathf.RoundToInt(total * CurrentBoostMultiplier);
            if (coins > 0)
            {
                _economy.AddCoins(coins);
                DevLog.Log($"[Hotel] Passive income tick: +{coins} coins from hidden floors.");
            }
        }

        private void UpdateArrivals()
        {
            // Suppress auto-spawn while the tutorial is running
            if (Tutorial.TutorialManager.Instance != null && Tutorial.TutorialManager.Instance.IsActive) return;

            _arrivalTimer += Time.deltaTime;
            if (_arrivalTimer < _config.arrivalInterval) return;
            _arrivalTimer = 0f;

            int maxCats = FloorProgression.Instance != null
                ? FloorProgression.Instance.MaxCats
                : _config.maxCats;
            if (_cats.Count >= maxCats) return;

            TrySpawnCat();
        }

        /// <summary>Spawn a PENSION cat on demand (used by tutorial). Returns the CatEntity.
        /// Pension duration is forced to 5 minutes, needs full.</summary>
        public CatEntity SpawnTutorialCat()
        {
            var breed = PickRandomBreed();
            if (breed == null) return null;

            var entrance = _gridRenderer.PensionEntrance;
            int prevCount = _cats.Count;
            SpawnCatInternal(breed, CatMode.Pension, entrance, false);

            if (_cats.Count > prevCount)
            {
                var cat = _cats[^1];
                cat.PensionDuration = 300f;
                cat.PensionTimeRemaining = 300f;
                cat.Needs?.FromArray(new float[] { 100f, 100f, 100f, 100f, 100f });
                return cat.Entity;
            }
            return null;
        }

        /// <summary>Spawn a refuge cat on demand (used by tutorial). Returns the CatEntity.</summary>
        public CatEntity SpawnTutorialRefugeCat()
        {
            var entrances = _gridRenderer.Entrances;
            if (entrances == null || entrances.Count == 0) return null;
            var breed = PickRandomBreed();
            if (breed == null) return null;

            var entrance = _gridRenderer.RefugeEntrance;
            int prevCount = _cats.Count;

            // Reuse TrySpawnCat's full logic but force Refuge mode.
            // Simplest: temporarily swap and call the full creation inline.
            // For now, just call TrySpawnCat-style code with mode forced.
            SpawnCatInternal(breed, CatMode.Refuge, entrance, false);

            if (_cats.Count > prevCount)
                return _cats[^1].Entity;
            return null;
        }

        /// <summary>Internal shared cat spawn (extracted from TrySpawnCat).</summary>
        private void SpawnCatInternal(CatBreedData breed, CatMode mode, Vector2Int entrance, bool isSpecial)
        {
            if (breed.controller != null && breed.hasSpecialVariant && breed.specialController != null
                && !isSpecial && UnityEngine.Random.value < breed.specialChance && !HasSpecialOfBreed(breed))
                isSpecial = true;

            var go = new GameObject($"Cat_{_cats.Count}_{breed.breedName}");
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = isSpecial && breed.specialFrontSprite != null ? breed.specialFrontSprite : breed.frontSprite;
            sr.sortingLayerName = "Cats";
            go.AddComponent<Core.SortByY>();

            if (breed.controller != null)
            {
                var animator = go.AddComponent<Animator>();
                var ctrl = isSpecial && breed.specialController != null ? breed.specialController : breed.controller;
                animator.runtimeAnimatorController = ctrl;
                animator.enabled = false;
            }

            float scale = breed.size * UnityEngine.Random.Range(0.6f, 0.8f);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            var needs = go.AddComponent<CatNeeds>();
            needs.Init(breed, _config, isSpecial);
            needs.SetSizeMultiplier(scale);
            if (mode == CatMode.Refuge) needs.SetRefugeStartValues();
            int repDeficit = _reputation.GetDeficit(breed.minReputation);
            needs.SetReputationDeficit(repDeficit);

            var happiness = go.AddComponent<CatHappiness>();
            happiness.Init(needs, _config);
            var moodBubble = go.AddComponent<CatMoodBubble>();
            moodBubble.Init(happiness, needs, _config,
                _moodHappy, _moodEcstatic, _moodDepressed, _moodAggressive, _moodAngry,
                _needHungry, _needThirsty, _needTired, _needBored, _needDirty);

            var entity = go.AddComponent<CatEntity>();
            entity.SetSprites(
                isSpecial && breed.specialFrontSprite != null ? breed.specialFrontSprite : breed.frontSprite,
                isSpecial && breed.specialRightSprite != null ? breed.specialRightSprite : breed.rightSprite,
                isSpecial && breed.specialBackSprite != null ? breed.specialBackSprite : breed.backSprite);
            entity.SetBreed(breed);
            entity.Init(_gridRenderer.Data, entrance, _catSpawner, 0, _gridRenderer);

            string catName = isSpecial ? breed.specialName : CatNames.GetRandomName(breed.name);
            float pensionDuration = mode == CatMode.Pension ? UnityEngine.Random.Range(60f, 300f) : 0f;

            string description = "";
            var traitMods = CatTraitModifiers.Default;
            if (_personalityConfig != null)
            {
                var (desc, mods) = _personalityConfig.GeneratePersonality(breed, catName.GetHashCode());
                description = desc;
                traitMods = mods;
            }
            needs.SetTraitModifiers(traitMods);
            happiness.SetTraitModifiers(traitMods);
            entity.SetTraitModifiers(traitMods);

            var affinityRng = new System.Random(catName.GetHashCode() + 7919);
            CatBreedData likedBreed = null, dislikedBreed = null;
            if (_breedRegistry.Count > 1)
            {
                var otherBreeds = new List<CatBreedData>();
                foreach (var b in _breedRegistry.LoadedBreeds)
                    if (b.breedName != breed.breedName) otherBreeds.Add(b);
                if (otherBreeds.Count > 0 && affinityRng.NextDouble() < _config.likedBreedChance)
                    likedBreed = otherBreeds[affinityRng.Next(otherBreeds.Count)];
                if (otherBreeds.Count > 0 && affinityRng.NextDouble() < _config.dislikedBreedChance)
                {
                    var dc = new List<CatBreedData>();
                    foreach (var b in otherBreeds) if (likedBreed == null || b.breedName != likedBreed.breedName) dc.Add(b);
                    if (dc.Count > 0) dislikedBreed = dc[affinityRng.Next(dc.Count)];
                }
            }

            var instance = new CatInstance
            {
                Entity = entity, Needs = needs, Happiness = happiness, Breed = breed,
                Mode = mode, IsSpecial = isSpecial, CatName = catName, Description = description,
                TraitModifiers = traitMods, LikedBreed = likedBreed, DislikedBreed = dislikedBreed,
                State = CatState.Arriving, PensionTimeRemaining = pensionDuration, PensionDuration = pensionDuration
            };

            if (likedBreed != null || dislikedBreed != null)
            {
                var affinity = go.AddComponent<CatAffinity>();
                affinity.Init(likedBreed, dislikedBreed, happiness, entity, _catSpawner, _config);
            }

            entity.OnServiceUsed += () => OnCatServiceUsed(instance);
            _cats.Add(instance);
            if (_catSpawner != null) _catSpawner.RegisterCat(entity);
            OnCatArrived?.Invoke(instance);
            PlayArrivalSfxFor(mode);

            var floorCells = _gridRenderer.CentralRoomFloorCells;
            if (floorCells.Count > 0)
            {
                var target = floorCells[UnityEngine.Random.Range(0, floorCells.Count)];
                entity.WalkToTarget(target, () => instance.State = CatState.Idle);
            }
            else instance.State = CatState.Idle;
        }

        /// <summary>Single merged loop for departures + pension timers (tick-based).</summary>
        private void UpdateCats(float dt)
        {
            for (int i = _cats.Count - 1; i >= 0; i--)
            {
                var cat = _cats[i];

                // Sample happiness for average tracking
                if (cat.Happiness != null && cat.State == CatState.Idle)
                {
                    cat.HappinessSum += cat.Happiness.Value;
                    cat.HappinessSamples++;
                }

                // Departure check (skip cats already leaving, being picked up/adopted, or still arriving)
                if (cat.Happiness != null && cat.Happiness.ShouldLeave
                    && cat.State != CatState.Leaving && cat.State != CatState.Pickup
                    && cat.State != CatState.Adopted && cat.State != CatState.Arriving)
                {
                    StartUnhappyDeparture(cat);
                    continue;
                }

                // Refuge: an adopter comes after a continuous happy period
                if (cat.Mode == CatMode.Refuge)
                {
                    UpdateAdoption(cat, dt);
                    continue;
                }

                // Pension timer
                if (cat.Mode != CatMode.Pension) continue;
                if (cat.State == CatState.Leaving || cat.State == CatState.Pickup) continue;

                cat.PensionTimeRemaining -= dt;
                if (cat.PensionTimeRemaining <= 0f)
                {
                    ProcessPensionEnd(cat);
                }
            }
        }

        /// <summary>Called when a cat finishes using a service object. Spawns a floating coin.</summary>
        private void OnCatServiceUsed(CatInstance cat)
        {
            DevLog.Log($"[Hotel] OnCatServiceUsed for {cat.CatName}, entity={cat.Entity != null}, happiness={cat.Happiness?.Value}, isUnhappy={cat.Happiness?.IsUnhappy}");
            Tutorial.TutorialManager.Instance?.NotifyEvent(Tutorial.TutorialTrigger.WaitForCatServiceUsed);
            if (cat.Entity == null || cat.Happiness == null) return;
            if (cat.Happiness.IsUnhappy) return;

            _economy.ProcessRevenueTick(
                cat.Happiness, cat.Breed, cat.Entity.transform, cat.IsSpecial);
        }

        /// <summary>
        /// Force-spawn a cat right now (resets the arrival timer).
        /// Used when the tutorial is skipped on an empty hotel so the player isn't left waiting.
        /// </summary>
        public void SpawnCatNow()
        {
            _arrivalTimer = 0f;
            TrySpawnCat();
        }

        private void TrySpawnCat()
        {
            var entrances = _gridRenderer.Entrances;
            if (entrances == null || entrances.Count == 0) return;

            // Pick a breed the player has unlocked
            var breed = PickRandomBreed();
            if (breed == null) return;

            // 80% pension, 20% refuge
            var mode = UnityEngine.Random.value < 0.8f ? CatMode.Pension : CatMode.Refuge;

            // Pick entrance based on mode: pension → top, refuge → bottom
            var entrance = mode == CatMode.Pension
                ? _gridRenderer.PensionEntrance
                : _gridRenderer.RefugeEntrance;

            // Animate entrance door
            var entranceDoor = mode == CatMode.Pension
                ? _gridRenderer.PensionDoor
                : _gridRenderer.RefugeDoor;
            if (entranceDoor != null) entranceDoor.PlayOpenClose();

            // Roll for special variant
            bool isSpecial = breed.hasSpecialVariant
                && breed.specialController != null
                && UnityEngine.Random.value < breed.specialChance
                && !HasSpecialOfBreed(breed);

            // Create the cat GameObject
            var go = new GameObject($"Cat_{_cats.Count}_{breed.breedName}");
            go.transform.SetParent(transform);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = isSpecial && breed.specialFrontSprite != null
                ? breed.specialFrontSprite : breed.frontSprite;

            // Cats always in front of objects — use "Cats" sorting layer
            sr.sortingLayerName = "Cats";
            go.AddComponent<Core.SortByY>();

            if (breed.controller != null)
            {
                var animator = go.AddComponent<Animator>();
                var ctrl = isSpecial && breed.specialController != null
                    ? breed.specialController : breed.controller;
                animator.runtimeAnimatorController = ctrl;
                animator.enabled = false;
            }

            float scale = breed.size * UnityEngine.Random.Range(0.6f, 0.8f);

            go.transform.localScale = new Vector3(scale, scale, 1f);

            // Add CatNeeds & CatHappiness BEFORE CatEntity.Init() so _needs is found
            var needs = go.AddComponent<CatNeeds>();
            needs.Init(breed, _config, isSpecial);
            needs.SetSizeMultiplier(scale);
            if (mode == CatMode.Refuge) needs.SetRefugeStartValues();

            int repDeficit = _reputation.GetDeficit(breed.minReputation);
            needs.SetReputationDeficit(repDeficit);

            var happiness = go.AddComponent<CatHappiness>();
            happiness.Init(needs, _config);

            var moodBubble = go.AddComponent<CatMoodBubble>();
            moodBubble.Init(happiness, needs, _config,
                _moodHappy, _moodEcstatic, _moodDepressed, _moodAggressive, _moodAngry,
                _needHungry, _needThirsty, _needTired, _needBored, _needDirty);
            var entity = go.AddComponent<CatEntity>();
            var frontSpr = isSpecial && breed.specialFrontSprite != null ? breed.specialFrontSprite : breed.frontSprite;
            var rightSpr = isSpecial && breed.specialRightSprite != null ? breed.specialRightSprite : breed.rightSprite;
            var backSpr = isSpecial && breed.specialBackSprite != null ? breed.specialBackSprite : breed.backSprite;
            entity.SetSprites(frontSpr, rightSpr, backSpr);
            entity.SetBreed(breed);
            entity.Init(_gridRenderer.Data, entrance, _catSpawner, 0, _gridRenderer);

            // Pick a name
            string catName = isSpecial ? breed.specialName : CatNames.GetRandomName(breed.name);

            // Pension duration: 1-5 minutes (only for pension cats)
            float pensionDuration = mode == CatMode.Pension
                ? UnityEngine.Random.Range(60f, 300f) : 0f;

            // Generate personality description + gameplay modifiers
            string description = "";
            var traitMods = CatTraitModifiers.Default;
            if (_personalityConfig != null)
            {
                var (desc, mods) = _personalityConfig.GeneratePersonality(breed, catName.GetHashCode());
                description = desc;
                traitMods = mods;
            }

            // Apply trait modifiers to gameplay systems
            needs.SetTraitModifiers(traitMods);
            happiness.SetTraitModifiers(traitMods);
            entity.SetTraitModifiers(traitMods);

            // Generate breed affinities (deterministic)
            var affinityRng = new System.Random(catName.GetHashCode() + 7919);
            CatBreedData likedBreed = null;
            CatBreedData dislikedBreed = null;
            if (_breedRegistry.Count > 1)
            {
                // Build list of other breeds (from currently loaded breeds)
                var otherBreeds = new System.Collections.Generic.List<CatBreedData>();
                foreach (var b in _breedRegistry.LoadedBreeds)
                    if (b.breedName != breed.breedName) otherBreeds.Add(b);

                if (otherBreeds.Count > 0 && affinityRng.NextDouble() < _config.likedBreedChance)
                {
                    likedBreed = otherBreeds[affinityRng.Next(otherBreeds.Count)];
                }

                if (otherBreeds.Count > 0 && affinityRng.NextDouble() < _config.dislikedBreedChance)
                {
                    // Pick a breed different from the liked one
                    var dislikeCandidates = new System.Collections.Generic.List<CatBreedData>();
                    foreach (var b in otherBreeds)
                        if (likedBreed == null || b.breedName != likedBreed.breedName) dislikeCandidates.Add(b);
                    if (dislikeCandidates.Count > 0)
                        dislikedBreed = dislikeCandidates[affinityRng.Next(dislikeCandidates.Count)];
                }
            }

            var instance = new CatInstance
            {
                Entity = entity,
                Needs = needs,
                Happiness = happiness,
                Breed = breed,
                Mode = mode,
                IsSpecial = isSpecial,
                CatName = catName,
                Description = description,
                TraitModifiers = traitMods,
                LikedBreed = likedBreed,
                DislikedBreed = dislikedBreed,
                State = CatState.Arriving,
                PensionTimeRemaining = pensionDuration,
                PensionDuration = pensionDuration
            };

            // Breed affinity system
            if (likedBreed != null || dislikedBreed != null)
            {
                var affinity = go.AddComponent<CatAffinity>();
                affinity.Init(likedBreed, dislikedBreed, happiness, entity, _catSpawner, _config);
            }

            // Generate a floating coin each time this cat finishes using a service
            entity.OnServiceUsed += () => OnCatServiceUsed(instance);

            _cats.Add(instance);
            if (_catSpawner != null) _catSpawner.RegisterCat(entity);
            OnCatArrived?.Invoke(instance);
            PlayArrivalSfxFor(mode);

            // Walk from entrance to a random floor cell
            var floorCells = _gridRenderer.CentralRoomFloorCells;
            if (floorCells.Count > 0)
            {
                var target = floorCells[UnityEngine.Random.Range(0, floorCells.Count)];
                entity.SetArriving();
                entity.WalkToTarget(target, () =>
                {
                    instance.State = CatState.Idle;
                    entity.ClearArriving();
                });
            }
            else
            {
                instance.State = CatState.Idle;
            }

        }

        // Reusable buffer to avoid allocation per spawn
        private readonly List<CatBreedData> _unlockedBuffer = new();

        /// <summary>GDD: only one special cat per breed at a time.</summary>
        private bool HasSpecialOfBreed(CatBreedData breed)
        {
            foreach (var cat in _cats)
                if (cat.IsSpecial && cat.Breed == breed)
                    return true;
            return false;
        }

        private CatBreedData PickRandomBreed()
        {
            int currentRep = _reputation != null ? _reputation.Level : 0;
            _breedRegistry.GetUnlocked(currentRep, _unlockedBuffer);
            if (_unlockedBuffer.Count == 0) return null;
            return _unlockedBuffer[UnityEngine.Random.Range(0, _unlockedBuffer.Count)];
        }

        private void ProcessPensionEnd(CatInstance cat)
        {
            cat.State = CatState.Pickup;
            cat.Entity.SetDeparting();

            // If the cat is on a hidden floor (frozen), the walk would never complete →
            // skip the visual departure and show the panel right away.
            int visibleFloor = _gridRenderer != null ? _gridRenderer.CurrentFloor : 0;
            if (cat.Entity.FloorIndex != visibleFloor)
            {
                ShowPensionEndPanel(cat);
                return;
            }

            // Pension cats leave via top-left entrance (no door animation)
            cat.Entity.WalkToTarget(_gridRenderer.PensionEntrance, () => ShowPensionEndPanel(cat));
        }

        private void ShowPensionEndPanel(CatInstance cat)
        {
            // Calculate payment
            float happiness = cat.Happiness.Value;
            float avgHappiness = cat.AverageHappiness;
            bool boosted = CurrentBoostMultiplier > 1f; // captured now: the boost may expire before collect
            float payment = _config.pensionBaseRate * cat.PensionDuration * (happiness / 100f)
                * cat.Breed.revenueMultiplier * CurrentBoostMultiplier;
            int baseCoins = Mathf.RoundToInt(payment);

            int tipCoins = 0;
            if (happiness > 80f)
                tipCoins = Mathf.RoundToInt(baseCoins * _config.tipPercent);

            int totalCoins = baseCoins + tipCoins;

            // Get front sprite
            Sprite frontSprite = cat.IsSpecial && cat.Breed.specialFrontSprite != null
                ? cat.Breed.specialFrontSprite : cat.Breed.frontSprite;

            // Show panel (game continues running)
            var panel = GetComponent<EndPensionPanel>();
            if (panel != null)
            {
                panel.Show(new PensionEndData
                {
                    CatSprite = frontSprite,
                    CatName = cat.CatName,
                    AvgHappiness = avgHappiness,
                    BaseCoins = baseCoins,
                    TipCoins = tipCoins,
                    TotalCoins = totalCoins
                }, (finalCoins) =>
                {
                    _economy.AddCoins(finalCoins);
                    GameAnalytics.PensionComplete(Mathf.RoundToInt(happiness), finalCoins, cat.IsSpecial,
                        _reputation != null ? _reputation.Level : 0, boosted);

                    _reputation.AwardPensionXp(happiness, cat.IsSpecial);
                    CatHotel.Audio.CatSoundManager.Instance?.PlayDeparture();

                    FinalizeDeparture(cat);
                });
            }
            else
            {
                // Fallback if no panel
                _economy.AddCoins(totalCoins);
                GameAnalytics.PensionComplete(Mathf.RoundToInt(happiness), totalCoins, cat.IsSpecial,
                    _reputation != null ? _reputation.Level : 0, boosted);
                _reputation.AwardPensionXp(happiness, cat.IsSpecial);
                CatHotel.Audio.CatSoundManager.Instance?.PlayDeparture();
                FinalizeDeparture(cat);
            }
        }

        // ========== ADOPTION (refuge) ==========

        private bool IsTutorialRunning =>
            Tutorial.TutorialManager.Instance != null && Tutorial.TutorialManager.Instance.IsActive;

        /// <summary>
        /// Accumulates the continuous happy time of a refuge cat; once it reaches
        /// adoptionHappyDuration an adopter takes the cat (GDD 3.10).
        /// </summary>
        private void UpdateAdoption(CatInstance cat, float dt)
        {
            if (cat.State == CatState.Leaving || cat.State == CatState.Pickup
                || cat.State == CatState.Adopted || cat.State == CatState.Arriving)
                return;

            // The tutorial drives its own refuge cat: no adoption until it is over.
            if (IsTutorialRunning || cat.Happiness == null)
            {
                cat.HappyDuration = 0f;
                return;
            }
            // Not in the middle of a fight or a stair climb (the departure walk would be interrupted).
            if (cat.Entity == null || cat.Entity.IsFighting || cat.Entity.IsChangingFloor)
                return;

            if (cat.Happiness.Value >= _config.adoptionHappyThreshold)
                cat.HappyDuration += dt;
            else
                cat.HappyDuration = 0f;

            if (cat.HappyDuration >= _config.adoptionHappyDuration)
                ProcessAdoption(cat);
        }

        /// <summary>
        /// An adopter takes a refuge cat: the cat walks out through the refuge entrance, then the
        /// adoption recap shows the fee (x2 via rewarded ad). Coins, XP and the departure are
        /// finalized when the recap collects.
        /// </summary>
        public void ProcessAdoption(CatInstance cat)
        {
            if (cat == null || cat.Mode != CatMode.Refuge) return;
            if (cat.State == CatState.Leaving || cat.State == CatState.Pickup || cat.State == CatState.Adopted)
                return;

            cat.State = CatState.Adopted;
            cat.Entity.SetDeparting();

            // Frozen-floor cats: skip the walk so the player still gets the reward.
            int visibleFloor = _gridRenderer != null ? _gridRenderer.CurrentFloor : 0;
            if (cat.Entity.FloorIndex != visibleFloor)
            {
                ShowAdoptionPanel(cat);
                return;
            }

            // Adopted cats leave via the refuge entrance (bottom-left, no door animation)
            cat.Entity.WalkToTarget(_gridRenderer.RefugeEntrance, () => ShowAdoptionPanel(cat));
        }

        private void ShowAdoptionPanel(CatInstance cat)
        {
            float happiness = cat.Happiness != null ? cat.Happiness.Value : _config.adoptionHappyThreshold;
            bool boosted = CurrentBoostMultiplier > 1f;
            float fee = cat.Breed.revenueMultiplier * _config.adoptionFeeMultiplier
                * 100f * (happiness / 100f) * CurrentBoostMultiplier;
            int coins = Mathf.RoundToInt(fee);

            void Collect(int finalCoins)
            {
                _economy.AddCoins(finalCoins);
                GameAnalytics.AdoptionComplete(cat.Breed != null ? cat.Breed.name : null, finalCoins, boosted);
                _reputation.AwardAdoptionXp(happiness, cat.IsSpecial);
                CatHotel.Audio.CatSoundManager.Instance?.PlayDeparture();
                FinalizeDeparture(cat);
            }

            Sprite frontSprite = cat.IsSpecial && cat.Breed.specialFrontSprite != null
                ? cat.Breed.specialFrontSprite : cat.Breed.frontSprite;

            var panel = GetComponent<EndPensionPanel>();
            if (panel != null)
            {
                panel.Show(new PensionEndData
                {
                    CatSprite = frontSprite,
                    CatName = cat.CatName,
                    AvgHappiness = happiness,
                    BaseCoins = coins,
                    TipCoins = 0,
                    TotalCoins = coins,
                    IsAdoption = true
                }, Collect);
            }
            else
            {
                Collect(coins);
            }
        }

        /// <summary>Unhappy cat leaves via right exit with sad walk + door animation.</summary>
        private void StartUnhappyDeparture(CatInstance cat)
        {
            // Asset name (e.g. Breed_Europeen2): unique per variant, unlike breedName
            GameAnalytics.CatLeftUnhappy(cat.Breed != null ? cat.Breed.name : null,
                _reputation != null ? _reputation.Level : 0);
            cat.State = CatState.Leaving;
            cat.Entity.SetDeparting();
            cat.Entity.SetSadWalk();
            CatHotel.Audio.CatSoundManager.Instance?.PlayEscape();

            if (_floatingCoinView != null && cat.Entity != null)
                _floatingCoinView.ForceCollectCoinForCat(cat.Entity.transform);

            // If the cat is frozen on a hidden floor, skip the visible walk and finalize.
            int visibleFloor = _gridRenderer != null ? _gridRenderer.CurrentFloor : 0;
            if (cat.Entity.FloorIndex != visibleFloor)
            {
                FinalizeDeparture(cat);
                return;
            }

            // Unhappy cats leave via the single right exit (with door animation)
            var exit = _gridRenderer.UnhappyExit;
            var exitDoor = _gridRenderer.ExitDoor;
            cat.Entity.WalkToTarget(exit, () =>
            {
                if (exitDoor != null) exitDoor.PlayOpenClose();
                FinalizeDeparture(cat);
            });
        }

        /// <summary>Immediately remove a cat from the game (used by tutorial to despawn the first cat).</summary>
        public void RemoveCat(CatEntity entity)
        {
            if (entity == null) return;
            var cat = _cats.FirstOrDefault(c => c.Entity == entity);
            if (cat != null) FinalizeDeparture(cat);
            else Destroy(entity.gameObject);
        }

        private void FinalizeDeparture(CatInstance cat)
        {
            _cats.Remove(cat);
            if (_catSpawner != null && cat.Entity != null)
                _catSpawner.UnregisterCat(cat.Entity);
            CatNames.ReleaseName(cat.CatName);
            OnCatDeparted?.Invoke(cat);

            // Force-collect floating coin before destroying cat
            if (_floatingCoinView != null && cat.Entity != null)
                _floatingCoinView.ForceCollectCoinForCat(cat.Entity.transform);

            if (cat.Entity != null)
                Destroy(cat.Entity.gameObject);

            // Auto-save after departure
            SaveProgression();
        }

        /// <summary>Get average happiness across all cats.</summary>
        public float GetAverageHappiness()
        {
            if (_cats.Count == 0) return 100f;
            float sum = 0f;
            foreach (var cat in _cats)
            {
                if (cat.Happiness != null)
                    sum += cat.Happiness.Value;
            }
            return sum / _cats.Count;
        }

        private static void PlayArrivalSfxFor(CatMode mode)
        {
            if (CatHotel.Audio.CatSoundManager.Instance == null) return;
            if (mode == CatMode.Refuge)
                CatHotel.Audio.CatSoundManager.Instance.PlayShelterArrival();
            else
                CatHotel.Audio.CatSoundManager.Instance.PlayArrival();
        }

        /// <summary>Number of cats currently above the happy threshold (used by level-up panel).</summary>
        public int CountHappyCats()
        {
            int n = 0;
            foreach (var c in _cats)
            {
                // Cats on their way out (picked up, adopted, leaving) no longer count
                if (c.State == CatState.Leaving || c.State == CatState.Pickup || c.State == CatState.Adopted)
                    continue;
                if (c.Happiness != null && c.Happiness.Value >= ReputationManager.HappyCatThreshold)
                    n++;
            }
            return n;
        }

        private void OnReputationLevelUp(int newLevel)
        {
            GameAnalytics.LevelUp(newLevel, _playTimeSeconds / 60);
        }

        // ========== CLOUD SAVE INTEGRATION ==========

        /// <summary>Collect current progression data for saving.</summary>
        public ProgressionSaveData CollectProgressionData()
        {
            // Preserve fields written by other systems (e.g. TutorialManager)
            var prev = CloudSaveManager.Instance?.Progression;

            var data = new ProgressionSaveData
            {
                reputationLevel = _reputation.Level,
                reputationXp = _reputation.Xp,
                coins = _economy.Coins,
                gems = _economy.Gems,
                floorTileIndex = _gridRenderer.FloorTileIndex,
                // Preserve tutorial state — these are written by TutorialManager, not by hotel state
                tutorialStepIndex = prev?.tutorialStepIndex ?? 0,
                tutorialComplete  = prev?.tutorialComplete ?? false,
                playTimeSeconds = _playTimeSeconds,
                placedObjects = new List<PlacedObjectSaveData>(),
                cats = new List<CatCloudSaveData>()
            };

            // Serialize placed objects
            foreach (var obj in ObjectRegistry.Objects)
            {
                if (obj.Data.isStairs) continue; // stairs are spawned by GridRenderer
                data.placedObjects.Add(new PlacedObjectSaveData
                {
                    objectAssetName = obj.Data.name,
                    gridX = obj.GridPos.x,
                    gridY = obj.GridPos.y,
                    floorIndex = obj.FloorIndex
                });
            }

            // Serialize cats
            foreach (var cat in _cats)
            {
                if (cat.State == CatState.Leaving || cat.State == CatState.Pickup
                    || cat.State == CatState.Adopted)
                    continue; // skip departing cats

                // Skip cats still arriving — they'd be saved with a half-walk position.
                // Saving an Arriving cat that hasn't reached a settled cell would mean restoring it
                // at a random spot anyway. Better to drop it cleanly.
                if (cat.State == CatState.Arriving) continue;

                int[] visited = null;
                if (cat.Entity != null && cat.Entity.VisitedFloors != null)
                {
                    var vf = cat.Entity.VisitedFloors;
                    visited = new int[vf.Count];
                    int i = 0;
                    foreach (var f in vf) visited[i++] = f;
                }

                // Capture exact scale (set randomly at spawn — must persist)
                float catScale = cat.Entity != null
                    ? cat.Entity.transform.localScale.x
                    : 1f;

                // Capture current grid position (cat may be mid-walk → snap to nearest cell)
                int gx = cat.Entity != null ? Mathf.FloorToInt(cat.Entity.transform.position.x) : 0;
                int gy = cat.Entity != null ? Mathf.FloorToInt(cat.Entity.transform.position.y) : 0;

                data.cats.Add(new CatCloudSaveData
                {
                    breedName = cat.Breed.breedName,
                    catName = cat.CatName,
                    mode = cat.Mode.ToString(),
                    isSpecial = cat.IsSpecial,
                    scale = catScale,
                    gridX = gx,
                    gridY = gy,
                    floorIndex = cat.Entity != null ? cat.Entity.FloorIndex : 0,
                    visitedFloors = visited,
                    needs = cat.Needs != null ? cat.Needs.ToArray() : new float[5],
                    happiness = cat.Happiness != null ? cat.Happiness.Value : 50f,
                    pensionDuration = cat.PensionDuration,
                    pensionTimeRemaining = cat.PensionTimeRemaining,
                    happinessSum = cat.HappinessSum,
                    happinessSamples = cat.HappinessSamples,
                    happyDuration = cat.HappyDuration,
                });
            }

            return data;
        }

        /// <summary>Save current progression to CloudSaveManager.</summary>
        public void SaveProgression()
        {
            if (CloudSaveManager.Instance == null) return;
            CloudSaveManager.Instance.Progression = CollectProgressionData();
            CloudSaveManager.Instance.SaveProgression();
        }

        /// <summary>
        /// Restore progression from CloudSaveManager.
        /// Must be called after GridRenderer has built the room.
        /// </summary>
        public void LoadProgression()
        {
            if (CloudSaveManager.Instance == null || !CloudSaveManager.Instance.IsLoaded) return;
            if (!CloudSaveManager.Instance.HasPersistedSave) return;

            var data = CloudSaveManager.Instance.Progression;
            if (data == null) return;

            // Economy + reputation already initialized from save in Start()

            // Restore floor tile visual
            if (data.floorTileIndex >= 0)
                _gridRenderer.SetFloorTileIndex(data.floorTileIndex);

            // Restore placed objects
            RestorePlacedObjects(data.placedObjects);

            // Restore cats
            RestoreCats(data.cats);

            Debug.Log($"[Hotel] Loaded progression: rep={data.reputationLevel}, coins={data.coins}, " +
                      $"objects={data.placedObjects?.Count ?? 0}, cats={data.cats?.Count ?? 0}");
        }

        private void RestorePlacedObjects(List<PlacedObjectSaveData> objects)
        {
            if (objects == null || objects.Count == 0) return;

            // Find all available object data assets
            var allObjectData = Resources.FindObjectsOfTypeAll<HotelObjectData>();

            foreach (var saved in objects)
            {
                var objData = allObjectData.FirstOrDefault(o => o.name == saved.objectAssetName);
                if (objData == null)
                {
                    Debug.LogWarning($"[Hotel] Object asset '{saved.objectAssetName}' not found, skipping");
                    continue;
                }

                var gridPos = new Vector2Int(saved.gridX, saved.gridY);

                // Create the object (same logic as ObjectPlacement.ConfirmPlacement)
                var go = new GameObject($"Obj_{objData.displayName}");
                float posY = objData.wallMount ? gridPos.y + 0.65f : gridPos.y + 0.25f;
                go.transform.position = new Vector3(
                    gridPos.x + objData.size.x * 0.5f, posY, 0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = objData.worldSprite != null ? objData.worldSprite : objData.icon;

                bool isCarpet = objData.category == ObjectCategory.Carpet
                             || (objData.category == ObjectCategory.Decoration
                                && objData.displayName != null
                                && objData.displayName.Contains("Tapis"));

                if (isCarpet)
                {
                    sr.sortingLayerName = "Carpets";
                    go.AddComponent<Core.SortByY>();
                }
                else if (objData.wallMount)
                {
                    sr.sortingLayerName = "Objects";
                    sr.sortingOrder = 0;
                }
                else
                {
                    sr.sortingLayerName = "Objects";
                    go.AddComponent<Core.SortByY>();
                }

                // Scale to fit
                if (sr.sprite != null)
                {
                    float spriteW = sr.sprite.bounds.size.x;
                    float spriteH = sr.sprite.bounds.size.y;
                    if (spriteW > 0f && spriteH > 0f)
                    {
                        float targetW = objData.size.x;
                        float targetH = objData.size.y;
                        float scale = Mathf.Min(targetW / spriteW, targetH / spriteH) * objData.visualScale;
                        go.transform.localScale = new Vector3(scale, scale, 1f);
                    }
                }

                // Animation
                if (objData.animFrames != null && objData.animFrames.Length > 0)
                {
                    var frameAnim = go.AddComponent<Core.SpriteFrameAnimator>();
                    frameAnim.Init(objData.animFrames, objData.animFps);
                }
                else if (objData.worldAnimController != null)
                {
                    var anim = go.AddComponent<Animator>();
                    anim.runtimeAnimatorController = objData.worldAnimController;
                }

                var hotelObj = go.AddComponent<HotelObject>();
                hotelObj.Init(objData, gridPos, saved.floorIndex);
                // Hide the restored sprite if the object's floor isn't the visible one
                if (sr != null && _gridRenderer != null && saved.floorIndex != _gridRenderer.CurrentFloor)
                    sr.enabled = false;
            }
        }

        private void RestoreCats(List<CatCloudSaveData> savedCats)
        {
            if (savedCats == null || savedCats.Count == 0) return;

            foreach (var saved in savedCats)
            {
                // Find breed (lazy-loads from Resources if not cached)
                CatBreedData breed = _breedRegistry.FindByName(saved.breedName);
                if (breed == null)
                {
                    Debug.LogWarning($"[Hotel] Breed '{saved.breedName}' not found, skipping cat");
                    continue;
                }

                Enum.TryParse<CatMode>(saved.mode, out var mode);

                // Create the cat (simplified version of TrySpawnCat for restored cats)
                var go = new GameObject($"Cat_{_cats.Count}_{breed.breedName}");
                go.transform.SetParent(transform);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = saved.isSpecial && breed.specialFrontSprite != null
                    ? breed.specialFrontSprite : breed.frontSprite;
                sr.sortingLayerName = "Cats";
                go.AddComponent<Core.SortByY>();

                if (breed.controller != null)
                {
                    var animator = go.AddComponent<Animator>();
                    var ctrl = saved.isSpecial && breed.specialController != null
                        ? breed.specialController : breed.controller;
                    animator.runtimeAnimatorController = ctrl;
                    animator.enabled = false;
                }

                // Restore exact saved scale (fallback to a random one if save predates this field)
                float scale = saved.scale > 0.01f
                    ? saved.scale
                    : breed.size * UnityEngine.Random.Range(0.6f, 0.8f);
                go.transform.localScale = new Vector3(scale, scale, 1f);

                var needs = go.AddComponent<CatNeeds>();
                needs.Init(breed, _config, saved.isSpecial);
                needs.SetSizeMultiplier(scale);
                // Restore the cat's actual needs from save (don't re-randomize refuge cats —
                // their saved needs ARE their current state).
                if (saved.needs != null && saved.needs.Length == 5)
                    needs.FromArray(saved.needs);
                else if (mode == CatMode.Refuge)
                    needs.SetRefugeStartValues();

                int repDeficit = _reputation.GetDeficit(breed.minReputation);
                needs.SetReputationDeficit(repDeficit);

                var happiness = go.AddComponent<CatHappiness>();
                happiness.Init(needs, _config);

                var moodBubble = go.AddComponent<CatMoodBubble>();
                moodBubble.Init(happiness, needs, _config,
                    _moodHappy, _moodEcstatic, _moodDepressed, _moodAggressive, _moodAngry,
                    _needHungry, _needThirsty, _needTired, _needBored, _needDirty);

                var entity = go.AddComponent<CatEntity>();
                var frontSpr = saved.isSpecial && breed.specialFrontSprite != null
                    ? breed.specialFrontSprite : breed.frontSprite;
                var rightSpr = saved.isSpecial && breed.specialRightSprite != null
                    ? breed.specialRightSprite : breed.rightSprite;
                var backSpr = saved.isSpecial && breed.specialBackSprite != null
                    ? breed.specialBackSprite : breed.backSprite;
                entity.SetSprites(frontSpr, rightSpr, backSpr);
                entity.SetBreed(breed);

                // Restore exact saved position. Fall back to a random floor cell only if
                // the saved data is missing or out of bounds (legacy save).
                int savedFloor = Mathf.Clamp(saved.floorIndex, 0, GridRenderer.FloorCount - 1);
                var savedGrid = _gridRenderer.GetFloorData(savedFloor) ?? _gridRenderer.Data;

                Vector2Int spawnPos = new Vector2Int(saved.gridX, saved.gridY);
                bool savedPosValid = savedGrid != null
                    && savedGrid.InBounds(spawnPos.x, spawnPos.y)
                    && savedGrid.IsWalkable(spawnPos.x, spawnPos.y);

                if (!savedPosValid)
                {
                    var floorCells = _gridRenderer.GetCentralRoomFloorCells(savedFloor)
                                     ?? _gridRenderer.CentralRoomFloorCells;
                    spawnPos = (floorCells != null && floorCells.Count > 0)
                        ? floorCells[UnityEngine.Random.Range(0, floorCells.Count)]
                        : new Vector2Int(10, 7);
                }

                entity.Init(savedGrid, spawnPos, _catSpawner, savedFloor, _gridRenderer);
                if (saved.visitedFloors != null)
                    foreach (var vf in saved.visitedFloors)
                        entity.AddVisitedFloor(vf);

                string description = "";
                var traitMods = CatTraitModifiers.Default;
                if (_personalityConfig != null)
                {
                    var (desc, mods) = _personalityConfig.GeneratePersonality(
                        breed, saved.catName.GetHashCode());
                    description = desc;
                    traitMods = mods;
                }

                needs.SetTraitModifiers(traitMods);
                happiness.SetTraitModifiers(traitMods);
                entity.SetTraitModifiers(traitMods);

                // Breed affinities (deterministic)
                var affinityRng = new System.Random(saved.catName.GetHashCode() + 7919);
                CatBreedData likedBreed = null;
                CatBreedData dislikedBreed = null;
                if (_breedRegistry.Count > 1)
                {
                    var otherBreeds = new List<CatBreedData>();
                    foreach (var b in _breedRegistry.LoadedBreeds)
                        if (b.breedName != breed.breedName) otherBreeds.Add(b);

                    if (otherBreeds.Count > 0 && affinityRng.NextDouble() < _config.likedBreedChance)
                        likedBreed = otherBreeds[affinityRng.Next(otherBreeds.Count)];

                    if (otherBreeds.Count > 0 && affinityRng.NextDouble() < _config.dislikedBreedChance)
                    {
                        var dislikeCandidates = new List<CatBreedData>();
                        foreach (var b in otherBreeds)
                            if (likedBreed == null || b.breedName != likedBreed.breedName)
                                dislikeCandidates.Add(b);
                        if (dislikeCandidates.Count > 0)
                            dislikedBreed = dislikeCandidates[affinityRng.Next(dislikeCandidates.Count)];
                    }
                }

                var instance = new CatInstance
                {
                    Entity = entity,
                    Needs = needs,
                    Happiness = happiness,
                    Breed = breed,
                    Mode = mode,
                    IsSpecial = saved.isSpecial,
                    CatName = saved.catName,
                    Description = description,
                    TraitModifiers = traitMods,
                    LikedBreed = likedBreed,
                    DislikedBreed = dislikedBreed,
                    State = CatState.Idle,
                    PensionDuration = saved.pensionDuration,
                    PensionTimeRemaining = saved.pensionTimeRemaining,
                    HappinessSum = saved.happinessSum,
                    HappinessSamples = saved.happinessSamples,
                    HappyDuration = saved.happyDuration
                };

                if (likedBreed != null || dislikedBreed != null)
                {
                    var affinity = go.AddComponent<CatAffinity>();
                    affinity.Init(likedBreed, dislikedBreed, happiness, entity, _catSpawner, _config);
                }

                entity.OnServiceUsed += () => OnCatServiceUsed(instance);

                _cats.Add(instance);
                if (_catSpawner != null) _catSpawner.RegisterCat(entity);
                OnCatArrived?.Invoke(instance);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus) return;
            // Synchronous: must finish before OS suspends
            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.Progression = CollectProgressionData();
                CloudSaveManager.Instance.SaveProgressionImmediate();
            }
        }

        private void OnApplicationQuit()
        {
            // Synchronous: must finish before process dies
            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.Progression = CollectProgressionData();
                CloudSaveManager.Instance.SaveProgressionImmediate();
            }
        }
    }

    /// <summary>Runtime data for a single cat in the hotel.</summary>
    public class CatInstance
    {
        public CatEntity Entity;
        public CatNeeds Needs;
        public CatHappiness Happiness;
        public CatBreedData Breed;
        public CatMode Mode;
        public CatState State;
        public bool IsSpecial;
        public string CatName;
        public string Description;
        public CatTraitModifiers TraitModifiers;

        // Breed affinities
        public CatBreedData LikedBreed;   // nullable — breed this cat likes
        public CatBreedData DislikedBreed; // nullable — breed this cat dislikes

        // Pension tracking
        public float PensionDuration;
        public float PensionTimeRemaining;

        // Average happiness tracking (sampled each tick)
        public float HappinessSum;
        public int HappinessSamples;
        public float AverageHappiness => HappinessSamples > 0 ? HappinessSum / HappinessSamples : 0f;

        // Adoption tracking
        public float HappyDuration; // seconds continuously happy (for adopter trigger)
    }
}
