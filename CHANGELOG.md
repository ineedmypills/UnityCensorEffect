# Changelog
All notable changes to this package will be documented in this file.

## [3.0.0] - 2025-08-15
### Changed
- **Complete Architectural Refactor:** The effect has been entirely rewritten to use a modern, robust `CommandBuffer` pipeline. This replaces the legacy `OnRenderImage` implementation, resolving numerous bugs related to occlusion, lighting, and stability.
- The `CensorEffect` component now dynamically updates in the editor when properties are changed, allowing for live preview of all settings.

### Fixed
- **Occlusion:** Now 100% reliable and works as expected out of the box.
- **Lighting:** Resolved a critical bug where the effect would break scene lighting or turn the screen black.
- **Borders:** The non-anti-aliased border is now a clean, pixel-perfect grid.

### Added
- **Shader Auto-Search:** The script now automatically finds its required shaders, simplifying setup.
- **Error Checking:** Added safety checks for unsupported hardware.

## [2.0.1] - 2025-08-15
### Fixed
- Occlusion is now reliable and no longer dependent on project-wide MSAA settings. It now uses a manual depth test against the scene's depth texture.
- `CensorAreaExpansion` now functions correctly in all scenarios, including when Anti-Aliasing is enabled or disabled.
- A compile error caused by an incorrect API call for `msaaSamples` on `RenderTexture` has been resolved.

### Changed
- When `EnableAntiAliasing` is disabled, the censorship border is now a sharp, pixel-perfect grid, as originally intended. When enabled, it remains a smooth, anti-aliased edge.

## [2.0.0] - 2025-08-14
### Added
- **Universal Render Pipeline (URP) Support:** The effect now fully supports URP via a custom `ScriptableRendererFeature`.
- **Occlusion Support:** Added an `Enable Occlusion` option. When enabled, censored objects will be correctly hidden by other objects in the foreground.
- **Built-in Pipeline Renderer:** Created a dedicated renderer component for the Built-in pipeline to ensure compatibility.

### Changed
- **Project Structure:** Completely refactored the package to support multiple render pipelines using Assembly Definitions.
- **Expansion Effect:** Replaced the old, inefficient expansion algorithm with a performant two-pass blur, resulting in a smoother and more configurable effect.
- **Shader Code:** All shaders were rewritten from CG to HLSL for modern pipeline compatibility.
- **Component Workflow:** The main `CensorEffect` component is now a clean data container, with renderer-specific logic handled by separate components.

### Removed
- Removed the old `OnRenderImage` logic from the main component, which is now handled by the dedicated `CensorEffectBuiltin` script.

## [1.1.0] - 2025-08-14
### Added:
Physically-based expansion
CensorAreaExpansion now uses world units → auto-converts to screen pixels

### Changed:
* Pixel size calculation.
Now adapts to camera FOV/resolution

* Resource handling.
Simplified texture management (single ARGB texture)

## [1.0.0] - 2025-08-14
This is the first release of *Censor Effect* package.