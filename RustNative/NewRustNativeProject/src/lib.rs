pub mod bind;

use interoptopus::ffi_function;

#[ffi_function]
#[no_mangle]
pub extern "C" fn add_one(x: u32) -> u32 {
    x + 1
}
