# Censor Effect for Unity

A simple, robust, and easy-to-use camera effect for censoring objects in the **Built-in Render Pipeline**.

This asset has been completely rewritten from the ground up for improved clarity and maintainability, while preserving the original's reliable, feature-rich functionality. It uses a temporary camera to render a mask of specified objects, providing guaranteed occlusion and compatibility.

![Sample](https://github.com/user-attachments/assets/479ff24e-876b-4243-9fb5-2cf481f04a9c)

## Features
- **Easy to use:** Add the `CensorEffect` component to a camera, select the layer to censor, and it works out of the box.
- **Reliable Occlusion:** Censored objects are correctly hidden by other geometry in the scene.
- **Configurable Appearance:**
    - Control the pixelation level with the `Pixel Block Count` slider.
    - Expand the censored area with the `Censor Area Expansion` slider.
    - Choose between soft, anti-aliased edges or sharp, **pixel-perfect** hard edges.
- **Automatic Setup:** Shaders are located automatically, so no manual linking is required.

## How to Install

### Unity PackageManager (Recommended)
1. In the Unity Editor, go to `Window` > `Package Manager`.
2. Click the `+` button in the top-left corner and select `Add package from git URL...`.
3. Enter the following URL and click `Add`:
   ```
   https://github.com/ineedmypills/CensorEffect.git
   ```
   *(Note: This requires Git to be installed on your system.)*

## How to Use

1.  **Create a Censor Layer:**
    *   Go to `Edit` > `Project Settings` > `Tags and Layers`.
    *   Under `Layers`, add a new layer (e.g., "Censored").

2.  **Assign Objects to the Layer:**
    *   Select the GameObjects you want to censor.
    *   In the Inspector, change their `Layer` to the "Censored" layer you just created.

3.  **Add the Censor Effect Component:**
    *   Select the `Camera` GameObject in your scene.
    *   In the Inspector, click `Add Component` and search for `CensorEffect`. Add it to the camera.

The effect is now active and can be configured in the Inspector.

## Configuration
-   **Censor Layer:** The layer containing the objects to be pixelated.
-   **Enable Occlusion:** If checked, censored objects will be hidden by other objects in front of them.
-   **Pixel Block Count:** The number of pixel blocks to draw across the screen's height. Smaller numbers mean larger, more abstract blocks.
-   **Censor Area Expansion:** How much to expand the censored area, useful for covering objects completely.
-   **Enable Anti-Aliasing:** Controls the style of the censorship border. When enabled, the edges are soft and anti-aliased. When disabled, the edges are sharp and snap perfectly to the pixelation grid.
