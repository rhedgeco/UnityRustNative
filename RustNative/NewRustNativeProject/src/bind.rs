use interoptopus::{function, Inventory, InventoryBuilder};

use crate::add_one;

pub fn build_binding_inventory() -> Inventory {
    InventoryBuilder::new()
        .register(function!(add_one))
        .inventory()
}
