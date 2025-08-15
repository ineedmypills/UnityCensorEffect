# Censor Effect - Built-in RP Sample

This directory is a placeholder for a sample scene demonstrating the Censor Effect in the Built-in Render Pipeline.

## How to Create the Sample Scene

1.  **Create a New Scene:** Create a new scene and save it in this directory (`Samples~/BuiltIn-Sample`).
2.  **Add Objects:**
    *   Create a Cube or any other 3D object that will be censored.
    *   Create another Cube or 3D object that will stand in front of the first one to test occlusion.
3.  **Create a "Censored" Layer:**
    *   Go to `Edit -> Project Settings -> Tags and Layers`.
    *   Add a new User Layer named "Censored".
4.  **Assign Layer:**
    *   Select the first object (the one to be censored) and assign it to the "Censored" layer.
5.  **Add Censor Effect to Camera:**
    *   Select your `Main Camera`.
    *   Click `Add Component` and add the `Censor Effect` script.
    *   In the `Censor Effect` component, set the `Censor Layer` to "Censored".
6.  **Run the Scene:** Run the scene to see the effect in action. You should see the first object pixelated, and it should be correctly hidden behind the second object.

By following these steps, you can create a working sample scene to test the Censor Effect asset.
