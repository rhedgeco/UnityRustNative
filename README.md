# Unity Rust Native

A system for compiling native rust plugins and hot reloading in the Unity Engine.

## Installation

The RustNative plugin needs to be installed in your Unity project for it to work.

If you are starting a new project, create one or open an existing project you wish to work with.

As demonstrated below, in the `Project` pane, right click on `Assets` and navigate to the `Import Package` submenu and choose `Custom Package...`.

![](https://github.com/nerdo/UnityRustNative/assets/1031502/45a5437b-db1a-4785-b9dc-05215f22c77b)

## Setup

Once RustNative is installed, navigate to the `Window` menu and choose `Rust Native Manager`. This will open a window with settings.

First, set the location of `cargo`. Then, click `New Project`, give it a name, and click `Create Project`. This will scaffold a Rust project for you.

Click `Rebuild Bindings` to have it build the project and create the bindings that allow it to be used from Unity scripts.

![](https://github.com/nerdo/UnityRustNative/assets/1031502/cf5d1545-a7b4-42fe-9c55-153d908e9fb9)

## Usage

You should have a Rust project inside the `Assets/RustNative` folder with the name you provided. The project is a simple Rust library that exposes a single function, `add_one`, which takes a `u32` as input and returns 1 + your input.

As a simple test, ensure that you can access this code from Unity before moving on to write your own.

Create or edit a Unity script. At the top, add `using RustNative;` to import the namespace into the script.

In a convenient place (like `Start()`), add `Debug.Log(TestProject.add_one(1));` where `TestProject` is the name you gave your Rust project.

Play your game and open the `Console` and you should see `2` get logged.

Now you can proceed to add your own code. Refer to the [interoptopus](https://docs.rs/interoptopus/latest/interoptopus/) documentaiton for details on how to expose Rust code to Unity.
