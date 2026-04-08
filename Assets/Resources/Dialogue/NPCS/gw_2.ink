=== gw_2 ===
{shuffle:
    -   Hi. Another boring day.
        The carriage got plundered by those goblins. 
    -   Uhhh. Smells good, what is that?
    -   Hey, it was a bad day.
        The carriage got plundered by those goblins. 
}
{shuffle:
    -  A doner. Please be fast, I am starving. Add Lettuce.<>
        ~ NormalOrder((Lettuce))
    -  Make me a doner. Any topping is cool but Tomatoes, ewww.<>
        ~ ExcOrder((Tomato))
} 
~ waitForOrder()
-> DONE

=order_check
{ order_state:
    - 1: 
        {shuffle:
            - This is FANTASTIC!
            - Just as how it smells. Nom nom.
        }
        
    - 2: 
        {shuffle:
            - Eh it's not what I asked. Hope next time you do it better.
            - Smells good at least... Not how I wanted though, sorry.
        }
        
    - else: 
        Error occured. Check gw1 ink.
}
-> END