using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Manages the player's inventory item list.
    /// Exposes events so the UI can react to any data change without polling.
    /// Singleton — one instance lives on the Player GameObject.
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        /// <summary>Read-only view of the current inventory contents.</summary>
        public IReadOnlyList<InventoryItem> Items => _items;

        /// <summary>Fired whenever an item is added or removed.</summary>
        public event Action OnInventoryChanged;

        private readonly List<InventoryItem> _items = new ();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Adds an item to the inventory and fires <see cref="OnInventoryChanged"/>.</summary>
        public void AddItem(InventoryItem item)
        {
            if (item == null) return;
            _items.Add(item);
            OnInventoryChanged?.Invoke();
            Debug.Log($"[Inventory] Added '{item.displayName}'. Total: {_items.Count}");
        }

        /// <summary>Removes an item from the inventory and fires <see cref="OnInventoryChanged"/>.</summary>
        public bool RemoveItem(InventoryItem item)
        {
            bool removed = _items.Remove(item);
            if (removed)
            {
                OnInventoryChanged?.Invoke();
                Debug.Log($"[Inventory] Removed '{item.displayName}'. Total: {_items.Count}");
            }
            return removed;
        }

        /// <summary>Returns true if the inventory contains the given item.</summary>
        public bool Contains(InventoryItem item) => _items.Contains(item);
    }
}
