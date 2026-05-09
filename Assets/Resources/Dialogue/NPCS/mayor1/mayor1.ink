=== mayor1 ===
{ dialogue_state:
    - 1: 
        Hello. I am this Village's mayor. You must be the guy who come from distant lands to our village.
        Folk told me that you brought us some delicious food called "Doner".
        I should taste it. Bring me one. If I like it, perhaps I let you serve your food here.
        // TODO
        So this is what they call Doner...
        This is...
        This is. Astonishing.
        Okay. 
        
    - 2: 
    
    - else: 
        Error occured. Check gw1 ink.
}

-> END      - This is FANTASTIC!
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