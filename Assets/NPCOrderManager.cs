using System;
using System.Collections.Generic;
using Ink.Runtime;
using System.Linq;
using UnityEngine;

public class NPCOrderManager : MonoBehaviour
{
    [SerializeField] DialogBoxWrapper dialogBoxWrapper;
    public Dictionary<ToppingType,int> CurrentOrder {get; private set;}
    void Start()
    {
        CurrentOrder = KebabData.CreateNew();
    }
    public void CreateOrder(InkList toppingList)
    {
        
        CurrentOrder = KebabData.CreateNew();
        foreach (InkListItem item in toppingList.Keys)
        {
            string itemstring = item.itemName;
            if(Enum.TryParse<ToppingType>(itemstring, out var parse))
            {
                CurrentOrder[parse] = 1;
            }
            else
            {
                Debug.LogError("A Parse failed while getting Order from Ink file. Ink String:" + itemstring + " Current Dialog NPC: "+ dialogBoxWrapper.Dialogue_id);
            }
        }
        
        CurrentOrder[ToppingType.Meat] = 1;
        
    }
    public void CreateExcludedOrder(InkList toppingList)
    {
        CurrentOrder = KebabData.CreateNew();
        var _keys = new List<ToppingType>(CurrentOrder.Keys);
        foreach (ToppingType k in _keys)
        {
            CurrentOrder[k] = 1;
        }
        foreach (InkListItem item in toppingList.Keys)
        {
            string itemstring = item.itemName;
            if(Enum.TryParse<ToppingType>(itemstring, out var parse))
            {
                CurrentOrder[parse] = 0;
            }
            else
            {
                Debug.LogError("A Parse failed while getting Order from Ink file. Ink String:" + itemstring + " Current Dialog NPC: "+ dialogBoxWrapper.Dialogue_id);
            }
        }
    }
    public void DeliverOrder(Dictionary<ToppingType,int> kebabData)
    {
        bool areEqual = kebabData.Count == CurrentOrder.Count && !kebabData.Except(CurrentOrder).Any();
        
        Debug.Log("kebabData");
        foreach (KeyValuePair<ToppingType,int> item in kebabData)
        {
            Debug.Log(item.Key + " - " + item.Value);
        }
        Debug.Log("CurrentOrder");
        foreach (KeyValuePair<ToppingType,int> item in CurrentOrder)
        {
            Debug.Log(item.Key + " - " + item.Value);
        }
        int order_success_value;
        if (areEqual)
        {
            order_success_value = 1;
        }
        else
        {
            order_success_value = 2;
        }
        CurrentOrder = KebabData.CreateNew();
        dialogBoxWrapper.DeliverOrder(order_success_value);
        
    }
}
