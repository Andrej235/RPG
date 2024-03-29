using Assets.Code.Items.Interfaces;
using Assets.Code.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Playables;

namespace Assets.Code.Inventory
{
#nullable enable
    public class Storage : IEnumerable<StorageSlot>
    {
        public event EventHandler<OnSlotChangedEventArgs>? OnSlotChanged;
        public class OnSlotChangedEventArgs : EventArgs
        {
            public OnSlotChangedEventArgs(int slotIndex, int amount, IItem? item)
            {
                SlotIndex = slotIndex;
                Amount = amount;
                Item = item;
            }

            public int SlotIndex { get; set; }
            public int Amount { get; set; }
            public IItem? Item { get; set; }
        }



        private readonly StorageSlot[] storage;

        public Storage(int capacity) => storage = new StorageSlot[capacity];

        public IEnumerator<StorageSlot> GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => storage.GetEnumerator();

        /// <summary>
        /// Adds a given item to the storage
        /// <br />if the given item already exists in storage or if amount exceeds it's maximum stack size it will be distributed across multiple storage slots
        /// </summary>
        /// <returns>Number of items added to the inventory</returns>
        public int Add(IItem item, int amount)
        {
            if (amount <= 0)
                return 0;

            int addedItems = 0;
            StorageSlot? slot;
            if (storage.Any(x => x.Item == item && x.Amount < x.Item.MaxStack))
            {
                var slotsWithItem = storage.Where(x => x.Item == item && x.Amount < x.Item.MaxStack);

                foreach (var slotWithItem in slotsWithItem)
                {
                    if (amount <= 0)
                        break;

                    if (amount > item.MaxStack - slotWithItem.Amount)
                    {
                        var available = item.MaxStack - slotWithItem.Amount;
                        slotWithItem.Amount = item.MaxStack;
                        addedItems += available;
                        amount -= available;
                    }
                    else
                    {
                        slotWithItem.Amount += amount;
                        addedItems += amount;
                        amount = 0;
                    }
                    OnSlotChanged?.Invoke(this, new(storage.GetIndexOf(slotWithItem), slotWithItem.Amount, slotWithItem.Item));
                }
            }

            while (amount > 0)
            {
                slot = storage.FirstOrDefault(x => x.Id == -1);
                if (slot is null)
                    break;

                slot.Item = item;
                if (amount > item.MaxStack)
                {
                    slot.Amount = item.MaxStack;
                    addedItems += item.MaxStack;
                    amount -= item.MaxStack;
                }
                else
                {
                    slot.Amount = amount;
                    addedItems += amount;
                    amount = 0;
                }
                OnSlotChanged?.Invoke(this, new(storage.GetIndexOf(slot), slot.Amount, slot.Item));
            }

            return addedItems;
        }

        /// <summary>
        /// Increases amount of items inside a slot with the given index
        /// </summary>
        /// <returns>Number of items added, -1 if index was outside of bounds or item in the slot is null</returns>
        public int AddToSlot(int index, int amount)
        {
            if (index < 0 || index >= storage.Length)
                return -1;

            var slot = storage[index];
            if (slot.Item == null)
                return -1;

            int itemsAdded;
            if (amount > slot.Item.MaxStack - slot.Amount)
            {
                itemsAdded = slot.Item.MaxStack - slot.Amount;
                slot.Amount = slot.Item.MaxStack;
            }
            else
            {
                slot.Amount += amount;
                itemsAdded = amount;
            }
            OnSlotChanged?.Invoke(this, new(storage.GetIndexOf(slot), slot.Amount, slot.Item));
            return itemsAdded;
        }

        /// <summary>
        /// Increases amount of items inside a slot with the given index
        /// <br />If the slot doesn't have an item already, it will be set to the given item/>
        /// </summary>
        /// <returns>Number of items added, -1 if index was outside of bounds or item in the slot is not the same as provided item</returns>
        public int AddToSlot(IItem item, int index, int amount)
        {
            if (index < 0 || index >= storage.Length)
                return -1;

            var slot = storage[index];
            if (slot.Item == item)
                return -1;

            slot.Item ??= item;

            int itemsAdded;
            if (amount > slot.Item.MaxStack - slot.Amount)
            {
                itemsAdded = slot.Item.MaxStack - slot.Amount;
                slot.Amount = slot.Item.MaxStack;
            }
            else
            {
                slot.Amount += amount;
                itemsAdded = amount;
            }
            OnSlotChanged?.Invoke(this, new(storage.GetIndexOf(slot), slot.Amount, slot.Item));
            return itemsAdded;
        }

        /// <summary>
        /// Finds a given item in storage and subtracts amount from it
        /// </summary>
        /// <returns>Number of items taken, -1 if Item was not found</returns>
        public int Take(IItem item, int amount)
        {
            if (!Contains(item))
                return -1;

            int takenItems = 0;
            var slotsContainingItem = storage.Where(x => x.Item == item);

            foreach (var slotWithItem in slotsContainingItem)
            {
                if (amount <= 0)
                    break;

                if (amount >= slotWithItem.Amount)
                {
                    var available = amount - slotWithItem.Amount;
                    slotWithItem.Item = null;
                    slotWithItem.Amount = 0;
                    takenItems += available;
                    amount -= available;
                }
                else
                {
                    slotWithItem.Amount -= amount;
                    takenItems += amount;
                    amount = 0;
                }
                OnSlotChanged?.Invoke(this, new(storage.GetIndexOf(slotWithItem), slotWithItem.Amount, slotWithItem.Item));
            }

            return takenItems;
        }

        /// <summary>
        /// Removes all items from storage and returns a list of pairs (item, amount)
        /// </summary>
        /// <returns>A list of pairs (item, amount) where item represents the item that has been taken out of storage and amount represents the amount of an item which was taken out of storage</returns>
        public List<(IItem item, int amount)> TakeAll()
        {
            List<(IItem item, int amount)> result = new();
            foreach (var slot in storage.Where(x => x != null))
                result.Add(new(slot.Item!, slot.Amount));

            Clear();
            return result;
        }

        /// <summary>
        /// Decreases amount of items inside a slot with the given index
        /// </summary>
        /// <returns>Number of items taken, -1 if index was outside of bounds or item in the slot is null</returns>
        public int TakeFromSlot(int index, int amount = int.MaxValue)
        {
            if (index < 0 || index >= storage.Length)
                return -1;

            var slot = storage[index];
            if (slot.Item == null)
                return -1;


            int itemsTaken;
            if (amount >= slot.Amount)
            {
                itemsTaken = slot.Amount;
                slot.Amount = 0;
                slot.Item = null;
            }
            else
            {
                itemsTaken = amount;
                slot.Amount -= amount;
            }

            OnSlotChanged?.Invoke(this, new(storage.GetIndexOf(slot), slot.Amount, slot.Item));
            return itemsTaken;
        }

        /// <summary>
        /// Sets all items inside storage to null and amounts to 0
        /// </summary>
        public void Clear()
        {
            foreach (var slot in storage)
            {
                slot.Item = null;
                slot.Amount = 0;
                OnSlotChanged?.Invoke(this, new(storage.GetIndexOf(slot), slot.Amount, slot.Item));
            }
        }

        /// <summary>
        /// Searches for a given item in storage, if the appropriate amount of item is found returns true
        /// </summary>
        /// <returns>True if storage contains the appropriate amount of a given item</returns>
        public bool Contains(IItem item, int amount = 1) => storage.Where(x => x.Item == item).Sum(x => x.Amount) >= amount;

        /// <summary>
        /// Swaps the item inside of a slot with the given index with the provided newItem and newAmount
        /// </summary>
        /// <returns>
        /// Tuple containing the old item and it's amount
        /// <br />If the index was ouside of bounds, returns (null, -1)
        /// </returns>
        public (IItem? item, int amount) Swap(int index, IItem newItem, int newAmount)
        {
            if (index < 0 || index >= storage.Length)
                return new(null, -1);

            var slot = storage[index];
            (IItem? item, int amount) oldPair = new(slot.Item, slot.Amount);

            slot.Item = newItem;
            slot.Amount = newAmount;
            OnSlotChanged?.Invoke(this, new(index, slot.Amount, slot.Item));
            return oldPair;
        }

        /// <summary>
        /// Swaps values of 2 slots with the given indices
        /// </summary>
        public void Swap(int index1, int index2)
        {
            if (index1 < 0 || index1 >= storage.Length || index2 < 0 || index2 >= storage.Length)
                return;

            (storage[index1], storage[index2]) = (storage[index2], storage[index1]);

            var slot1 = storage[index1];
            var slot2 = storage[index2];
            OnSlotChanged?.Invoke(this, new(index1, slot1.Amount, slot1.Item));
            OnSlotChanged?.Invoke(this, new(index2, slot2.Amount, slot2.Item));
        }

        /// <summary>
        /// Tries to equip an item which is inside a slot with a given index
        /// </summary>
        /// <returns>False if index was out of bounds, item was not an IEquipable or there is no space in <see cref="InventoryManager.Inventory"/> to equip the given item</returns>
        public bool TryEquip(int index)
        {
            if (index < 0 || index >= storage.Length)
                return false;

            var slot = storage[index];
            if (slot.Item is not IEquipable)
                return false;

            bool success = slot.Item switch
            {
                IWeapon weapon => InventoryManager.EquipWeapon(weapon),
                IAbility ability => InventoryManager.EquipAbility(ability),
                IAccessory accessory => InventoryManager.EquipAccessory(accessory),
                _ => false,
            };

            if (!success)
                return false;

            TakeFromSlot(index);
            return true;
        }

        /// <summary>
        /// Tries to equip an item which is inside a slot with a given index
        /// <br />If there are no free slots corresponding to it's type, it will first unequip an already equipped item
        /// </summary>
        /// <returns>Item it unequipped, null if there was a free slot, index was out of bounds or item was not an IEquipable</returns>
        public IEquipable? EquipAgressive(int index)
        {
            if (index < 0 || index >= storage.Length)
                return null;

            var slot = storage[index];
            if (slot.Item is not IEquipable)
                return null;

            IEquipable? unequipedEquipable = null;

            switch (slot.Item)
            {
                case IWeapon weapon:
                    unequipedEquipable = InventoryManager.UnequipWeapon();
                    InventoryManager.EquipWeapon(weapon);
                    break;

                case IAbility ability:
                    //In case there is an empty ability slot just equip the item
                    if (InventoryManager.EquipAbility(ability))
                        break;

                    unequipedEquipable = InventoryManager.UnequipAbility();
                    InventoryManager.EquipAbility(ability);
                    break;

                case IAccessory accessory:
                    //In case there is an empty accessory slot just equip the item
                    if (InventoryManager.EquipAccessory(accessory))
                        break;

                    unequipedEquipable = InventoryManager.UnequipAccessory();
                    InventoryManager.EquipAccessory(accessory);
                    break;

                default:
                    break;
            }

            TakeFromSlot(index);
            return unequipedEquipable;
        }
    }
}
