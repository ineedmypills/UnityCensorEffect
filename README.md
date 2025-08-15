# Censor Effect for Unity

A versatile and performant camera effect to censor objects on a specific layer with a pixelated shader. Supports both the **Built-in Render Pipeline** and the **Universal Render Pipeline (URP)**.

![Sample](https://github.com/user-attachments/assets/479ff24e-876b-4243-9fb5-2cf481f04a9c)

## Features
- **Easy to use:** Add a component to your camera and you're ready to go.
- **Performant:** Uses a two-pass blur for efficient area expansion.
- **URP & Built-in RP:** Automatically adapts to your project's render pipeline.
- **Occlusion Culling:** Optional depth testing to correctly hide censored objects behind others.

## How to Install

### Recommended: Unity PackageManager (for Unity 2019.4 or newer)
1. In the Unity Editor, go to `Window` > `Package Manager`.
2. Click the `+` button in the top-left corner and select `Add package from git URL...`.
3. Enter the following URL and click `Add`:
   ```
   https://github.com/ineedmypills/CensorEffect.git
   ```

## How to Use

### 1. Basic Setup (for all pipelines)
1.  **Create a Censor Layer:**
    *   Go to `Edit` > `Project Settings` > `Tags and Layers`.
    *   Under `Layers`, add a new layer (e.g., "Censored").

2.  **Assign Objects to the Layer:**
    *   Select the GameObjects you want to censor.
    *   In the Inspector, change their `Layer` to the "Censored" layer you just created.

3.  **Add the Censor Effect Component:**
    *   Select the `Camera` GameObject in your scene.
    *   In the Inspector, click `Add Component` and search for `CensorEffect`. Add it to the camera.

### 2. Pipeline-Specific Setup

#### For Built-in Render Pipeline
No extra steps are needed! The effect will automatically detect the pipeline and configure itself.

#### For Universal Render Pipeline (URP)
1.  Select your **URP Renderer** asset (usually found in your project's `Settings` folder, assigned to your URP Asset).
2.  In the Inspector for the Renderer, click the **`Add Renderer Feature`** button.
3.  Select the **`Censor Feature`** from the list.

### 3. Configure the Censor Effect
-   Select your `Camera` GameObject.
-   In the `CensorEffect` component, you can now configure the following settings:
    -   **Censor Layer:** The layer containing the objects to be pixelated.
    -   **Enable Occlusion:** If checked, censored objects will be hidden by other objects in front of them. If unchecked, the effect will appear over everything (legacy behavior).
    -   **Pixel Block Count:** The number of pixel blocks to draw across the screen's height. Smaller numbers mean larger, more abstract blocks.
    -   **Censor Area Expansion:** How much to expand the censored area, useful for covering objects completely.
    -   **Enable Anti-Aliasing:** Toggles smoothing on the edges of the pixelated area.
