using System;
using System.Collections.Generic;
using UnityEngine ; 
using InventorySystem.Utils;


public class Inventory : MonoBehaviour
{        



    #region  Events
    public event Action <List<StorableItem<Item>>>  OnInventoryChanged ; 
    public event Action <List<StorableItem<Item>>>  OnReservationsChanged ; 

    #endregion
    #region  Public Properties 

    ///<summary>
    /// Duh ? Actual inventory items main list. 
    ///</summary>
    public List<StorableItem<Item>> Items  {get {return  items ; }}
    /// <summary>
    /// Reserved items list is used to temprryly (wtf how is this shit spelt)
    /// store items that might be in use by other system , perhaps a crafting system
    /// which is going to reserve some items needed to craft something, if the process is cancelled mid progress, 
    /// these items will be safely  returned to normal inventory collection. 
    /// Keep in mind, once an item goes in reserved list, it cannot be used by other systems except the one that put the object in reserve.
    /// </summary>  
    public List<StorableItem<Item>> ReservedItems {get {return  reservedItems; }}
    #endregion
    #region  Private fields
    [SerializeField] private List<StorableItem<Item>>  items =  new();   
    [SerializeField] private List<StorableItem<Item>> reservedItems =  new(); 
    
    [SerializeField] private InventorySettings settings ;

    #endregion
    #region  Constants
    private const int  DEFAULT_STACK_CHANGE  = 1 ;
    
    #endregion
    #region  Inventory Management 

    /// <summary>
    /// Adds an item to inventory if there is space.
    /// Adds to  stack amount of item if  the item  is already present in inventory 
    /// otherwise or if the stack is full ,  creates a new instance of item in inventory
    /// </summary>
    /// <param name="newItem"></param>
    
    
    private void AddItem (Item  newItem , ref List<StorableItem<Item>> targetCollection , int stackAmount = DEFAULT_STACK_CHANGE) {
        
        // Fuck off already if no space is available
        if (GetRemainingSpace () <=    0  )  
        {
            Debug.LogWarning("No more space in inventory. ") ; 
            return; 
        } 

        StorableItem<Item>  availableItem =  FindStorableStacked(newItem ,targetCollection);
        // The query will not return in item entry if the stack is maxed 
        // hence asserting the behaviour here to HardAdd if query return value is null 
        if (availableItem ==  null ) {   
            StorableItem<Item>  newItemInstance = new (newItem , stackAmount); 
            targetCollection.Add(newItemInstance) ; 
        } 
        else
          availableItem.AddStack(stackAmount); 

    }
    private void RemoveItem (Item targetItem, ref List<StorableItem<Item>> targetCollection , int stackAmount ){

        StorableItem<Item> target  =  FindStorable(targetItem , targetCollection) ; 
       
        if (target == null )  return  ;  // You can't take away what you never gave me to begin with ~  Franz Kafka  (idk I didn't watch the movie)
        Debug.Log("Calling item to reduce  stack") ; 
        bool itemZeroed = target.RemoveStackAndCheckZero(stackAmount);
        if (itemZeroed)
            targetCollection.Remove(target) ;

    }   

    /* 
    
    To allow for both, abstract and specfic  functionality for adding and removing items
    we have these 2 helper methods (AddToInventory   & RemoveFromInventory)  and 2 internal and verbose (argument wise) 
    methods (AddItem & RemoveItem).
    
    Use helper functions which are more straightforward to use, but for a "low level" control
    over adding and removing items  from and to different parts of inventory ,use the normal RemoveItem  /  AddItem functions, which have     
    far more flexibility of use and direct control over target collection to work on

    
    */

    /// <summary>
    /// Add to inventory uses internal  'AddItem' method 
    /// to add item into inventory list with some preset params, use this when 
    /// adding to inventory from external sources. 
    /// </summary>
    /// <param name="itemToAdd"></param>
    public void AddToInventory(Item itemToAdd){
        AddItem(itemToAdd , ref items )  ;
        OnInventoryChanged?.Invoke(items) ; 
    }
    /// <summary>
    /// Removes given stack of target item from inventory, use externally.  
    /// </summary>
    /// <param name="itemToRemove">Target item to be removed</param>
    /// <param name="amountToRemove">Amount of stack to be removed</param>
    public void RemoveFromInventory(Item itemToRemove , int amountToRemove = DEFAULT_STACK_CHANGE){
        RemoveItem(itemToRemove , ref items ,  amountToRemove) ; 
        OnInventoryChanged?.Invoke(items) ; 
    }

    #endregion 
    #region  Reservation Management 

    // High level external use methods to reserve /  refund items
    public void ReserveItem(Item  item ,  int amount) {

        StorableItem<Item> targetItem   =  FindStorable(item ,  items);

        if(targetItem ==  null ||  amount > targetItem.StackAmount ){
            Debug.LogWarning("Reserving more than available stack is not allowed");
            return ;
        }   

        // Reserve 
        AddItem(targetItem.Item , ref reservedItems ,   amount ) ;  
        // Remove that amount from inventory
        RemoveItem(targetItem.Item , ref items ,  amount ) ; 
        
        OnReservationsChanged?.Invoke(reservedItems);
        OnInventoryChanged?.Invoke(items) ; 

    }

    private void RefundItem(Item  item  , int amount ){

        // Repeating myself until a better solution is found

        StorableItem<Item> targetItem   =  FindStorable(item ,  reservedItems);
        if(targetItem == null || amount  >  targetItem.StackAmount ){
            Debug.LogWarning("Can't refund null /  more than refundable amount ");
            return ;
        }
        AddItem(targetItem.Item,  ref items ,  amount) ;    
        RemoveItem(targetItem.Item , ref reservedItems ,  amount)  ;
        OnReservationsChanged?.Invoke(reservedItems);
        OnInventoryChanged?.Invoke(items) ; 
    }


    

    #endregion
    #region  Item Search functions

    // --> Find from a specific collection 

    // -- >  Find a collectable, irrespective of stack , using just an item instance matching 
    public StorableItem<Item>  FindStorable ( Item refrenceItem, List<StorableItem<Item>>  storage  ){

        List<StorableItem<Item>> searchContext   =  storage ;
        StorableItem<Item>  searchItem =  searchContext.Find(query =>  query.Item ==  refrenceItem  ) ; 
        return searchItem ; 

    }

    // -->  Find  a collectable, considering stack, only return if stack is not maxxed up 

    public StorableItem<Item>  FindStorableStacked ( Item refrenceItem, List<StorableItem<Item>>  storage  ){

        List<StorableItem<Item>> searchContext   =  storage ;
        StorableItem<Item>  searchItem =  searchContext.Find(query =>  query.Item ==  refrenceItem   && query.StackAmount !=  settings.MaxStackSize) ; 
        return searchItem  ; 

    }

    #endregion
    #region  Built In Utility 
    private int GetRemainingSpace(){

        int remainingSpace  = settings.InventorySize  ;    

        foreach (StorableItem<Item> item  in items){
            remainingSpace -=  settings.CountStack ? item.StackAmount :  1 ; 
        }
        return  remainingSpace  ; 
    }
    #endregion
    #region  Debugging
    public static Inventory fuckiingSingleton ;

    void Awake(){
        fuckiingSingleton =  this ;
    }


    [Header("---> debug")]


    public Item testItem1; 
    public Item testItem2 ;

    [Range( 0 , 1)]public int toAdd ;

    [ContextMenu("Test Add ")]
    public void AddDebug(){
        AddToInventory(toAdd == 1 ? testItem2  :  testItem1) ; 
    }
    [ContextMenu("Test Remove")]
    public void RemoveDebug(){
        Debug.Log("Removing from inspector")  ; 
        RemoveFromInventory(toAdd == 1 ? testItem2 :  testItem1) ; 
    }

    [ContextMenu("Test Reserve")]
    public void Reserve(){
        Debug.Log("Reserving from inspector")  ; 
        ReserveItem(toAdd == 1 ? testItem2 :  testItem1 , 1) ; 
    }

    [ContextMenu("Test Refund")]
    public void Refund(){
        Debug.Log("Refunding from inspector")  ; 
        RefundItem(toAdd == 1 ? testItem2 :  testItem1 , 1) ; 
    }

    #endregion
    #region  System Summary
    /*
        -- Stacking Items

        -- Reserving Item Stack
            -Reserving By Stack
            -Reserving By Refrence 

        -- Refunding 
            - Refund by stack
            - Refund by refrence  
            
        -- Adding / Removing by  refrence or stack 
            - Remove  3 of x y z
            - Add 2 x y z    
        -- Events
            - When Item is altered majorly  (removed , added)
            - Item stack is altered (reserved, refunded)
            ->  Casting events with changed data /   entire new data
    */
    #endregion

}   


