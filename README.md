# Censor Effect for Unity

A simple and lightweight camera post-processing effect to censor objects on a specific layer with a pixelated shader.

![Sample](https://github.com/user-attachments/assets/479ff24e-876b-4243-9fb5-2cf481f04a9c)

## How to Install

### Recommended: Unity Package Manager (for Unity 2019.4 or newer)
1. In the Unity Editor, go to `Window` > `Package Manager`.
2. Click the `+` button in the top-left corner and select `Add package from git URL...`.
3. Enter the following URL and click `Add`:


## How to Use
1.  **Add the Censor Effect Component:**
    *   Select the `Camera` GameObject in your scene.
    *   In the Inspector, click `Add Component` and search for `CensorEffect`. Add it to the camera.

2.  **Create a Censor Layer:**
    *   Go to `Edit` > `Project Settings` > `Tags and Layers`.
    *   Under `Layers`, add a new layer (e.g., "Censored").

3.  **Assign Objects to the Layer:**
    *   Select the GameObjects you want to censor.
    *   In the Inspector, change their `Layer` to the "Censored" layer you just created.

4.  **Configure the Censor Effect:**
    *   Select your `Camera` GameObject again.
    *   In the `CensorEffect` component in the Inspector, set the `Censor Layer` property to the "Censored" layer.
