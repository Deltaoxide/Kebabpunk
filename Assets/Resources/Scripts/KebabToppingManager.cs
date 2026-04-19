using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class KebabToppingManager : MonoBehaviour,IDropHandler
{
    [SerializeField] GameObject Topping_Meat;
    [SerializeField] GameObject Topping_Tomato;
    [SerializeField] GameObject Topping_Lettuce;

    public Dictionary<ToppingType, int> Data {get; private set;}
    
    Dictionary<ToppingType, GameObject> topping_sprite_pair;

    

    void Start()
    {
        topping_sprite_pair = new Dictionary<ToppingType, GameObject>()
        {
            { ToppingType.Meat, Topping_Meat },
            { ToppingType.Lettuce, Topping_Lettuce },
            { ToppingType.Tomato, Topping_Tomato }
        };

        Data = KebabData.CreateNew();
    }

    public void OnDrop(PointerEventData eventData)
    {
        ToppingBoxManager droppedObject = eventData.pointerDrag.GetComponent<ToppingBoxManager>();
        if(droppedObject != null)
        {
            ToppingType objectToppingType = droppedObject.ToppingType;
        
            switch (objectToppingType)
            {
                case ToppingType.Meat:
                    Data[ToppingType.Meat] = 1;
                    topping_sprite_pair[ToppingType.Meat].SetActive(true);
                    break;

                case ToppingType.Lettuce:
                    Data[ToppingType.Lettuce] = 1;
                    topping_sprite_pair[ToppingType.Lettuce].SetActive(true);
                    break;

                case ToppingType.Tomato:
                    Data[ToppingType.Tomato] = 1;
                    topping_sprite_pair[ToppingType.Tomato].SetActive(true);
                    break;
            }
        }
        
        UpdateToppingSprites();
    }

    void UpdateToppingSprites()
    {
        foreach (KeyValuePair<ToppingType, int> entry in Data)
        {
            if (entry.Value != 0)
            {
                topping_sprite_pair[entry.Key].SetActive(true);
            }
            else
            {
                topping_sprite_pair[entry.Key].SetActive(false);
                
            }
        }
    }

}
