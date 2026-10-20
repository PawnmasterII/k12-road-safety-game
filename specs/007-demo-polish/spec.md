# 007 — Presentation readiness

Status: In progress

## Scope
Polish the existing Safe Travelers Club website and Unity city prototype for a live research-work demonstration. Preserve unfinished intentional work; avoid broad architectural changes. Publish source to the existing PawnmasterII/k12-road-safety-game GitHub repository.

## User stories and acceptance
- P1: A presenter can start the website, enter as a guest, complete an activity, and demonstrate feedback without broken navigation.
- P1: A presenter can open the Unity City scene, explore, interact with an NPC, and answer a quiz without compile/runtime exceptions or permanently frozen movement.
- P1: A collaborator can clone the repository and find setup, controls, a short demo sequence, and honest prototype limitations.
- P2: Website controls are readable on desktop/mobile and keyboard accessible; error/loading states are intelligible.

## Requirements
- FR-001 Preserve guest and authenticated progress semantics; no real student data or secrets in Git.
- FR-002 Preserve fog-of-war and crossing mechanics; improve only verified defects and presentation usability.
- FR-003 Include Unity Assets, Packages and ProjectSettings, excluding Library, build output and transient captures.
- FR-004 Verify website build and meaningful user flows; verify Unity compile, advancing play frames, interactions, and console.
- FR-005 Push to the requested existing GitHub repository and read back the published commit.

## Success criteria
Website production build succeeds; demo flows are exercised; Unity scene runs and interaction probes pass; repository has reproducible instructions; published SHA equals local deliverable SHA.

## Non-goals
Public hosting, a claimed production release, completed WebGL/backend integration, adding audio assets, or wholesale UI technology migration.
