# Unity Engine — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | Unity 6.3 LTS |
| **Release Date** | December 2025 |
| **Project Pinned** | 2026-03-25 |
| **Last Docs Verified** | 2026-03-25 |
| **LLM Knowledge Cutoff** | May 2025 |

## Knowledge Gap Warning

The LLM's training data likely covers Unity up to ~2022 LTS (2022.3). The entire
Unity 6 release series (formerly Unity 2023 Tech Stream) introduced significant
changes that the model does NOT know about. Always cross-reference this directory
before suggesting Unity API calls.

## Post-Cutoff Version Timeline

| Version | Release | Risk Level | Key Theme |
|---------|---------|------------|-----------|
| 6.0 | Oct 2024 | HIGH | Unity 6 rebrand, new rendering features, Entities 1.3, DOTS improvements |
| 6.1 | Nov 2024 | MEDIUM | Bug fixes, stability improvements |
| 6.2 | Dec 2024 | MEDIUM | Performance optimizations, new input system improvements |
| 6.3 LTS | Dec 2025 | HIGH | First LTS since 6.0, production-ready DOTS, enhanced graphics features |

## Major Changes from 2022 LTS to Unity 6.3 LTS

### Breaking Changes
- **Entities/DOTS**: Major API overhaul in Entities 1.0+, complete redesign of ECS patterns
- **Input System**: Legacy Input Manager deprecated, new Input System is default
- **Rendering**: URP/HDRP significant upgrades, SRP Batcher improvements
- **Addressables**: Asset management workflow changes
- **Scripting**: C# 9 support, new API patterns

### New Features (Post-Cutoff)
- **DOTS**: Production-ready Entity Component System (Entities 1.3+)
- **Graphics**: Enhanced URP/HDRP pipelines, GPU Resident Drawer
- **Multiplayer**: Netcode for GameObjects improvements
- **UI Toolkit**: Production-ready for runtime UI (replaces UGUI for new projects)
- **Async Asset Loading**: Improved Addressables performance
- **Web**: WebGPU support

### Deprecated Systems
- **Legacy Input Manager**: Use new Input System package
- **Legacy Particle System**: Use Visual Effect Graph
- **UGUI**: Still supported, but UI Toolkit recommended for new projects
- **Old ECS (GameObjectEntity)**: Replaced by modern DOTS/Entities

## Unity 6.3 LTS Highlights

- **2D+3D Mixed Rendering**: URP 2D Renderer now supports Mesh Renderer and
  Skinned Mesh Renderer alongside 2D sprites in the same scene.
- **Box2D v3 API**: New low-level 2D physics API running alongside existing API.
- **Accessibility**: AccessibilityRole changed from flags enum to standard enum.
- **USS Parser upgraded**: Better error detection, may flag previously ignored invalid USS.
- **Legacy ETC compression removed**: Auto-migrated to default compressor.
- **HTTP/2 default**: UnityWebRequest now uses HTTP/2 for faster networking.
- **Animation performance**: Legacy Animation evaluation up to 30% faster.

## Verified Sources

- Official docs: https://docs.unity3d.com/6000.3/Documentation/Manual/
- What's new in 6.3: https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html
- Upgrade guide 6.2→6.3: https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity63.html
- Upgrade guide to Unity 6: https://docs.unity3d.com/6000.3/Documentation/Manual/UpgradeGuideUnity6.html
- Unity 6 release: https://unity.com/releases/unity-6
- Unity 6.3 LTS announcement: https://unity.com/blog/unity-6-3-lts-is-now-available
- Unity 6 support: https://unity.com/releases/unity-6/support
- C# API reference: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/index.html
