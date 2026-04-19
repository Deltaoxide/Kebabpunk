=== gw_1 ===
{shuffle:
    -   Hello, What a lovely day.!
    -   Greetings! It smells so good.
    -   Hey, it's me again.
    -   Hi! I was passing by and smelled those kebabs. It worths waiting the queue.
}
{shuffle:
    -  I would like to order a doner kebab. I like Tomatoes and Lettuce.<>
        ~ NormalOrder((Tomato, Lettuce))
    -  I would like to order a doner kebab. I dont like Tomatoes.<>
        ~ ExcOrder((Tomato))
} 
~ waitForOrder() 
-> DONE

= order_check
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