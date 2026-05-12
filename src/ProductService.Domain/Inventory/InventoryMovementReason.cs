namespace ProductService.Domain.Inventory;

/// <summary>
/// Why an inventory movement happened. Stored on every row in the
/// InventoryMovement table so we can answer "where did our stock go?"
/// long after the fact.
///
/// Affects either OnHand, Reserved, or both. See InventoryItem.Apply()
/// for the exact effect of each value.
/// </summary>
public enum InventoryMovementReason
{
    /// <summary>Stock physically received from a supplier or transfer.</summary>
    Received = 1,

    /// <summary>Customer added items to cart / order placed. Increases Reserved, no change to OnHand.</summary>
    Reserved = 2,

    /// <summary>Cart abandoned, order cancelled before fulfillment, or reservation expired. Decreases Reserved.</summary>
    Released = 3,

    /// <summary>Order fulfilled: stock leaves the warehouse. Decreases OnHand AND Reserved together.</summary>
    Sold = 4,

    /// <summary>Customer returned a delivered item in resellable condition. Increases OnHand.</summary>
    Returned = 5,

    /// <summary>Customer filed a chargeback and the item physically came back. Increases OnHand.</summary>
    ReturnedFromChargeback = 6,

    /// <summary>Customer filed a chargeback but kept the item (friendly fraud or lost-in-transit). Decreases OnHand without a paired Reserved decrement.</summary>
    ChargebackLoss = 7,

    /// <summary>Stock damaged, expired, or otherwise unsellable. Decreases OnHand.</summary>
    WriteOff = 8,

    /// <summary>Physical count adjustment - OnHand is set to a new absolute value. Quantity stored is the absolute new OnHand, not a delta.</summary>
    Recount = 9,

    /// <summary>Manual positive correction (found extra stock, data entry fix, etc.). Increases OnHand.</summary>
    ManualAdjustmentIn = 10,

    /// <summary>Manual negative correction (theft, miscounted, data entry fix, etc.). Decreases OnHand.</summary>
    ManualAdjustmentOut = 11,
}
