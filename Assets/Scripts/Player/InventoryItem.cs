using UnityEngine;

namespace Player
{
    /// <summary>
    /// Data record for a single item stored in the player's inventory.
    /// Holds a reference to the original prefab/object and display metadata.
    /// </summary>
    [System.Serializable]
    public sealed class InventoryItem
    {
        /// <summary>Display name shown in the inventory UI.</summary>
        public string displayName;

        /// <summary>Optional icon sprite displayed in the inventory slot.</summary>
        public Sprite icon;

        /// <summary>Whether this item is drinkable (can be consumed from inventory).</summary>
        public bool isDrinkable;

        /// <summary>Reference to the Pickupable component of the stored item (may be null if consumed).</summary>
        public Pickupable sourcePickupable;

        public InventoryItem(string displayName, Sprite icon, bool isDrinkable, Pickupable sourcePickupable)
        {
            this.displayName = displayName;
            this.icon = icon;
            this.isDrinkable = isDrinkable;
            this.sourcePickupable = sourcePickupable;
        }
    }
}
