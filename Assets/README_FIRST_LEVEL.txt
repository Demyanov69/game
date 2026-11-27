GARDEN / ECHO ISLANDS - FIRST LEVEL

Main level: Assets/Scenes/Main.unity
Unity: 6000.3.0f1

The first level is rebuilt automatically when LevelFlow.Begin(1) starts.
It uses the existing world-switch system and the low-poly dungeon assets in Assets/BrokenVector/LowPolyDungeon.

Route:
1. Entrance: take the staff with E.
2. Trial 1: light stone bridge -> common safe ledge -> switch to dark -> dark wooden bridge.
3. Trial 2: four common moving platforms. Platform heights change between worlds; switching is only needed while standing on a platform.
4. Trial 3: take the dark-world orb -> switch to light -> use safe pads to shelter from wind -> place orb on the light pedestal -> use the activated portal.

Controls:
WASD - move
Space - jump
LMB - switch world (after collecting the staff)
E - interact / pick up / socket orb / use portal
Esc - release or lock mouse

Checkpoints are placed after Trials 1 and 2. Falling respawns at the latest checkpoint.

The old PrototypeBuilder menu entry is retained only as: Echo Islands/Build Legacy Prototype (old).


ЗАПУСК ПЕРВОГО УРОВНЯ
1. Откройте Assets/Scenes/Main.unity в Unity 6000.3.0f1.
2. Нажмите Play.
3. Появится меню Echo Islands. Нажмите "Начать".
4. После этого можно двигаться WASD, смотреть мышью, прыгать Space. ЛКМ переключает мир после подбора посоха, E взаимодействует с объектами.

В этой версии LevelFlow дополнительно сам строит L1 при входе в Play, поэтому персонаж больше не остается в старой прототипной сцене и не появляется за пределами пола.
