using UnityEngine;
using UnityEngine.InputSystem;
using CatHotel.Audio;
using CatHotel.Core;
using CatHotel.Grid;
using CatHotel.Economy;

namespace CatHotel.Hotel
{
    /// <summary>
    /// Handles buying objects from the shop and placing them on the grid.
    /// Flow: ShopPanel calls BeginPlacement → object follows pointer → tap to place →
    /// hold+drag to reposition → Ready to confirm, Cancel to abort.
    /// </summary>
    public class ObjectPlacement : MonoBehaviour
    {
        [SerializeField] private GridRenderer _gridRenderer;
        [SerializeField] private EconomyManager _economy;

        [Header("Button Sprites")]
        [SerializeField] private Sprite[] _readyFrames;
        [SerializeField] private Sprite[] _cancelFrames;

        private HotelObjectData _currentData;
        private GameObject _previewObj;
        private SpriteRenderer _previewSr;
        private Vector2Int _currentGridPos;
        private bool _isPlacing;
        private bool _isDragging;
        private bool _isValid;

        // Animated action buttons (world-space sprites)
        private GameObject _readyBtnObj;
        private GameObject _cancelBtnObj;
        private SpriteRenderer _readySr;
        private SpriteRenderer _cancelSr;
        private float _animTimer;
        private const float FrameInterval = 0.1f; // 10 FPS
        private int _animFrame;

        // Button sizes in world units
        private const float ReadySize = 0.7f;
        private const float CancelSize = 0.5f;
        private const float BtnOffsetY = 0.1f; // gap between object top and buttons

        private Camera _cam;

        public bool IsPlacing => _isPlacing;

        private void Start()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (!_isPlacing) return;

            HandleDrag();
            AnimateButtons();
            HandleTap();
        }

        /// <summary>
        /// Start placement mode for a given object.
        /// Called by ShopPanel after purchase tap.
        /// </summary>
        public void BeginPlacement(HotelObjectData data)
        {
            if (_isPlacing) CancelPlacement();

            _currentData = data;
            _isPlacing = true;
            _isDragging = false;

            // Create preview object
            _previewObj = new GameObject($"Preview_{data.displayName}");
            _previewSr = _previewObj.AddComponent<SpriteRenderer>();
            _previewSr.sprite = data.icon;
            _previewSr.sortingOrder = 15;
            _previewSr.color = new Color(1f, 1f, 1f, 0.7f);

            // Scale to fit grid size
            ScaleToFit(_previewObj, _previewSr, data);

            // Place at the center of the camera view
            if (data.wallMount)
                _currentGridPos = FindVisibleWallCell();
            else
                _currentGridPos = FindVisibleFloorCell();

            UpdatePreviewPosition();
            CreateButtons();
            UpdateValidity();
        }

        private bool IsTapOnButton(Vector3 worldPos)
        {
            if (_readyBtnObj != null &&
                Vector2.Distance(worldPos, _readyBtnObj.transform.position) < ReadySize * 0.6f)
                return true;
            if (_cancelBtnObj != null &&
                Vector2.Distance(worldPos, _cancelBtnObj.transform.position) < CancelSize * 0.6f)
                return true;
            return false;
        }

        private void HandleDrag()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            // Start drag on press — but NOT if tapping a button
            if (pointer.press.wasPressedThisFrame)
            {
                Vector2 screenPos = pointer.position.ReadValue();
                Vector3 worldPos = _cam.ScreenToWorldPoint(screenPos);

                if (IsTapOnButton(worldPos))
                    return; // let HandleTap process this

                // Check if tapping on the preview object
                float dist = Vector2.Distance(worldPos, _previewObj.transform.position);
                if (dist < Mathf.Max(_currentData.size.x, _currentData.size.y) * 0.8f)
                {
                    _isDragging = true;
                }
            }

            // End drag on release
            if (pointer.press.wasReleasedThisFrame)
            {
                _isDragging = false;
            }

            // Move while dragging
            if (_isDragging)
            {
                Vector2 screenPos = pointer.position.ReadValue();
                Vector3 worldPos = _cam.ScreenToWorldPoint(screenPos);
                int gx = Mathf.FloorToInt(worldPos.x);
                int gy = _currentData.wallMount ? GetTopWallY() : Mathf.FloorToInt(worldPos.y);
                var newPos = new Vector2Int(gx, gy);

                if (newPos != _currentGridPos)
                {
                    _currentGridPos = newPos;
                    UpdatePreviewPosition();
                    UpdateValidity();
                }
            }
        }

        private void HandleTap()
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;
            if (_isDragging) return;

            Vector2 screenPos = pointer.position.ReadValue();
            Vector3 worldPos = _cam.ScreenToWorldPoint(screenPos);

            // Check Ready button tap
            if (_readyBtnObj != null)
            {
                float dist = Vector2.Distance(worldPos, _readyBtnObj.transform.position);
                if (dist < ReadySize * 0.6f)
                {
                    if (_isValid)
                        ConfirmPlacement();
                    else
                        UISoundManager.Instance?.PlayTapNegative();
                    return;
                }
            }

            // Check Cancel button tap
            if (_cancelBtnObj != null)
            {
                float dist = Vector2.Distance(worldPos, _cancelBtnObj.transform.position);
                if (dist < CancelSize * 0.6f)
                {
                    CancelPlacement();
                    return;
                }
            }

            // Tap on grid = reposition
            int gx = Mathf.FloorToInt(worldPos.x);
            int gy = _currentData.wallMount ? GetTopWallY() : Mathf.FloorToInt(worldPos.y);
            var gridData = _gridRenderer.Data;
            if (gridData != null && gridData.InBounds(gx, gy))
            {
                _currentGridPos = new Vector2Int(gx, gy);
                UpdatePreviewPosition();
                UpdateValidity();
            }
        }

        private void UpdatePreviewPosition()
        {
            if (_previewObj == null) return;
            float cx = _currentGridPos.x + _currentData.size.x * 0.5f;
            float cy;
            if (_currentData.wallMount)
                cy = _currentGridPos.y + 0.45f; // centered on wall tile
            else
                cy = _currentGridPos.y + 0.25f;
            _previewObj.transform.position = new Vector3(cx, cy, 0f);
            UpdateButtonPositions();
        }

        private void UpdateValidity()
        {
            var gridData = _gridRenderer.Data;
            if (gridData == null) { _isValid = false; return; }

            var rect = new RectInt(_currentGridPos, _currentData.size);

            bool cellsOk;
            if (_currentData.wallMount)
            {
                // Wall objects: check that all cells in the rect are Wall cells
                cellsOk = true;
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    int y = _currentGridPos.y;
                    if (!gridData.InBounds(x, y) || gridData.GetCell(x, y) != CellType.Wall)
                    {
                        cellsOk = false;
                        break;
                    }
                }
            }
            else
            {
                // Floor objects: check all cells are walkable floor
                cellsOk = true;
                for (int y = rect.yMin; y < rect.yMax; y++)
                    for (int x = rect.xMin; x < rect.xMax; x++)
                    {
                        if (!gridData.InBounds(x, y) || !gridData.IsWalkable(x, y))
                        {
                            cellsOk = false;
                            break;
                        }
                        if (gridData.GetCell(x, y) == CellType.Door)
                        {
                            cellsOk = false;
                            break;
                        }
                    }
            }

            // Check placement constraints
            int floor = _gridRenderer != null ? _gridRenderer.CurrentFloor : 0;
            bool areaFree;
            if (_currentData.requiresTable)
            {
                var belowRect = new RectInt(_currentGridPos.x, _currentGridPos.y - 1,
                    _currentData.size.x, 1);
                areaFree = ObjectRegistry.HasTableAt(belowRect, floor)
                        && ObjectRegistry.IsAreaFree(rect, floor);
            }
            else
            {
                areaFree = ObjectRegistry.IsAreaFree(rect, floor);
            }

            _isValid = cellsOk && areaFree;

            // Update preview tilemap
            _gridRenderer.ShowPreview(rect, _isValid);

            // Tint preview sprite
            _previewSr.color = _isValid
                ? new Color(1f, 1f, 1f, 0.8f)
                : new Color(1f, 0.4f, 0.4f, 0.6f);

            // Dim ready button if invalid
            if (_readySr != null)
                _readySr.color = _isValid ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        }

        private void ConfirmPlacement()
        {
            if (!_isValid || _currentData == null) return;

            // Debit coins
            if (!_economy.TrySpend(_currentData.cost))
            {
                UISoundManager.Instance?.PlayTapNegative();
                Debug.LogWarning("[Placement] Not enough coins");
                return;
            }

            UISoundManager.Instance?.PlayTapPositive();
            CatHotel.Services.GameAnalytics.ObjectBought(_currentData.name, CatHotel.Services.GameAnalytics.CurrencyCoins, _currentData.cost);

            // Create the real HotelObject
            var go = new GameObject($"Obj_{_currentData.displayName}");
            float posY;
            if (_currentData.wallMount)
                posY = _currentGridPos.y + 0.45f;
            else
                posY = _currentGridPos.y + 0.25f;
            go.transform.position = new Vector3(
                _currentGridPos.x + _currentData.size.x * 0.5f, posY, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _currentData.worldSprite != null ? _currentData.worldSprite : _currentData.icon;

            // Assign sorting layer based on object type
            bool isCarpet = _currentData.category == ObjectCategory.Carpet
                         || _currentData.category == ObjectCategory.Decoration
                            && _currentData.displayName != null
                            && _currentData.displayName.Contains("Tapis");

            if (isCarpet)
            {
                sr.sortingLayerName = "Carpets";
                go.AddComponent<Core.SortByY>();
            }
            else if (_currentData.wallMount)
            {
                sr.sortingLayerName = "Objects";
                sr.sortingOrder = 0; // wall objects don't Y-sort
            }
            else if (_currentData.requiresTable)
            {
                sr.sortingLayerName = "Objects";
                // On-table objects: follow table's sortingOrder + 1
                SpriteRenderer tableSr = null;
                var tableCell = new Vector2Int(_currentGridPos.x, _currentGridPos.y - 1);
                int searchFloor = _gridRenderer != null ? _gridRenderer.CurrentFloor : 0;
                foreach (var obj in ObjectRegistry.GetFloor(searchFloor))
                {
                    var objRect = new RectInt(obj.GridPos, obj.Data.size);
                    if (objRect.Contains(tableCell))
                    {
                        tableSr = obj.GetComponent<SpriteRenderer>();
                        break;
                    }
                }
                if (tableSr != null)
                {
                    var follower = go.AddComponent<Core.SortOrderFollower>();
                    follower.Init(tableSr, 1);
                }
                else
                {
                    go.AddComponent<Core.SortByY>();
                }
            }
            else
            {
                // Floor objects (beds, bowls, litter, trees, tables...)
                sr.sortingLayerName = "Objects";
                go.AddComponent<Core.SortByY>();
            }

            // Scale to fit
            ScaleToFit(go, sr, _currentData);

            // Animated objects (aquarium, etc.)
            if (_currentData.animFrames != null && _currentData.animFrames.Length > 0)
            {
                var frameAnim = go.AddComponent<Core.SpriteFrameAnimator>();
                frameAnim.Init(_currentData.animFrames, _currentData.animFps);
            }
            else if (_currentData.worldAnimController != null)
            {
                var anim = go.AddComponent<Animator>();
                anim.runtimeAnimatorController = _currentData.worldAnimController;
            }

            int placeFloor = _gridRenderer != null ? _gridRenderer.CurrentFloor : 0;
            var hotelObj = go.AddComponent<HotelObject>();
            hotelObj.Init(_currentData, _currentGridPos, placeFloor);
            // HotelObject.OnEnable will call ObjectRegistry.Register automatically

            string placedName = _currentData.displayName;
            Vector2Int placedPos = _currentGridPos;
            var placedCategory = _currentData.category;
            CleanupPlacement();
            Debug.Log($"[Placement] Placed {placedName} at {placedPos}");
            CatHotel.Audio.CatSoundManager.Instance?.PlayItemDrop();
            Tutorial.TutorialManager.Instance?.NotifyEvent(
                Tutorial.TutorialTrigger.WaitForObjectPlaced, placedCategory);

            // Auto-save after placing object
            var hotel = GetComponent<HotelManager>() ?? FindAnyObjectByType<HotelManager>();
            if (hotel != null) hotel.SaveProgression();
        }

        public void CancelPlacement()
        {
            UISoundManager.Instance?.PlayTapNeutral();
            CleanupPlacement();
        }

        private void CleanupPlacement()
        {
            _isPlacing = false;
            _isDragging = false;
            _currentData = null;

            if (_previewObj != null) Destroy(_previewObj);
            if (_readyBtnObj != null) Destroy(_readyBtnObj);
            if (_cancelBtnObj != null) Destroy(_cancelBtnObj);

            _gridRenderer.ClearPreview();
        }

        private void CreateButtons()
        {
            // Ready button (top-right of object)
            _readyBtnObj = new GameObject("ReadyBtn");
            _readySr = _readyBtnObj.AddComponent<SpriteRenderer>();
            _readySr.sortingOrder = 25;
            if (_readyFrames != null && _readyFrames.Length > 0)
                _readySr.sprite = _readyFrames[0];
            SetSpriteWorldSize(_readyBtnObj, _readySr, ReadySize);

            // Cancel button (bottom-right of object)
            _cancelBtnObj = new GameObject("CancelBtn");
            _cancelSr = _cancelBtnObj.AddComponent<SpriteRenderer>();
            _cancelSr.sortingOrder = 25;
            if (_cancelFrames != null && _cancelFrames.Length > 0)
                _cancelSr.sprite = _cancelFrames[0];
            SetSpriteWorldSize(_cancelBtnObj, _cancelSr, CancelSize);

            _animFrame = 0;
            _animTimer = 0f;
            UpdateButtonPositions();
        }

        private void UpdateButtonPositions()
        {
            if (_previewObj == null) return;

            float right = _currentGridPos.x + _currentData.size.x + 0.1f;
            float top = _currentGridPos.y + _currentData.size.y;
            float bottom = _currentGridPos.y;

            if (_readyBtnObj != null)
                _readyBtnObj.transform.position = new Vector3(right + ReadySize * 0.5f, top - ReadySize * 0.5f + BtnOffsetY, 0f);
            if (_cancelBtnObj != null)
                _cancelBtnObj.transform.position = new Vector3(right + CancelSize * 0.5f, bottom + CancelSize * 0.5f - BtnOffsetY, 0f);
        }

        private void AnimateButtons()
        {
            _animTimer += Time.deltaTime;
            if (_animTimer < FrameInterval) return;
            _animTimer = 0f;

            _animFrame++;

            if (_readyFrames != null && _readyFrames.Length > 0 && _readySr != null)
                _readySr.sprite = _readyFrames[_animFrame % _readyFrames.Length];

            if (_cancelFrames != null && _cancelFrames.Length > 0 && _cancelSr != null)
                _cancelSr.sprite = _cancelFrames[_animFrame % _cancelFrames.Length];
        }

        private const float WallMountScaleFactor = 0.75f;

        private static void ScaleToFit(GameObject go, SpriteRenderer sr, HotelObjectData data)
        {
            if (sr.sprite == null) return;
            float spriteW = sr.sprite.bounds.size.x;
            float spriteH = sr.sprite.bounds.size.y;
            if (spriteW <= 0f || spriteH <= 0f) return;

            float targetW = data.size.x;
            float targetH = data.size.y;
            float scale = Mathf.Min(targetW / spriteW, targetH / spriteH) * data.visualScale;
            if (data.wallMount) scale *= WallMountScaleFactor;
            go.transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// Find a floor cell near the center of the current camera view.
        /// </summary>
        private Vector2Int FindVisibleFloorCell()
        {
            var gridData = _gridRenderer.Data;
            Vector3 camCenter = _cam.transform.position;
            int cx = Mathf.FloorToInt(camCenter.x);
            int cy = Mathf.FloorToInt(camCenter.y);

            // Spiral search outward from camera center to find nearest walkable, non-door cell
            for (int radius = 0; radius < 30; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                        int x = cx + dx;
                        int y = cy + dy;
                        if (!gridData.InBounds(x, y)) continue;
                        if (!gridData.IsWalkable(x, y)) continue;
                        if (gridData.GetCell(x, y) == CellType.Door) continue;
                        return new Vector2Int(x, y);
                    }
                }
            }

            // Fallback
            var floorCells = _gridRenderer.CentralRoomFloorCells;
            return floorCells.Count > 0 ? floorCells[floorCells.Count / 2] : new Vector2Int(10, 7);
        }

        private int GetTopWallY()
        {
            var gridData = _gridRenderer.Data;
            if (gridData == null) return 29;
            // Top wall row = first wall row from top of the central room
            for (int y = GridData.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < GridData.Width; x++)
                {
                    if (gridData.GetCell(x, y) == CellType.Wall)
                    {
                        // Check if there's floor below → this is the top wall
                        if (y > 0 && gridData.IsWalkable(x, y - 1))
                            return y;
                    }
                }
            }
            return 29;
        }

        private Vector2Int FindVisibleWallCell()
        {
            int wallY = GetTopWallY();
            var gridData = _gridRenderer.Data;
            Vector3 camCenter = _cam.transform.position;
            int cx = Mathf.FloorToInt(camCenter.x);

            // Find nearest wall cell at wallY from camera center X
            for (int radius = 0; radius < 30; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = cx + dx;
                    if (gridData.InBounds(x, wallY) && gridData.GetCell(x, wallY) == CellType.Wall)
                    {
                        int floorW = _gridRenderer != null ? _gridRenderer.CurrentFloor : 0;
                        if (ObjectRegistry.IsAreaFree(new RectInt(x, wallY, _currentData.size.x, 1), floorW))
                            return new Vector2Int(x, wallY);
                    }
                }
            }
            return new Vector2Int(cx, wallY);
        }

        private static void SetSpriteWorldSize(GameObject go, SpriteRenderer sr, float worldSize)
        {
            if (sr.sprite == null) return;
            float spriteSize = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            if (spriteSize <= 0f) return;
            float scale = worldSize / spriteSize;
            go.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
