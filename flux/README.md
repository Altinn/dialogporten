# Flux configuration (Dialogporten)

This folder contains the DIS app-config (`flux/dialogporten`) and syncroot wiring (`flux/syncroot`).
Flux pulls directly from this repository via `GitRepository` and applies environment overlays under
`flux/dialogporten/overlays/<env>`.

## Open action
RoleAssignment resources for the ApplicationIdentity principals are still pending. We need the DIS
ASO RoleAssignment schema before adding them.
