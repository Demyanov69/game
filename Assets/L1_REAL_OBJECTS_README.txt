L1 REAL OBJECTS FIX

This version stops generating the main L1 gameplay objects only at runtime. They are real scene objects in Main.unity.

Hierarchy:
Level_1
  L1_Gameplay_REAL_OBJECTS
    L1_StaffPickup_Air
      Air staff (prefab child)
    L1_PortalSpawn
      L1_ExitPortal
        PortalFrameLeft
        PortalFrameRight
        PortalFrameTop
        HovlPortal
        OrbSocket
    L1_OrbSpawn
      L1_DarkWorldOrb
    L1_DarkBridge
      PT_Wooden_Bridge_02
    L1_BridgeBarrier_LightOnly

Move the marker objects directly in Scene view:
- L1_StaffSpawn
- L1_OrbSpawn
- L1_PortalSpawn

The actual bridge object is under L1_DarkBridge and is Dark-only. The invisible collider is Light-only.

Temporary testing: press T to switch worlds. The bottom-right button also switches worlds.

The portal is linked to LevelFlow nextLevel=2. Put the player into the green Hovl portal after it activates to enter level 2.

The old EnvironmentFreeL1Bootstrap is disabled in Main.unity so it cannot spawn duplicate procedural items.
