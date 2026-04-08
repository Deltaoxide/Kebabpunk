LIST Toppings = Lettuce, Tomato
VAR order_state = 0


EXTERNAL normalOrder(orderList)
EXTERNAL excludedOrder(orderList)


EXTERNAL waitForOrder()

=== function NormalOrder(items) ===
    ~ normalOrder(items) // Calling the external Unity function
    ~ return        // Functions must end with 'return' or 'return [value]'
    
=== function ExcOrder(items) ===
    ~ excludedOrder(items) // Calling the external Unity function
    ~ return        // Functions must end with 'return' or 'return [value]'