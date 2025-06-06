using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks; // For async operations
using System.Linq; // For HashSet an ToList

namespace MatchAndEarn.Gameplay
{
    public class GridManager : MonoBehaviour
    {
        public int gridWidth = 8;
        public int gridHeight = 8;
        public GameObject tilePrefab; // Assign in Unity Editor (Prefab with Tile.cs and SpriteRenderer)
        public Tile[,] grid;

        // Assign these in the Unity Editor. The order should correspond to TileType enum values.
        // For example, tileSprites[0] for Red, tileSprites[1] for Green, etc.
        public Sprite[] tileSprites;
        public Sprite dartSprite; // Assign in Editor
        public Sprite horizontalBombSprite; // Assign in Editor
        public Sprite verticalBombSprite; // Assign in Editor
        public Sprite assimilationBombSprite; // Assign in Editor

        private Tile firstSelectedTile;
        private Tile secondSelectedTile;
        public bool isProcessingMove = false; // Public for Tile to check

        void Start()
        {
            if (tilePrefab == null)
            {
                Debug.LogError("GridManager: tilePrefab is not assigned in the Inspector!");
                return;
            }
            if (tileSprites == null || tileSprites.Length == 0)
            {
                Debug.LogError("GridManager: tileSprites array is not assigned or is empty in the Inspector!");
                return;
            }
            // Ensure the number of sprites roughly matches the number of tile types (excluding 'Empty')
            if (tileSprites.Length < System.Enum.GetValues(typeof(Tile.TileType)).Length -1)
            {
                Debug.LogWarning("GridManager: There are fewer tile sprites than tile types. Some tiles may not have unique sprites.");
            }

            InitializeGrid();
        }

        void InitializeGrid()
        {
            grid = new Tile[gridWidth, gridHeight];
            firstSelectedTile = null;
            secondSelectedTile = null;

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    CreateTile(x, y, true); // isInitialCreation = true
                }
            }
        }

        // Modified to handle initial creation vs. refill and set visual position
        void CreateTile(int x, int y, bool isInitialCreation)
        {
            if (tilePrefab == null) return;

            // For refills, new tiles might spawn off-screen and drop in.
            // For initial, they are placed directly.
            Vector3 spawnPosition = isInitialCreation ? new Vector3(x, y, 0) : new Vector3(x, gridHeight + 1, 0); // Spawn above grid for refill
            GameObject newTileObject = Instantiate(tilePrefab, spawnPosition, Quaternion.identity);
            newTileObject.name = $"Tile_{x}_{y}";
            newTileObject.transform.SetParent(transform);

            Tile tileComponent = newTileObject.GetComponent<Tile>();
            if (tileComponent == null)
            {
                Debug.LogError($"GridManager: Tile prefab does not have a Tile component attached! Failed at ({x},{y})");
                Destroy(newTileObject);
                return;
            }

            Tile.TileType randomType;
            do
            {
                randomType = GetRandomTileType();
                // Ensure new tile doesn't create immediate match with tiles below it if it's a refill
            } while (IsMatchOnSpawn(x, y, randomType) || (!isInitialCreation && IsMatchWithBelow(x,y,randomType)));

            Sprite tileSprite = GetSpriteForType(randomType);
            if (tileSprite == null && randomType != Tile.TileType.Empty)
            {
                Debug.LogWarning($"GridManager: No sprite found for TileType {randomType}. Tile at ({x},{y}) will be invisible or use default.");
            }

            tileComponent.Initialize(randomType, x, y, tileSprite, this);
            grid[x, y] = tileComponent;

            // If not initial creation, this tile will be moved into place by DropTiles or similar logic
            // If it's initial, its position is already (x,y). If it's a refill, it will be animated down.
            // For now, we'll ensure its logical position is correct, visual position handled by DropTiles.
            if (!isInitialCreation)
            {
                // This new tile will be handled by DropTiles, which will set its final position.
                // For now, we set its target logical position. Visuals will be updated by DropTiles.
                // Visual spawn from top:
                tileComponent.transform.position = new Vector3(x, gridHeight + 0.5f, 0);
            }
            else
            {
                tileComponent.transform.position = new Vector3(x,y,0);
            }
            tileComponent.UpdateSpriteBasedOnState(); // Ensure correct sprite is shown
        }

        // Helper for refill to avoid immediate matches with tiles below the newly spawned one
        bool IsMatchWithBelow(int x, int y, Tile.TileType type)
        {
            if (y > 1 && grid[x, y - 1] != null && grid[x, y - 1].type == type &&
                grid[x, y - 2] != null && grid[x, y - 2].type == type)
            {
                return true;
            }
            return false;
        }

        Tile.TileType GetRandomTileType()
        {
            // Get all values of TileType, excluding 'Empty'
            List<Tile.TileType> availableTypes = System.Enum.GetValues(typeof(Tile.TileType))
                                                       .Cast<Tile.TileType>()
                                                       .Where(t => t != Tile.TileType.Empty)
                                                       .ToList();
            if (availableTypes.Count == 0)
            {
                Debug.LogError("No tile types available for random selection (excluding Empty).");
                return Tile.TileType.Red; // Fallback, though this state is problematic
            }
            int randomIndex = Random.Range(0, availableTypes.Count);
            return availableTypes[randomIndex];
        }

        Sprite GetSpriteForType(Tile.TileType type)
        {
            if (type == Tile.TileType.Empty) return null;

            int typeIndex = (int)type;
            if (typeIndex >= 0 && typeIndex < tileSprites.Length)
            {
                return tileSprites[typeIndex];
            }
            Debug.LogWarning($"GridManager: No sprite configured for TileType: {type}. Index {typeIndex} is out of bounds for tileSprites array (length {tileSprites.Length}).");
            return null; // Or a default error/missing sprite
        }

        bool IsMatchOnSpawn(int x, int y, Tile.TileType type)
        {
            if (type == Tile.TileType.Empty) return false; // Empty tiles don't form matches

            // Check left for two identical tiles
            if (x >= 2 && grid[x - 1, y] != null && grid[x - 1, y].type == type &&
                grid[x - 2, y] != null && grid[x - 2, y].type == type)
            {
                return true;
            }

            // Check down for two identical tiles
            if (y >= 2 && GetTileAt(x, y - 1) != null && GetTileAt(x, y - 1).type == type &&
                GetTileAt(x, y - 2) != null && GetTileAt(x, y - 2).type == type)
            {
                return true;
            }
            // No initial match found
            return false;
        }

        public Tile GetTileAt(int x, int y)
        {
            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            {
                return grid[x, y];
            }
            return null;
        }

        public async void SelectTile(Tile tile)
        {
            if (isProcessingMove || tile == null || tile.type == Tile.TileType.Empty) return;

            if (firstSelectedTile == null)
            {
                firstSelectedTile = tile;
                firstSelectedTile.SetSelected(true);
            }
            else
            {
                // Prevent selecting the same tile twice for a swap
                if (firstSelectedTile == tile)
                {
                    firstSelectedTile.SetSelected(false);
                    firstSelectedTile = null;
                    return;
                }
                secondSelectedTile = tile;
                secondSelectedTile.SetSelected(true);

                // Check for adjacency
                if (Mathf.Abs(firstSelectedTile.x - secondSelectedTile.x) + Mathf.Abs(firstSelectedTile.y - secondSelectedTile.y) == 1)
                {
                    isProcessingMove = true; // Set flag before async operation
                    await ProcessSwap(firstSelectedTile, secondSelectedTile);
                }
                else // Not adjacent
                {
                    firstSelectedTile.SetSelected(false);
                    secondSelectedTile.SetSelected(false);
                    ClearSelectionsAndState(false); // Don't set isProcessingMove to false here, ProcessSwap wasn't called
                }
            }
        }

        private void ClearSelectionsAndState(bool processingDone = true)
        {
            if(firstSelectedTile != null) firstSelectedTile.SetSelected(false);
            if(secondSelectedTile != null) secondSelectedTile.SetSelected(false);
            firstSelectedTile = null;
            secondSelectedTile = null;
            if(processingDone) isProcessingMove = false;
        }

        private async Task ProcessSwap(Tile tile1, Tile tile2)
        {
            await VisuallySwapTiles(tile1, tile2);
            SwapTilesData(tile1, tile2);

            List<Tile> matchedTiles = FindAllMatches();
            Tile pivotTile = null;
            bool specialCreated = false;

            // Determine swipe direction for potential bomb
            Tile.BombDirection createdBombDirection = Tile.BombDirection.None;
            if (Mathf.Abs(tile1.x - tile2.x) > 0) // Horizontal swipe
                createdBombDirection = Tile.BombDirection.Horizontal;
            else if (Mathf.Abs(tile1.y - tile2.y) > 0) // Vertical swipe
                createdBombDirection = Tile.BombDirection.Vertical;

            // Check for 5-match first
            int matchLength1 = CountContiguousMatches(tile1, matchedTiles, tile1.type);
            int matchLength2 = CountContiguousMatches(tile2, matchedTiles, tile2.type);

            // Priority: 6+ (Assimilation), 5 (Directional), 4 (Dart)
            if (matchedTiles.Contains(tile1) && matchLength1 >= 6)
            {
                pivotTile = tile1;
                await ConvertAndDestroyMatches(matchedTiles, pivotTile, Tile.SpecialType.AssimilationBomb);
                specialCreated = true;
            }
            else if (matchedTiles.Contains(tile2) && matchLength2 >= 6)
            {
                pivotTile = tile2;
                await ConvertAndDestroyMatches(matchedTiles, pivotTile, Tile.SpecialType.AssimilationBomb);
                specialCreated = true;
            }
            else if (matchedTiles.Contains(tile1) && matchLength1 == 5)
            {
                pivotTile = tile1;
                if (createdBombDirection != Tile.BombDirection.None)
                {
                    await ConvertAndDestroyMatches(matchedTiles, pivotTile, Tile.SpecialType.DirectionalBomb, createdBombDirection);
                    specialCreated = true;
                }
                // If no valid direction for directional bomb (e.g. center of L/T match not from swipe)
                // still might want to make it a bomb, or default to dart, or just clear. For now, requires swipe.
            }
            else if (matchedTiles.Contains(tile2) && matchLength2 == 5)
            {
                pivotTile = tile2;
                 if (createdBombDirection != Tile.BombDirection.None)
                {
                    await ConvertAndDestroyMatches(matchedTiles, pivotTile, Tile.SpecialType.DirectionalBomb, createdBombDirection);
                    specialCreated = true;
                }
            }
            else if (matchedTiles.Contains(tile1) && matchLength1 == 4) // Check for Dart if no 5-match made
            {
                pivotTile = tile1;
                await ConvertAndDestroyMatches(matchedTiles, pivotTile, Tile.SpecialType.Dart);
                specialCreated = true;
            }
            else if (matchedTiles.Contains(tile2) && matchLength2 == 4) // Check for Dart
            {
                pivotTile = tile2;
                await ConvertAndDestroyMatches(matchedTiles, pivotTile, Tile.SpecialType.Dart);
                specialCreated = true;
            }


            if (specialCreated)
            {
                await HandleCascadeAndRefill();
                await CheckMatchesAndProcess();
            }
            else if (matchedTiles.Count > 0) // Regular 3-match
            {
                await DestroyMatchedTiles(matchedTiles); // Regular destruction
                await HandleCascadeAndRefill();
                await CheckMatchesAndProcess(); // For chain reactions
            }
            else // No match from swap
            {
                await Task.Delay(100); // Brief pause for player to see failed swap
                SwapTilesData(tile1, tile2); // Revert data
                await VisuallySwapTiles(tile1, tile2); // Revert visuals
            }
            ClearSelectionsAndState();
        }


        private void SwapTilesData(Tile tile1, Tile tile2)
        {
            // Swap references in the grid array
            grid[tile1.x, tile1.y] = tile2;
            grid[tile2.x, tile2.y] = tile1;

            // Swap internal coordinates
            int tempX = tile1.x;
            int tempY = tile1.y;
            tile1.x = tile2.x;
            tile1.y = tile2.y;
            tile2.x = tempX;
            tile2.y = tempY;
        }

        private async Task VisuallySwapTiles(Tile tile1, Tile tile2)
        {
            Vector3 pos1 = tile1.transform.position;
            Vector3 pos2 = tile2.transform.position;

            // Simple immediate swap for now. Replace with animation (e.g., LeanTween or Coroutine)
            tile1.transform.position = pos2;
            tile2.transform.position = pos1;

            await Task.Delay(150); // Placeholder for swap animation duration, adjust as needed
        }

        // This method is now primarily for handling cascading matches after an initial move or power-up.
        private async Task<bool> CheckMatchesAndProcess()
        {
            if(isProcessingMove && firstSelectedTile != null) { /* Safety break, should be cleared by ProcessSwap or ActivatePowerUp */ }

            List<Tile> allMatchedTiles = FindAllMatches();
            if (allMatchedTiles.Count > 0)
            {
                isProcessingMove = true; // Ensure no input during cascade processing
                // Cascading matches typically don't form new special tiles, just destroy.
                await DestroyMatchedTiles(allMatchedTiles);
                await HandleCascadeAndRefill();
                await CheckMatchesAndProcess(); // Recursive call for chain reactions
                ClearSelectionsAndState(); // Clear after full cascade finishes
                return true;
            }
            ClearSelectionsAndState(); // Ensure state is cleared if no matches found initially
            return false;
        }

        // Helper to count contiguous tiles of the same type as startTile within a list of already matched tiles.
        // This is to determine the length of a specific match segment that startTile is part of.
        private int CountContiguousMatches(Tile startTile, List<Tile> allFoundMatches, Tile.TileType targetType)
        {
            if (startTile == null || !allFoundMatches.Contains(startTile) || startTile.type != targetType)
            {
                return 0;
            }

            Queue<Tile> queue = new Queue<Tile>();
            HashSet<Tile> visited = new HashSet<Tile>();
            int count = 0;

            queue.Enqueue(startTile);
            visited.Add(startTile);

            int currentLengthHorizontal = 0;
            int currentLengthVertical = 0;

            // Check Horizontal
            HashSet<Tile> horizontalSegment = new HashSet<Tile>();
            Queue<Tile> hQueue = new Queue<Tile>();
            hQueue.Enqueue(startTile);
            horizontalSegment.Add(startTile);

            while(hQueue.Count > 0)
            {
                Tile current = hQueue.Dequeue();
                int[] dx = {-1, 1, 0, 0}; // Check only left/right for horizontal

                for(int i=0; i<2; ++i) // Only check dx[0] and dx[1]
                {
                    int nx = current.x + dx[i];
                    int ny = current.y; // y stays same for horizontal
                    Tile neighbor = GetTileAt(nx, ny);
                    if(neighbor != null && neighbor.type == targetType && allFoundMatches.Contains(neighbor) && !horizontalSegment.Contains(neighbor))
                    {
                        horizontalSegment.Add(neighbor);
                        hQueue.Enqueue(neighbor);
                    }
                }
            }
            currentLengthHorizontal = horizontalSegment.Count;

            // Check Vertical
            HashSet<Tile> verticalSegment = new HashSet<Tile>();
            Queue<Tile> vQueue = new Queue<Tile>();
            vQueue.Enqueue(startTile);
            verticalSegment.Add(startTile);

            while(vQueue.Count > 0)
            {
                Tile current = vQueue.Dequeue();
                int[] dy = {0, 0, -1, 1}; // Check only up/down for vertical

                for(int i=2; i<4; ++i) // Only check dy[2] and dy[3]
                {
                    int nx = current.x; // x stays same for vertical
                    int ny = current.y + dy[i];
                    Tile neighbor = GetTileAt(nx, ny);
                     if(neighbor != null && neighbor.type == targetType && allFoundMatches.Contains(neighbor) && !verticalSegment.Contains(neighbor))
                    {
                        verticalSegment.Add(neighbor);
                        vQueue.Enqueue(neighbor);
                    }
                }
            }
            currentLengthVertical = verticalSegment.Count;

            return Mathf.Max(currentLengthHorizontal, currentLengthVertical);
        }


        private List<Tile> FindAllMatches()
        {
            HashSet<Tile> matchedTiles = new HashSet<Tile>();

            // Horizontal matches
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth - 2; x++)
                {
                    Tile t1 = GetTileAt(x, y);
                    Tile t2 = GetTileAt(x + 1, y);
                    Tile t3 = GetTileAt(x + 2, y);

                    if (t1 != null && t2 != null && t3 != null &&
                        t1.type != Tile.TileType.Empty &&
                        t1.type == t2.type && t1.type == t3.type)
                    {
                        matchedTiles.Add(t1);
                        matchedTiles.Add(t2);
                        matchedTiles.Add(t3);
                        // Check for 4th and 5th tile in a row
                        if (x + 3 < gridWidth)
                        {
                            Tile t4 = GetTileAt(x + 3, y);
                            if (t4 != null && t4.type == t1.type)
                            {
                                matchedTiles.Add(t4);
                                if (x + 4 < gridWidth)
                                {
                                     Tile t5 = GetTileAt(x + 4, y);
                                     if (t5 != null && t5.type == t1.type) matchedTiles.Add(t5);
                                }
                            }
                        }
                    }
                }
            }

            // Vertical matches
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight - 2; y++)
                {
                    Tile t1 = GetTileAt(x, y);
                    Tile t2 = GetTileAt(x, y + 1);
                    Tile t3 = GetTileAt(x, y + 2);

                    if (t1 != null && t2 != null && t3 != null &&
                        t1.type != Tile.TileType.Empty && t1.specialType == Tile.SpecialType.None && // Only match normal tiles for forming new matches
                        t2.specialType == Tile.SpecialType.None && t3.specialType == Tile.SpecialType.None &&
                        t1.type == t2.type && t1.type == t3.type)
                    {
                        matchedTiles.Add(t1);
                        matchedTiles.Add(t2);
                        matchedTiles.Add(t3);
                        if (y + 3 < gridHeight)
                        {
                            Tile t4 = GetTileAt(x, y + 3);
                            if (t4 != null && t4.type == t1.type && t4.specialType == Tile.SpecialType.None)
                            {
                                matchedTiles.Add(t4);
                                if (y + 4 < gridHeight)
                                {
                                    Tile t5 = GetTileAt(x, y + 4);
                                    if (t5 != null && t5.type == t1.type && t5.specialType == Tile.SpecialType.None) matchedTiles.Add(t5);
                                }
                            }
                        }
                    }
                }
            }
            return matchedTiles.ToList();
        }

        private async Task ConvertAndDestroyMatches(List<Tile> allMatchedTiles, Tile pivotTile, Tile.SpecialType specialToCreate, Tile.BombDirection direction = Tile.BombDirection.None)
        {
            if (pivotTile == null)
            {
                Debug.LogError("PivotTile is null. Cannot create special tile.");
                await DestroyMatchedTiles(allMatchedTiles); // Fallback
                return;
            }

            pivotTile.specialType = specialToCreate;
            if (specialToCreate == Tile.SpecialType.DirectionalBomb)
            {
                pivotTile.bombDirection = direction;
            }
            pivotTile.UpdateSpriteBasedOnState();

            List<Tile> tilesToDestroy = new List<Tile>();
            foreach (Tile t in allMatchedTiles)
            {
                if (t != pivotTile) // Don't destroy the pivot tile that becomes special
                {
                    tilesToDestroy.Add(t);
                }
            }
            await DestroyMatchedTiles(tilesToDestroy, false); // Don't destroy the pivot tile
            grid[pivotTile.x, pivotTile.y] = pivotTile; // Ensure pivot tile remains in grid
        }


        private async Task DestroyMatchedTiles(List<Tile> matchedTiles, bool includePivotInSpecialCreation = true)
        {
            List<Tile> toActuallyDestroy = new List<Tile>(matchedTiles);
            if (!includePivotInSpecialCreation) // This flag is a bit of a hack for ConvertAndDestroy
            {
                // This logic might be redundant if ConvertAndDestroyMatches already filters the list.
            }

            foreach (Tile tile in toActuallyDestroy)
            {
                if (tile != null && GetTileAt(tile.x, tile.y) == tile && tile.specialType == Tile.SpecialType.None) // Only destroy if it's still there and not already special
                {
                    DestroySingleTile(tile, false); // Don't trigger cascade per tile
                }
            }
            await Task.Delay(100); // Placeholder for group destruction animation
        }

        // Overload for simpler calls
        private async Task DestroyMatchedTiles(List<Tile> matchedTiles)
        {
            await DestroyMatchedTiles(matchedTiles, true);
        }


        // Destroys a single tile and optionally triggers cascade/refill.
        private void DestroySingleTile(Tile tileToDestroy, bool triggerCascade = true)
        {
            if (tileToDestroy != null && GetTileAt(tileToDestroy.x, tileToDestroy.y) == tileToDestroy)
            {
                grid[tileToDestroy.x, tileToDestroy.y] = null;
                Destroy(tileToDestroy.gameObject);

                // if (triggerCascade) // This logic is now handled by the main game loop (ProcessSwap, ActivateDartTile)
                // {
                //     await HandleCascadeAndRefill();
                //     await CheckMatchesAndProcess();
                // }
            }
        }

        public async void ActivateDartTile(Tile dartTile)
        {
            if (isProcessingMove || dartTile == null || dartTile.specialType != Tile.SpecialType.Dart) return;

            isProcessingMove = true;
            List<Tile> availableTargets = new List<Tile>();
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Tile t = GetTileAt(x,y);
                    if (t != null && t != dartTile && t.type != Tile.TileType.Empty) // Can target any non-empty tile, including other specials
                    {
                        availableTargets.Add(t);
                    }
                }
            }

            if (availableTargets.Count > 0)
            {
                Tile targetTile = availableTargets[Random.Range(0, availableTargets.Count)];

                // Placeholder for particle effect from dart to target
                Debug.Log($"Dart activated: From ({dartTile.x},{dartTile.y}) to ({targetTile.x},{targetTile.y})");
                await Task.Delay(300); // Animation delay

                DestroySingleTile(targetTile, false); // Destroy target, don't trigger cascade yet
                DestroySingleTile(dartTile, false);   // Destroy dart tile, don't trigger cascade yet

                await HandleCascadeAndRefill();
                await CheckMatchesAndProcess(); // Check for new matches after cascade
            }
            else
            {
                // No targets, maybe just destroy itself or do nothing
                DestroySingleTile(dartTile, false);
                await HandleCascadeAndRefill();
                await CheckMatchesAndProcess();
            }
            ClearSelectionsAndState();
        }


        private async Task HandleCascadeAndRefill()
        {
            await DropTiles();
            await RefillGrid();
            // A small delay might be good for visual pacing, but not strictly necessary for logic
            // await Task.Delay(100);
        }

        public async void ActivateAssimilationBomb(Tile bombTile)
        {
            if (isProcessingMove || bombTile == null || bombTile.specialType != Tile.SpecialType.AssimilationBomb) return;

            isProcessingMove = true;
            Tile.TileType typeToAssimilate = bombTile.type;
            List<Tile> tilesToDestroy = new List<Tile>();
            tilesToDestroy.Add(bombTile); // Add the bomb itself to the destruction list

            Debug.Log($"Activating Assimilation Bomb at ({bombTile.x},{bombTile.y}) for type {typeToAssimilate}");

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Tile currentTile = GetTileAt(x,y);
                    // Add other tiles of the same type, ensuring they are not the bomb itself (already added)
                    // and are not already some other special type that might be immune or have its own effect.
                    if (currentTile != null && currentTile != bombTile &&
                        currentTile.type == typeToAssimilate && currentTile.specialType == Tile.SpecialType.None)
                    {
                        tilesToDestroy.Add(currentTile);
                    }
                }
            }

            // Placeholder for particle effects (e.g., from bomb to all assimilated tiles)
            await Task.Delay(500); // Animation delay

            await DestroySpecificTiles(tilesToDestroy);

            await HandleCascadeAndRefill();
            await CheckMatchesAndProcess();
            ClearSelectionsAndState();
        }

        public async void ActivateDirectionalBomb(Tile bombTile)
        {
            if (isProcessingMove || bombTile == null || bombTile.specialType != Tile.SpecialType.DirectionalBomb) return;

            isProcessingMove = true;
            List<Tile> tilesToDestroy = new List<Tile>();
            tilesToDestroy.Add(bombTile); // The bomb itself is destroyed

            if (bombTile.bombDirection == Tile.BombDirection.Horizontal)
            {
                Debug.Log($"Activating Horizontal Bomb at ({bombTile.x},{bombTile.y})");
                for (int x = 0; x < gridWidth; x++)
                {
                    Tile t = GetTileAt(x, bombTile.y);
                    if (t != null && !tilesToDestroy.Contains(t))
                    {
                        tilesToDestroy.Add(t);
                    }
                }
            }
            else if (bombTile.bombDirection == Tile.BombDirection.Vertical)
            {
                Debug.Log($"Activating Vertical Bomb at ({bombTile.x},{bombTile.y})");
                for (int y = 0; y < gridHeight; y++)
                {
                    Tile t = GetTileAt(bombTile.x, y);
                    if (t != null && !tilesToDestroy.Contains(t))
                    {
                        tilesToDestroy.Add(t);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"DirectionalBomb at ({bombTile.x},{bombTile.y}) has no direction set!");
                 // Fallback: Destroy just the bomb or do nothing further before clearing state
                DestroySingleTile(bombTile, false); // Destroy the misconfigured bomb
                ClearSelectionsAndState();
                return;
            }

            // Placeholder for particle effects for row/column clear
            await Task.Delay(300); // Animation delay for effect

            await DestroySpecificTiles(tilesToDestroy);

            await HandleCascadeAndRefill();
            await CheckMatchesAndProcess();
            ClearSelectionsAndState();
        }

        private async Task DestroySpecificTiles(List<Tile> specificTiles)
        {
            foreach (Tile t in specificTiles)
            {
                if (t != null && GetTileAt(t.x, t.y) == t) // Check if it's still there
                {
                    DestroySingleTile(t, false); // Use existing single tile destruction, no individual cascade
                }
            }
            await Task.Delay(100); // Brief delay after group destruction
        }


        private async Task DropTiles()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++) // Start from bottom row, check for empty spots
                {
                    if (grid[x, y] == null)
                    {
                        for (int k = y + 1; k < gridHeight; k++) // Find next tile above in the same column
                        {
                            if (grid[x, k] != null)
                            {
                                Tile tileToDrop = grid[x, k];
                                grid[x, y] = tileToDrop;
                                grid[x, k] = null;

                                tileToDrop.y = y; // Update internal coordinate
                                // TODO: Animate tile movement from (x,k) to (x,y)
                                tileToDrop.transform.position = new Vector2(x, y); // Placeholder direct position update
                                break; // Found a tile to drop into this empty spot
                            }
                        }
                    }
                }
            }
            await Task.Yield(); // Allow a frame for visual updates if many tiles drop.
        }

        private async Task RefillGrid()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grid[x, y] == null)
                    {
                        CreateTile(x, y, false); // isInitialCreation = false
                        // The CreateTile method now handles placing it at the top and it will be part of the next DropTiles pass
            // or requires an animation here. For simplicity now, CreateTile places it visually at top, then DropTiles will handle it.
                    }
                }
            }
            await Task.Yield(); // Allow a frame for visual updates.
        }
    }
}
