using System.Collections.Generic;
using Action = System.Action;
using UnityEngine;
using DG.Tweening;
using CatHotel.Audio;
using CatHotel.Grid;
using CatHotel.Core;
using CatHotel.Hotel;

namespace CatHotel.Cats
{
    public enum CatDirection { Front, Back, Right, Left }

    public class CatEntity : MonoBehaviour
    {
        [Header("Sprites (fallback)")]
        [SerializeField] private Sprite _frontSprite;
        [SerializeField] private Sprite _rightSprite;
        [SerializeField] private Sprite _backSprite;

        [Header("Movement")]
        [SerializeField] private float _cellMoveTime   = 0.7f;
        [SerializeField] private float _idleTimeMin    = 1f;
        [SerializeField] private float _idleTimeMax    = 3f;
        [SerializeField] private int   _wanderStepsMin = 2;
        [SerializeField] private int   _wanderStepsMax = 6;

        [Header("Lick Grooming")]
        [Tooltip("Chance per idle to do a paw-licking animation instead of a regular idle.")]
        [SerializeField, Range(0f, 1f)] private float _lickChance = 0.18f;
        [Tooltip("How long the cat stays in the Lick state before moving on.")]
        [SerializeField] private float _lickDuration = 2.2f;

        // Idle: multiple variants per direction, chosen randomly
        private static readonly string[][] IdleStates =
        {
            new[] { "Idle1_Front", "Idle2_Front", "Idle3_Front" }, // Front
            new[] { "Idle1_Back", "Idle3_Back" },                   // Back (no Idle2)
            new[] { "Idle1_Right", "Idle2_Right", "Idle3_Right" }, // Right
            new[] { "Idle1_Left", "Idle2_Left", "Idle3_Left" },    // Left
        };

        // Lick has no Back variant — Back direction falls back to Front.
        // Index order matches CatDirection: Front, Back, Right, Left
        private static readonly string[] LickByDir =
        {
            "Lick_Front", "Lick_Front", "Lick_Right", "Lick_Left"
        };

        private SpriteRenderer _sr;
        private Animator _animator;
        private GridData _grid;
        private CatHotel.Grid.GridRenderer _gridRenderer;
        private CatSpawner _spawner;
        private CatNeeds _needs;
        private CatHappiness _happiness; // added before CatEntity in every spawn path
        private CatBreedData _breed;
        private Vector2Int _gridPos;
        private Sequence _moveSequence;
        private Tween _pendingAction;
        private CatDirection _currentDir;
        private bool _isWalking;
        private bool _useSadWalk;
        private bool _isFighting;
        private float _fightCooldown;
        private string _chosenRestState;
        private BedSpot _claimedBed;
        private bool _isDeparting;
        private bool _isArriving;

        /// <summary>True while the cat is still walking from its entrance to its first rest cell.</summary>
        public bool IsArriving => _isArriving;

        /// <summary>Called by HotelManager once the arrival walk starts.</summary>
        public void SetArriving() => _isArriving = true;

        /// <summary>Called automatically when the arrival walk completes (or manually).</summary>
        public void ClearArriving() => _isArriving = false;

        // Object interaction
        private HotelObject _targetObject;
        private bool _isUsingObject;
        private bool _isRunning; // true when rushing to satisfy an urgent need

        // Multi-floor
        private int _floorIndex;
        private readonly HashSet<int> _visitedFloors = new() { 0 };
        private bool _isChangingFloor;
        private float _floorChangeCooldown;

        // Pooled path buffer to avoid GC allocation per wander
        private readonly List<Vector2Int> _pathBuffer = new(8);

        public Vector2Int GridPos => _gridPos;
        public bool IsFighting => _isFighting;
        public bool IsUsingObject => _isUsingObject;
        public bool IsChangingFloor => _isChangingFloor;
        public int FloorIndex => _floorIndex;
        public IReadOnlyCollection<int> VisitedFloors => _visitedFloors;
        public SpriteRenderer SpriteRenderer => _sr;
        public CatBreedData Breed => _breed;

        /// <summary>Fired when the cat finishes using a service object.</summary>
        public event Action OnServiceUsed;

        /// <summary>
        /// Sync _gridPos with actual world position.
        /// Must be called whenever a walk sequence is killed mid-way,
        /// otherwise _gridPos stays at the start of the interrupted walk
        /// and all subsequent pathfinding uses a stale position.
        /// </summary>
        private void SyncGridPos()
        {
            Vector3 pos = transform.position;
            var newPos = new Vector2Int(
                Mathf.FloorToInt(pos.x),
                Mathf.FloorToInt(pos.y));
            _gridPos = newPos;
        }

        public void SetSprites(Sprite front, Sprite right, Sprite back)
        {
            _frontSprite = front;
            _rightSprite = right;
            _backSprite = back;
        }

        public void Init(GridData grid, Vector2Int startCell, CatSpawner spawner = null,
            int floorIndex = 0, CatHotel.Grid.GridRenderer gridRenderer = null)
        {
            _grid = grid;
            _gridRenderer = gridRenderer;
            _spawner = spawner;
            _gridPos = startCell;
            _floorIndex = floorIndex;
            _visitedFloors.Add(floorIndex);
            _sr = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();
            _needs = GetComponent<CatNeeds>();
            _happiness = GetComponent<CatHappiness>();
            transform.position = CellToWorld(startCell);

            _currentDir = CatDirection.Front;
            SyncFloorVisibility();
            EnterRest();
        }

        /// <summary>Add a floor to the set the cat remembers having visited.</summary>
        public void AddVisitedFloor(int floor) => _visitedFloors.Add(floor);

        /// <summary>Set the floor this cat is on (used by FloorManager / climb logic).</summary>
        public void SetFloorIndex(int floorIndex)
        {
            _floorIndex = floorIndex;
            _visitedFloors.Add(floorIndex);
            if (_gridRenderer != null)
            {
                var data = _gridRenderer.GetFloorData(floorIndex);
                if (data != null) _grid = data;
            }
            SyncFloorVisibility();
        }

        /// <summary>
        /// Show/hide this cat based on whether its floor is the visible one.
        /// When hiding: kill active tweens + snap to cell center + pause animator. This avoids
        /// mid-tween / mid-animation artifacts when the player returns to this floor.
        /// When showing: resume animator and restart the AI from a clean state.
        /// </summary>
        public void SyncFloorVisibility()
        {
            int visible = _gridRenderer != null ? _gridRenderer.CurrentFloor : 0;
            bool on = _floorIndex == visible;
            if (_sr != null) _sr.enabled = on;

            // Also toggle child sprites (mood bubbles, etc.)
            foreach (var childSr in GetComponentsInChildren<SpriteRenderer>(true))
                childSr.enabled = on;

            _canPlaySfx = on;

            // Don't freeze/unfreeze while the cat is in the middle of a stair climb —
            // the climb manages its own state machine.
            if (_isChangingFloor) return;

            if (on)
            {
                // --- Resume ---
                if (_animator != null) _animator.speed = 1f;
                if (_simulationFrozen)
                {
                    _simulationFrozen = false;
                    if (!_isDeparting)
                    {
                        // Fresh AI pass from the cat's snapped position.
                        _isWalking = false;
                        _chosenRestState = null;
                        EnterRest();
                    }
                }
            }
            else
            {
                // --- Freeze ---
                _moveSequence?.Kill();
                _pendingAction?.Kill();
                SyncGridPos();
                transform.position = CellToWorld(_gridPos);
                _isWalking = false;
                _chosenRestState = null;
                _simulationFrozen = true;
                if (_animator != null) _animator.speed = 0f;
            }
        }

        private bool _canPlaySfx = true;
        private bool _simulationFrozen;
        public bool CanPlaySfx => _canPlaySfx;

        private void LateUpdate()
        {
            // Sort by Y: cats use a higher base (15000) than objects (10000)
            // so they always render in front, except tables (base 20000)
            if (_sr != null)
                _sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f) + 15000;

            if (_fightCooldown > 0f)
                _fightCooldown -= Time.deltaTime;
        }

        /// <summary>Set breed data for speed multiplier.</summary>
        private CatTraitModifiers _traitMods = CatTraitModifiers.Default;

        public void SetBreed(CatBreedData breed)
        {
            _breed = breed;
            ApplySpeed();
        }

        public void SetTraitModifiers(CatTraitModifiers mods)
        {
            _traitMods = mods;
            ApplySpeed();
        }

        private void ApplySpeed()
        {
            float breedSpeed = _breed != null ? _breed.speed : 1f;
            _cellMoveTime = 0.7f / (breedSpeed * _traitMods.SpeedMult);
        }

        private void OnDestroy()
        {
            _moveSequence?.Kill();
            _pendingAction?.Kill();
            ReleaseCurrentObject();
            if (_claimedBed != null && _spawner != null)
            {
                _spawner.ReleaseBed(_claimedBed);
                _claimedBed = null;
            }
        }

        // --- Emotes (one-shot, triggered externally) ---

        public void PlayHappy() => PlayEmote("Happy", 2f);
        public void PlayUnhappy() => PlayEmote("Unhappy", 1f);

        public void PlayPetting(RuntimeAnimatorController handController)
        {
            if (_isFighting) return;

            _moveSequence?.Kill();
            _pendingAction?.Kill();
            _isWalking = false;
            _chosenRestState = null;
            ReleaseCurrentObject();
            ReleaseBedIfClaimed();

            // Purr while being petted (only if visible on screen)
            if (_canPlaySfx)
                CatSoundManager.Instance?.PlayPurring();

            _currentDir = CatDirection.Front;
            PlayAnimState("Pet_Front");

            GameObject handGo = null;
            if (handController != null)
            {
                handGo = new GameObject("HandPet");
                handGo.transform.position = transform.position + new Vector3(0, 0.5f, 0);
                var handSr = handGo.AddComponent<SpriteRenderer>();
                handSr.sortingLayerName = "Bubbles";
                handSr.sortingOrder = 0;
                var handAnim = handGo.AddComponent<Animator>();
                handAnim.runtimeAnimatorController = handController;
            }

            _pendingAction = DOVirtual.DelayedCall(2f, () =>
            {
                if (handGo != null) Object.Destroy(handGo);
                _chosenRestState = null;
                EnterIdle();
            });
        }

        private void ReleaseBedIfClaimed()
        {
            if (_claimedBed != null && _spawner != null)
            {
                _spawner.ReleaseBed(_claimedBed);
                _claimedBed = null;
            }
        }

        private void ReleaseCurrentObject()
        {
            ClearUseOffset();
            if (_targetObject != null)
            {
                // Show toy objects again after cat is done playing
                if (_targetObject.Data.category == CatHotel.Core.ObjectCategory.Play)
                    SetObjectVisible(_targetObject, true);
                _targetObject.Release(GetInstanceID());
                _targetObject = null;
            }
            _isUsingObject = false;
        }

        private static void SetObjectVisible(HotelObject obj, bool visible)
        {
            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = visible;
        }

        private void PlayEmote(string prefix, float duration)
        {
            if (_isFighting) return;
            _moveSequence?.Kill();
            _pendingAction?.Kill();
            _isWalking = false;
            _chosenRestState = null;
            ReleaseCurrentObject();
            ReleaseBedIfClaimed();

            string state = DirectionalState(prefix, _currentDir);
            PlayAnimState(state);

            _pendingAction = DOVirtual.DelayedCall(duration, () =>
            {
                _chosenRestState = null;
                EnterIdle();
            });
        }

        // --- Combat ---

        private bool CanFight()
        {
            if (_animator == null || _isFighting) return false;
            if (_fightCooldown > 0f) return false;
            if (_isArriving || _isDeparting) return false;
            if (_isUsingObject || _isChangingFloor) return false;
            return _animator.HasState(0, Animator.StringToHash("Fight_In_Left"));
        }

        private bool TryInitiateFight()
        {
            if (_spawner == null || !CanFight()) return false;

            // Only fight if happiness is low (GDD: < 50%)
            if (_happiness != null && _happiness.Value >= 50f) return false;

            // Find nearest cat on the SAME Y row and within 1.5 cells horizontally
            CatEntity neighbor = null;
            float bestDist = 1.5f;
            foreach (var other in _spawner.AllCats)
            {
                if (other == this || !other.CanFight() || other.IsFighting) continue;
                // Must be on same grid row
                if (other.GridPos.y != _gridPos.y) continue;
                float dx = Mathf.Abs(transform.position.x - other.transform.position.x);
                float dy = Mathf.Abs(transform.position.y - other.transform.position.y);
                // Must be close horizontally AND at same visual height
                if (dx < bestDist && dy < 0.6f)
                {
                    bestDist = dx;
                    neighbor = other;
                }
            }

            if (neighbor == null) return false;

            CatEntity leftCat = transform.position.x < neighbor.transform.position.x ? this : neighbor;
            CatEntity rightCat = transform.position.x < neighbor.transform.position.x ? neighbor : this;

            RunFightSequence(leftCat, rightCat);
            return true;
        }

        private void EnterFight()
        {
            _isFighting = true;
            _moveSequence?.Kill();
            _pendingAction?.Kill();
            _isWalking = false;
            _chosenRestState = null;
            ReleaseCurrentObject();
            ReleaseBedIfClaimed();
        }

        private void RunFightSequence(CatEntity left, CatEntity right)
        {
            left.EnterFight();
            right.EnterFight();

            // Snap both cats to same Y and face each other, ~0.5 apart
            float midX = (left.transform.position.x + right.transform.position.x) / 2f;
            float midY = (left.transform.position.y + right.transform.position.y) / 2f;
            left.transform.position = new Vector3(midX - 0.4f, midY, 0f);
            right.transform.position = new Vector3(midX + 0.4f, midY, 0f);

            // Apply happiness penalty (GDD: -25)
            if (left._happiness != null) left._happiness.ApplyFightPenalty();
            if (right._happiness != null) right._happiness.ApplyFightPenalty();

            var seq = DOTween.Sequence();
            GameObject cloudGo = null;

            seq.AppendCallback(() =>
            {
                left._currentDir = CatDirection.Right;
                right._currentDir = CatDirection.Left;
                left.PlayAnimState("Fight_In_Right");
                right.PlayAnimState("Fight_In_Left");
            });
            seq.AppendInterval(2f);

            seq.AppendCallback(() =>
            {
                left._sr.enabled = false;
                right._sr.enabled = false;

                Vector3 midpoint = (left.transform.position + right.transform.position) / 2f;
                cloudGo = new GameObject("FightCloud");
                cloudGo.transform.position = midpoint;

                var cloudSr = cloudGo.AddComponent<SpriteRenderer>();
                cloudSr.sortingLayerName = "Bubbles";
                cloudSr.sortingOrder = 0;

                if (_spawner != null)
                {
                    var cloudAnim = cloudGo.AddComponent<Animator>();
                    cloudAnim.runtimeAnimatorController = _spawner.FightCloudController;
                }
            });
            seq.AppendInterval(3f); // GDD: 3 seconds

            seq.AppendCallback(() =>
            {
                if (cloudGo != null) Object.Destroy(cloudGo);
                left._sr.enabled = true;
                right._sr.enabled = true;
                left.PlayAnimState("Fight_Out_Right");
                right.PlayAnimState("Fight_Out_Left");
            });
            seq.AppendInterval(1f);

            seq.AppendCallback(() =>
            {
                left._isFighting = false;
                right._isFighting = false;
                left._fightCooldown = 30f;
                right._fightCooldown = 30f;
                left.EnterRest();
                right.EnterRest();
            });
        }

        // --- Need-Driven AI ---

        /// <summary>
        /// Main decision point. Checks needs and decides what to do next.
        /// Priority: Combat > Seek object for urgent need > Random floor change > Idle/Wander.
        /// </summary>
        private void EnterRest()
        {
            if (_isDeparting) return;
            if (_isChangingFloor) return;
            if (_simulationFrozen) return;
            SyncGridPos();
            _isWalking = false;
            _chosenRestState = null;
            ReleaseCurrentObject();

            if (_floorChangeCooldown > 0f) _floorChangeCooldown -= Time.deltaTime;

            if (TryInitiateFight()) return;
            if (_needs != null && TrySeekObject()) return;
            if (TryRandomFloorChange()) return;
            EnterIdle();
        }

        // ---- Multi-floor: climb stairs ----

        private const float FloorChangeChance = 0.10f;
        private const float FloorChangeCooldownSec = 20f;
        private const float ClimbMinScale = 0.5f;

        private bool TryRandomFloorChange()
        {
            if (_isChangingFloor || _gridRenderer == null) return false;
            if (_floorChangeCooldown > 0f) return false;
            if (CatHotel.Grid.GridRenderer.FloorCount < 2) return false;
            // Don't wander to another floor while the tutorial is running
            if (CatHotel.Tutorial.TutorialManager.Instance != null
                && CatHotel.Tutorial.TutorialManager.Instance.IsActive) return false;
            if (Random.value >= FloorChangeChance) return false;

            // Pick a random floor different from current, among UNLOCKED floors only.
            var prog = CatHotel.Hotel.FloorProgression.Instance;
            int highest = prog != null ? prog.HighestUnlockedFloor : 0;
            if (highest <= 0) return false; // only RDC unlocked → nowhere to go
            int target;
            do { target = Random.Range(0, highest + 1); } while (target == _floorIndex);
            StartFloorChange(target);
            return true;
        }

        /// <summary>
        /// Navigate to the cell at the SOUTH of the stairs using the normal walk system,
        /// then play the climb tween (walk up the stairs, fade to black at the top),
        /// then walk back DOWN the stairs on the new floor to end at the south-adjacent cell.
        /// Entry and exit are always on the same side so the cat ends up in the usable room area.
        /// </summary>
        private void StartFloorChange(int targetFloor)
        {
            if (_gridRenderer == null || targetFloor == _floorIndex) return;
            _isChangingFloor = true;
            _floorChangeCooldown = FloorChangeCooldownSec;
            ReleaseCurrentObject();
            ReleaseBedIfClaimed();

            var stairsBL = _gridRenderer.StairsBottomLeft;
            var stairsSize = _gridRenderer.StairsSize;

            // Always enter and exit on the SOUTH side (same column, one cell below the stair footprint).
            int laneX = stairsBL.x + stairsSize.x / 2;
            int approachY = stairsBL.y - 1;
            var approach = new Vector2Int(laneX, approachY);

            if (_gridPos == approach)
            {
                PlayClimbSequence(targetFloor, laneX, stairsBL, stairsSize);
                return;
            }

            WalkToTarget(approach, () =>
                PlayClimbSequence(targetFloor, laneX, stairsBL, stairsSize));
        }

        private void PlayClimbSequence(int targetFloor, int laneX,
            Vector2Int stairsBL, Vector2Int stairsSize)
        {
            // Everything on a single vertical lane (laneX + 0.5 world units).
            float laneWorldX = laneX + 0.5f;

            int southCellY  = stairsBL.y - 1;                        // entry/exit cell (same side)
            int topStairY   = stairsBL.y + stairsSize.y - 1;         // last stair row (north)
            float southCenterY = southCellY + 0.5f;
            float topStairCenterY = topStairY + 0.5f;

            Vector3 origScale = transform.localScale;
            if (origScale == Vector3.zero) origScale = Vector3.one;

            // Snap to the south approach cell center (eliminates any residual drift).
            transform.position = new Vector3(laneWorldX, southCenterY, 0f);

            // PHASE 1 — walk NORTH onto the stairs on the CURRENT floor, scale 1 → 0.5.
            _currentDir = CatDirection.Back;
            PlayAnimState($"Walk_{_currentDir}");

            Vector3 p1End = new Vector3(laneWorldX, topStairCenterY, 0f);
            float   p1Dist = Mathf.Abs(p1End.y - southCenterY);
            float   p1Dur  = p1Dist * _cellMoveTime;

            var seq = DOTween.Sequence();
            seq.Append(transform.DOMove(p1End, p1Dur).SetEase(Ease.Linear));
            seq.Join(transform.DOScale(origScale * ClimbMinScale, p1Dur).SetEase(Ease.Linear));

            // Swap floor at the top. Cat keeps the same world position (stairs occupy
            // the same grid cells on every floor, so (laneWorldX, topStairCenterY) is still
            // at the top of the stairs on the new floor).
            seq.AppendCallback(() =>
            {
                SetFloorIndex(targetFloor);
                _currentDir = CatDirection.Front;
                PlayAnimState($"Walk_{_currentDir}");
            });

            // PHASE 2 — walk SOUTH back off the stairs on the NEW floor, scale 0.5 → 1.
            Vector3 p2End = new Vector3(laneWorldX, southCenterY, 0f);
            float   p2Dist = Mathf.Abs(topStairCenterY - p2End.y);
            float   p2Dur  = p2Dist * _cellMoveTime;

            seq.Append(transform.DOMove(p2End, p2Dur).SetEase(Ease.Linear));
            seq.Join(transform.DOScale(origScale, p2Dur).SetEase(Ease.Linear));

            seq.OnComplete(() =>
            {
                _gridPos = new Vector2Int(laneX, southCellY);
                transform.localScale = origScale;
                _isChangingFloor = false;
                _chosenRestState = null;

                // Re-sync visibility now that the climb is done. If the new floor isn't
                // the one the player is watching, SyncFloorVisibility will freeze the cat.
                SyncFloorVisibility();
                if (!_simulationFrozen)
                    Wander();
            });
        }

        /// <summary>
        /// Find the most urgent need and walk to an object that satisfies it.
        /// Prefers objects on the current floor; falls back to objects on floors
        /// the cat has already visited (triggers a stair climb).
        /// </summary>
        private bool TrySeekObject()
        {
            var urgentNeed = _needs.GetMostUrgentNeed();
            if (urgentNeed == null) return false;

            // 1) Try current floor first.
            var obj = ObjectRegistry.FindNearest(urgentNeed.Value, _gridPos, _floorIndex);
            if (obj != null)
            {
                var usePos = FindUsePosition(obj);
                // Verify path exists before reserving — avoids stuck-walk-in-place
                var testPath = _grid.FindPath(_gridPos, usePos);
                if (testPath == null || testPath.Count < 2)
                {
                    // Can't reach this object — skip
                }
                else if (obj.TryReserve(GetInstanceID()))
                {
                    _targetObject = obj;
                    _isUsingObject = false;
                    // Run if the need is critical (below seekThreshold)
                    _isRunning = _needs != null && _needs.GetNeed(urgentNeed.Value) < 40f;
                    WalkToTarget(usePos, () => { _isRunning = false; StartUsingObject(urgentNeed.Value); });
                    return true;
                }
            }

            // 2) Fallback: check other visited floors.
            if (_visitedFloors.Count > 1 && !_isChangingFloor)
            {
                foreach (var f in _visitedFloors)
                {
                    if (f == _floorIndex) continue;
                    var alt = ObjectRegistry.FindNearest(urgentNeed.Value, _gridPos, f);
                    if (alt != null)
                    {
                        // Go to stairs, change floor, then re-seek on arrival.
                        StartFloorChange(f);
                        return true;
                    }
                }
            }

            return false;
        }

        private void ApplyUseOffset(HotelObject obj) { }
        private void ClearUseOffset() { }

        /// <summary>
        /// Determines which animation to play for Play-category objects.
        /// Scratchers and cat trees use Scratch_In → Scratch_Boucle → Scratch_Out sequence.
        /// Other Play objects (wool ball) use the standard Play animation.
        /// </summary>
        private string GetPlayAnimPrefix()
        {
            if (_targetObject == null) return "Play";
            string name = _targetObject.Data.displayName;
            if (name != null && (name.Contains("Griffoir") || name.Contains("Arbre")))
            {
                // Check if this cat has scratch animations
                if (_animator != null && _animator.HasState(0, Animator.StringToHash("Scratch_Boucle_Left")))
                    return "Scratch_Boucle";
            }
            return "Play";
        }

        private Vector2Int FindUsePosition(HotelObject obj)
        {
            return obj.GridPos;
        }

        /// <summary>
        /// Cat arrived at object. Play use animation and satisfy need over time.
        /// </summary>
        private void StartUsingObject(NeedType need)
        {
            if (_targetObject == null)
            {
                EnterIdle();
                return;
            }

            _isUsingObject = true;

            // Offset cat to the side of the object so sprites don't overlap
            ApplyUseOffset(_targetObject);

            // Hide toy objects while cat is playing with them (but not scratchers/trees)
            if (_targetObject.Data.category == CatHotel.Core.ObjectCategory.Play)
            {
                string objName = _targetObject.Data.displayName;
                bool isScratchObject = objName != null && (objName.Contains("Griffoir") || objName.Contains("Arbre"));
                if (!isScratchObject)
                    SetObjectVisible(_targetObject, false);
            }

            string animPrefix = need switch
            {
                NeedType.Hunger => "Eat",
                NeedType.Thirst => "Drink",
                NeedType.Sleep => "Sleep",
                NeedType.Play => GetPlayAnimPrefix(),
                NeedType.Clean => "Clean",
                _ => "Eat"
            };

            _chosenRestState = DirectionalState(animPrefix, _currentDir);
            PlayAnimState(_chosenRestState);

            // Play action SFX (only if this cat is on the visible floor)
            if (_canPlaySfx)
            {
                if (need == NeedType.Hunger)
                    CatSoundManager.Instance?.PlayEat();
                else if (need == NeedType.Thirst)
                    CatSoundManager.Instance?.PlayDrink();
                else if (need == NeedType.Clean)
                    CatSoundManager.Instance?.PlayLitter();
            }

            float duration = _targetObject.Data.useDuration;
            float ratePerSec = _targetObject.SatisfactionRate;
            float elapsed = 0f;

            DevLog.Log($"[CatEntity] {name} started using object for need={need}, duration={duration}s");
            _pendingAction = DOVirtual.Float(0f, duration, duration, t =>
            {
                float dt = t - elapsed;
                elapsed = t;
                if (_needs != null)
                    _needs.Satisfy(need, ratePerSec * dt);
            }).OnComplete(() =>
            {
                DevLog.Log($"[CatEntity] {name} finished using object");
                ReleaseCurrentObject();
                _chosenRestState = null;
                OnServiceUsed?.Invoke();
                EnterIdle();
            });
        }

        private void EnterIdle()
        {
            _isWalking = false;

            // Rare: grooming animation (licks paw) — uses the full animation duration
            bool doLick = Random.value < _lickChance;
            float idleTime;

            if (doLick)
            {
                _chosenRestState = LickByDir[(int)_currentDir];
                idleTime = _lickDuration;
            }
            else
            {
                // Don't reuse a previous Lick state — pick a new regular idle
                if (_chosenRestState == null || _chosenRestState.StartsWith("Lick_"))
                {
                    var options = IdleStates[(int)_currentDir];
                    _chosenRestState = options[Random.Range(0, options.Length)];
                }
                idleTime = Random.Range(_idleTimeMin, _idleTimeMax);
            }

            PlayAnimState(_chosenRestState);

            _pendingAction = DOVirtual.DelayedCall(idleTime, () =>
            {
                // Clear Lick so the cat doesn't repeat grooming next tick
                if (_chosenRestState != null && _chosenRestState.StartsWith("Lick_"))
                    _chosenRestState = null;

                // After idle, check needs again before wandering
                if (_needs != null && TrySeekObject()) return;
                Wander();
            });
        }

        // --- Targeted Movement (BFS pathfinding) ---

        public void WalkToTarget(Vector2Int target)
        {
            WalkToTarget(target, null);
        }

        /// <summary>Force sad walk animation (used for unhappy departures).</summary>
        public void SetSadWalk() => _useSadWalk = true;

        /// <summary>Mark cat as departing — stops all AI behavior.</summary>
        public void SetDeparting()
        {
            _isDeparting = true;
            _pendingAction?.Kill();
            if (_isUsingObject && _targetObject != null)
            {
                _targetObject.Release(GetInstanceID());
                _targetObject = null;
                _isUsingObject = false;
            }
        }

        public void WalkToTarget(Vector2Int target, System.Action onArrival)
        {
            if (_isFighting) return;
            _moveSequence?.Kill();
            _pendingAction?.Kill();
            SyncGridPos();
            _isWalking = false;
            _chosenRestState = null;

            var path = _grid.FindPath(_gridPos, target);
            if (path == null || path.Count < 2)
            {
                if (onArrival != null)
                {
                    onArrival();
                    if (!_isWalking && !_isUsingObject && !_isFighting)
                        EnterRest();
                }
                else EnterRest();
                return;
            }

            _isWalking = true;
            _moveSequence = DOTween.Sequence();

            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int cell = path[i];
                Vector2Int from = path[i - 1];
                Vector2Int delta = cell - from;

                CatDirection dir = DeltaToDirection(delta);
                _moveSequence.AppendCallback(() => SetWalkDirection(dir));

                Vector3 worldTarget = CellToWorld(cell);
                float moveTime = _isRunning ? _cellMoveTime * 0.6f : _cellMoveTime;
                _moveSequence.Append(
                    transform.DOMove(worldTarget, moveTime)
                        .SetEase(Ease.Linear));
            }

            Vector2Int finalCell = path[^1];
            _moveSequence.OnComplete(() =>
            {
                _gridPos = finalCell;
                _isWalking = false;
                if (onArrival != null) onArrival();
                // If callback didn't start a new action, restart AI
                if (!_isWalking && !_isUsingObject && !_isFighting)
                    EnterRest();
            });
        }

        // --- Random Movement ---

        private void Wander()
        {
            SyncGridPos();
            int steps = Random.Range(_wanderStepsMin, _wanderStepsMax + 1);
            var path = BuildRandomPath(steps);

            if (path.Count == 0)
            {
                EnterRest();
                return;
            }

            _isWalking = true;
            _chosenRestState = null;
            _moveSequence = DOTween.Sequence();

            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int target = path[i];
                Vector2Int from = i == 0 ? _gridPos : path[i - 1];
                Vector2Int delta = target - from;

                CatDirection dir = DeltaToDirection(delta);
                _moveSequence.AppendCallback(() => SetWalkDirection(dir));

                Vector3 worldTarget = CellToWorld(target);
                _moveSequence.Append(
                    transform.DOMove(worldTarget, _cellMoveTime)
                        .SetEase(Ease.Linear));
            }

            Vector2Int finalCell = path[^1];
            _moveSequence.OnComplete(() =>
            {
                _gridPos = finalCell;
                EnterRest();
            });
        }

        private void SetWalkDirection(CatDirection dir)
        {
            _currentDir = dir;
            if (_useSadWalk && dir == CatDirection.Right)
            {
                PlayAnimState("SadWalk_Right");
                return;
            }
            if (_isRunning)
            {
                string runState = $"Run_{dir}";
                if (_animator != null && _animator.HasState(0, Animator.StringToHash(runState)))
                {
                    PlayAnimState(runState);
                    return;
                }
                // Run state missing for this breed — fall back to Walk so the cat
                // still animates instead of sliding with a static sprite.
                DevLog.Warn($"[CatEntity] Missing {runState} on '{_animator?.runtimeAnimatorController?.name}' — fallback to Walk_{dir}");
            }
            PlayAnimState($"Walk_{dir}");
        }

        // --- Helpers ---

        private static string DirectionalState(string prefix, CatDirection dir)
        {
            return dir == CatDirection.Back
                ? $"{prefix}_Front"
                : $"{prefix}_{dir}";
        }

        private void PlayAnimState(string stateName)
        {
            if (_animator == null) return;
            _sr.flipX = false;

            int hash = Animator.StringToHash(stateName);
            if (_animator.HasState(0, hash))
            {
                if (!_animator.enabled) _animator.enabled = true;
                _animator.Play(hash, 0, 0f);
            }
            else
            {
                _animator.enabled = false;
                switch (_currentDir)
                {
                    case CatDirection.Back:
                        _sr.sprite = _backSprite != null ? _backSprite : _frontSprite;
                        break;
                    case CatDirection.Right:
                        _sr.sprite = _rightSprite != null ? _rightSprite : _frontSprite;
                        break;
                    case CatDirection.Left:
                        _sr.sprite = _rightSprite != null ? _rightSprite : _frontSprite;
                        _sr.flipX = true;
                        break;
                    default:
                        _sr.sprite = _frontSprite;
                        break;
                }
            }
        }

        private List<Vector2Int> BuildRandomPath(int steps)
        {
            _pathBuffer.Clear();
            Vector2Int current = _gridPos;

            for (int i = 0; i < steps; i++)
            {
                var neighbors = _grid.GetFloorNeighbors(current.x, current.y);

                // Never wander into Door cells (entrance/exit corridors) — those are
                // for targeted walks only. Prevents cats from drifting into dark corridors.
                neighbors.RemoveAll(n => _grid.GetCell(n.x, n.y) != CellType.Floor);

                if (_pathBuffer.Count >= 1)
                    neighbors.Remove(_pathBuffer.Count >= 2 ? _pathBuffer[^2] : _gridPos);

                if (neighbors.Count == 0) break;

                Vector2Int next = neighbors[Random.Range(0, neighbors.Count)];
                _pathBuffer.Add(next);
                current = next;
            }

            return _pathBuffer;
        }

        private static CatDirection DeltaToDirection(Vector2Int delta)
        {
            if (Mathf.Abs(delta.y) >= Mathf.Abs(delta.x))
                return delta.y > 0 ? CatDirection.Back : CatDirection.Front;
            return delta.x > 0 ? CatDirection.Right : CatDirection.Left;
        }

        private static Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }
    }
}
