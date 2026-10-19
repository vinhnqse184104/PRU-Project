M4 CHAPTER 4–5 — INTEGRATION

Start: Assets/Scenes/Chapter4_LythongCuopCong.unity
Quick test: Assets/Gameplay_M4/Chapter5/Scene_M4_Chapter5_Tracking.unity

Flow:
E near soldier -> Chapter4SoldierEscort -> court dialogue -> Chapter2_LyThong
-> Chapter2OfficialReturnCutscene -> Chapter5_DaiBangBatCongChua
-> DaiBangBatCoc -> Scene_M4_Chapter5_Tracking -> Chapter6_HangDaiBang.

Court: E starts the quest, actor follows the soldier automatically.
Dialogue: Enter / Space / left mouse.
Tracking: WASD, Shift, Space, hold RMB to aim, LMB shoots, E interacts,
R respawns to a safe checkpoint, F5 restarts the tracking quest.
One arrow must hit the eagle. Discover all three clues in order and confirm
the cave. Release E and press again to enter Chapter6.

M5 hooks:
Chapter4SoldierEscort.onEscortStarted / onReportCompleted.
Chapter2OfficialReturnCutscene.onOfficialReturnCompleted;
set continueToChapter5=false if M5 owns that transition.
DaiBangBatCoc.onKidnappingCompleted;
set continueToTracking=false if M5 owns that transition.
M4Chapter5Quest.onEagleWounded / onClueFound / onChapterCompleted / onCaveEntered;
set loadNextScene=false if M5 owns entry into the cave.
Do not let two components load the next scene for the same event.

StoryStateManager saves IsChapter4Completed and IsChapter5Completed.
StartNewGame resets both along with existing story flags.
The in-progress trail is local to this scene, not a mid-quest save system.
The M4 Chapter3 boss scene still needs its separate story adapter.

Dependencies: existing Gameplay_M4 player/Chapter2 movement animations,
M1 foliage, M2 eagle from the shared Chapter5 cutscene,
Assets/Props/cung and Assets/Props/mui_ten from Huy.
This forest scene is an M4 gameplay test map and can be replaced by M1.
There is no dedicated bow-draw animation yet; existing locomotion is reused.

Editor setup utilities have guards against batch/Play Mode scene edits;
their manual menu commands are retained.
