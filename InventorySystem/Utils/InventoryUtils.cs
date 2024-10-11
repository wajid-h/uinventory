using UnityEngine ; 
using System;


namespace InventorySystem.Utils {

    [Serializable]
    public class  StorableItem<TItem>{
        [SerializeField] private  TItem  item ; 
        [SerializeField] private  int stackCount ; 
        public TItem  Item {get {return  item;} }  
        public int StackAmount  {get {return stackCount; }}
        public void AddStack(int stackAmount = 1){
            stackCount ++  ; 
        }
        public void RemoveStack(int amountToRemove){
            if (amountToRemove <=   stackCount)
                stackCount -= amountToRemove;
            else 
            {       
                string item_info = (item as Item ) != null?  (item as Item).name  : "item" ; 
                Debug.LogWarning($"Trying to remove more stack of {item_info}  than total stack ");
                return ;
            }

        }
        public bool RemoveStackAndCheckZero(int amount__){
            RemoveStack(amount__);  
            return  stackCount ==  0 ; 
        }
        public StorableItem(TItem item_ ,  int amount__ ){
            item = item_ ; 
            stackCount =  amount__ ; 
        }
    }   

    [Serializable]
    public struct  InventorySettings {

        public readonly int InventorySize {get {return  inventoryCapacity ; }}
        
        public readonly bool CountStack  {get {return  countStack; }}
        
        public readonly int MaxStackSize {get {return  maxStackSize; }}

        [SerializeField] private int inventoryCapacity  ; 

        [Tooltip("When true , Capacity of inventory is counted using amount of stack  of each item, when false capacity is counted by number of unique items in inventory")]
        [SerializeField] private bool countStack   ;  

        [SerializeField] private int maxStackSize; 
    }



    public enum  StorageType {
        Inventory , 
        Reserve

    }
}