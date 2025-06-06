using UnityEngine;

namespace MatchAndEarn.Gameplay
{
    public class Tile : MonoBehaviour
    {
        public enum TileType
        {
            Red,
            Green,
            Blue,
            Yellow,
            Purple,
            Orange,
            // Add more types if needed, e.g., special bomb types
            Empty // Represents an empty or cleared tile space
        }

        public enum SpecialType
        {
            None,
            Dart,
            DirectionalBomb,
            AssimilationBomb
        }

        public enum BombDirection
        {
            None,
            Horizontal,
            Vertical
        }

        public TileType type;
        public SpecialType specialType = SpecialType.None;
        public BombDirection bombDirection = BombDirection.None;
        public int x, y; // Grid coordinates
        public GridManager gridManager; // Reference to the GridManager

        public SpriteRenderer spriteRenderer;
        private Sprite regularSprite; // Store the original sprite for this tile's type

        // Initialization method
        public void Initialize(TileType type, int x, int y, Sprite initialSprite, GridManager manager)
        {
            this.type = type;
            this.x = x;
            this.y = y;
            this.gridManager = manager;
            this.specialType = SpecialType.None; // Default to no special type
            this.bombDirection = BombDirection.None; // Default bomb direction
            this.regularSprite = initialSprite; // Store the regular sprite

            // Ensure spriteRenderer is assigned.
            // It might be assigned in the editor or found dynamically.
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    // If still null, add one. This is a fallback.
                    Debug.LogWarning($"Tile ({x},{y}) had no SpriteRenderer, adding one.");
                    spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
            }
            else
            {
                Debug.LogError($"Tile ({x},{y}) could not find or create a SpriteRenderer.");
            }

            // Set initial sprite
            UpdateSpriteBasedOnState();
        }

        // Example method to change visual state, e.g., when selected
        public void SetSelected(bool isSelected)
        {
            if (spriteRenderer != null)
            {
                // Example: change color or scale when selected
                spriteRenderer.color = isSelected ? Color.gray : Color.white;
            }
        }

        // Updates the sprite based on current type and specialType
        public void UpdateSpriteBasedOnState()
        {
            if (spriteRenderer == null) return;

            if (type == TileType.Empty)
            {
                spriteRenderer.enabled = false;
                return;
            }
            spriteRenderer.enabled = true;

            switch (specialType)
            {
                case SpecialType.Dart:
                    if (gridManager != null && gridManager.dartSprite != null)
                    {
                        spriteRenderer.sprite = gridManager.dartSprite;
                    }
                    else
                    {
                        // Fallback if dart sprite is missing, use regular or a placeholder color
                        spriteRenderer.sprite = regularSprite;
                        Debug.LogWarning($"Dart sprite missing for tile {x},{y}. Falling back to regular sprite.");
                    }
                    break;
                case SpecialType.DirectionalBomb:
                    if (gridManager != null)
                    {
                        if (bombDirection == BombDirection.Horizontal && gridManager.horizontalBombSprite != null)
                        {
                            spriteRenderer.sprite = gridManager.horizontalBombSprite;
                        }
                        else if (bombDirection == BombDirection.Vertical && gridManager.verticalBombSprite != null)
                        {
                            spriteRenderer.sprite = gridManager.verticalBombSprite;
                        }
                        else
                        {
                            spriteRenderer.sprite = regularSprite; // Fallback
                            Debug.LogWarning($"DirectionalBomb sprite missing for tile {x},{y} with direction {bombDirection}. Falling back to regular sprite.");
                        }
                    }
                    else
                    {
                        spriteRenderer.sprite = regularSprite; // Fallback
                    }
                    break;
                case SpecialType.AssimilationBomb:
                    if (gridManager != null && gridManager.assimilationBombSprite != null)
                    {
                        spriteRenderer.sprite = gridManager.assimilationBombSprite;
                    }
                    else
                    {
                        spriteRenderer.sprite = regularSprite; // Fallback
                        Debug.LogWarning($"AssimilationBomb sprite missing for tile {x},{y}. Falling back to regular sprite.");
                    }
                    break;
                case SpecialType.None:
                default:
                    spriteRenderer.sprite = regularSprite;
                    break;
            }
        }

        // Call this if the type changes and it needs a new regular sprite
        public void SetRegularSprite(Sprite newRegularSprite)
        {
            this.regularSprite = newRegularSprite;
            UpdateSpriteBasedOnState(); // Refresh visuals
        }


        // Called when the user clicks on this tile's collider
        void OnMouseDown()
        {
            if (gridManager == null || type == TileType.Empty || gridManager.isProcessingMove) // Prevent interaction during processing
            {
                return;
            }

            if (specialType == SpecialType.Dart)
            {
                gridManager.ActivateDartTile(this);
            }
            else if (specialType == SpecialType.DirectionalBomb)
            {
                gridManager.ActivateDirectionalBomb(this);
            }
            else if (specialType == SpecialType.AssimilationBomb)
            {
                gridManager.ActivateAssimilationBomb(this);
            }
            else
            {
                gridManager.SelectTile(this);
            }
        }
    }
}
