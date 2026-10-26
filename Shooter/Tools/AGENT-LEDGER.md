# Subagent token ledger (Shooter port)

Figures are the `subagent_tokens` each agent reported on completion. Failed/limit-hit agents report nothing, so their usage is unknown (not zero).

| Agent | Task | Tokens |
|---|---|---|
| Baseline capture (run 1) | Unity batch screenshots | 78,871 |
| UnityExtractor | YAML -> JSON tool | 224,813 |
| Static asset import | textures/audio/fonts/meshes | 262,739 |
| Skinned pipeline (incl. follow-up) | rigs + animation | 269,159 |
| Scene loader | bake, loader, harness | 209,908 |
| Renderer + shaders | Phase 3 M1-M3 | unknown (hit session limit) |
| Baseline play-mode redo | Play-mode captures | unknown (hit session limit) |

Known subtotal: 1,045,490 tokens (excludes this main conversation and the two unknowns).

Phase 3 verification (pose wiring, renderer harness fixes, blaster emission, static HoverBot, eye glow, pose fitting) was done in the main conversation with no subagents, so there is nothing to add to the table for it.
