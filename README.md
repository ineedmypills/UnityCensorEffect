# PPSv2 Censor Effect

A post-processing effect for Unity's Post Processing Stack v2 that pixelates or censors objects on specific layers.

## Features

*   **Layer-based Censorship:** Apply the effect only to objects on specified layers.
*   **Pixelation:** Censor objects with a customizable pixelation effect.
*   **Hard Edges:** Option to maintain a sharp, clean edge for the censored area, conforming to the object's silhouette.
*   **Occlusion Support:** The effect is correctly occluded by objects in the foreground.

## Installation

This package is a standalone UPM package. To install, use the Unity Package Manager:

1.  Open the Package Manager window (`Window > Package Manager`).
2.  Click the `+` button in the top-left corner and select "Add package from git URL...".
3.  Enter the repository URL where you have hosted this package.
4.  Click "Add".

Alternatively, you can add it directly to your project's `manifest.json` file:

```json
{
  "dependencies": {
    "com.ineedmypills.censor": "https://github.com/ineedmypills/UnityCensorEffect.git"
  }
}
```

## How to Use

1.  **Add a Post Process Volume:** Make sure you have a Post Process Volume component in your scene.
2.  **Create a Profile:** Create a new Post Process Profile or use an existing one.
3.  **Add the Effect:** In the Post Process Volume inspector, click "Add effect..." and select "Custom > Censor".
4.  **Configure the Effect:**
    *   **Censor Layer:** Select the layer(s) you want to apply the censorship effect to. Objects on this layer will be pixelated.
    *   **Pixel Size:** Adjust the size of the pixelation blocks.
    *   **Hard Edges:** Check this box if you want the censored area to be sharply cropped to the object's silhouette. Uncheck it for a softer, full-screen pixelation of the censored objects.

## Notes

*   This effect requires Unity's Post Processing Stack v2.
*   The `Censor Camera` GameObject is created automatically at runtime and should not be modified.
*   For the "Hard Edges" feature to work correctly, the objects on the `Censor Layer` should be opaque.
