# Cloud character-art pipeline

## Purpose

This pipeline creates the full rigged Idea Zoo baseline without requiring Blender or Unity on the local computer.

## Generation stage

The `Cloud art production` GitHub Actions workflow:

1. Checks out `cloud-art-production-v1`.
2. Installs Blender in the Linux runner.
3. Runs `cloud_art/generate_idea_zoo_assets.py` in background mode.
4. Exports FBX, GLB, editable Blender sources and review PNGs.
5. Builds a single contact sheet.
6. Validates asset count, file presence and mobile triangle limits.
7. Commits deterministic generated output back to the branch.
8. Uploads the complete package as a workflow artifact.

The workflow only watches source scripts and design-package files. Its generated-output commit does not recursively trigger another art build.

## Unity integration stage

When the branch is opened as a pull request, the existing licensed Unity cloud workflow:

1. Imports every generated FBX.
2. Applies mobile-safe importer settings.
3. Runs `CloudArtPrefabBaker.BakeAll()` through EditMode tests.
4. Creates Resources prefabs for all characters and creature families.
5. Validates required humanoid nodes and creature sockets.
6. Runs PlayMode tests with the runtime replacement bridge.
7. Produces the WebGL review build.

## Runtime fallback

Imported art is additive. If a prefab is absent, the previous procedural character or creature remains visible. Once a cloud prefab is available, the bridge hides the procedural renderers and binds the imported rig.

## Generated paths

Runtime FBX files:

`unity/Assets/IdeaZoo/Art/CloudGenerated/Models/`

Unity Resources prefabs:

`unity/Assets/IdeaZoo/Resources/IdeaZooArt/`

Editable sources and portable GLB files:

`unity/ArtProduction/Source/`

Review images:

`unity/ArtProduction/Previews/`

Machine-readable output manifest:

`unity/ArtProduction/generated_manifest.json`

## Safe regeneration

The generator resets each Blender scene before constructing an asset and produces all files from source definitions. Do not hand-edit generated FBX files. Change the generator or replace the corresponding editable source through a deliberate art revision.
