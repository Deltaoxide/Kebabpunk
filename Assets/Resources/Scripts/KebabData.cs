using System.Collections.Generic;
using UnityEngine;

public static class KebabData
{
    private static readonly  Dictionary<ToppingType, int> DefaultKebabData = new() 
    {
        { ToppingType.Meat,0 },
        { ToppingType.Lettuce, 0 },
        { ToppingType.Tomato, 0 }
    };
    public static Dictionary<ToppingType, int> CreateNew() 
    {
        return new Dictionary<ToppingType, int>(DefaultKebabData);
    }
}
