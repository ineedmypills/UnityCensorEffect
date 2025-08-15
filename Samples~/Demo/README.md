# Censor Effect Demo

This folder contains a demo scene showing how to use the Censor Effect.

## How to use

1.  Import the sample into your `Assets` folder using the Package Manager window.
2.  Open the `Demo` scene.
3.  The `Main Camera` in the scene has the `Censor Effect` component on it.
4.  The `CensorLayer` property is set to the `CensorMe` layer.
5.  The red cube in the scene is on the `CensorMe` layer, so it will be pixelated by the effect.
6.  Press Play to see the effect in action. You can modify the properties on the `Censor Effect` component to see how they change the result.

*Note: Due to environment limitations, the `.unity` scene file and materials could not be generated automatically. You will need to create a simple scene with a camera and a cube, add the `CensorEffect` to the camera, and set the cube's layer to a new layer that you assign to the `CensorLayer` property on the effect.*
