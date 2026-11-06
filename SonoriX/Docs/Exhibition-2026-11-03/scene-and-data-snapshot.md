# Scene, prefab and data static snapshot

## GUID scan findings

No duplicate GUIDs were found among local Assets metadata. No unresolved non-built-in GUIDs were found in the scanned FFrameWork prefabs or song assets. This does not validate local fileID references or serialized component compatibility.

Unresolved GUIDs (Assets plus local Library/PackageCache lookup):

| Scene | GUIDs |
|---|---|
| xxAxxLive.unity | `ece92f11be2f424381ae86e42cb3f510` (previewSusScore; also present in HEAD) |
| SonoriX.unity | `8d9ff4af4a8eb0f4499ae05be3ec4562`, `c64a285facc38594391c583783782416`, `82afbe637a8a67b46b6bd2ff480066f3`, `1c28e2fd01e71ef4b8c971373a2f887d`, `0f6175279bd2f324b8acd3049fa1fc1a`, `ece92f11be2f424381ae86e42cb3f510` |
| TestMINIs.unity | `e8c9c792a3b6846a2a471fbe03bbe9f6`, `4017884bd923c4fa38c2ba21de9d98bf`, `df58902d991454bceac7c2632cb6ef51`, `dca676fe8c5cd41f2951188e76d0624b`, `15511ade9f4d14af694ed2252dd4c8c4` |
| TestScene.unity | `1219d495d6995c547af63dea092114f1`, `d5ac0162acca47378b08e1cc45553aa8`, `811797e0aa043e44f96440f3f2bdcfb8` |

Only xxAxxLive.unity is enabled in EditorBuildSettings. Unresolved references in other scenes still matter if those scenes are retained for development.

Baseline HEAD: 76799ed. Date: 2026-11-03. GUID scan covers Assets and local Library/PackageCache. Runtime validation is pending.

## Assets/Scenes/SonoriX.unity

Direct serialized objects: 148; Legacy Text: 21; TMP UGUI: 1. Nested prefabs excluded.

Prefab sources:

- `Assets/Resources/Prefab/FFrameWork/NoteSpeedSettingPanel.prefab` x 1
- `Assets/Resources/Prefab/FFrameWork/ArrowJudgmentView.prefab` x 1
- `Assets/Resources/Prefab/RotatingWheels.prefab` x 3
- `Assets/Resources/Prefab/MusicVisualizer.prefab` x 1
- `Assets/Resources/Prefab/FFrameWork/OperationGuidePanel.prefab` x 1
- `Assets/Resources/Prefab/FFrameWork/OperationGuidePromptPanel.prefab` x 1
- `Assets/Resources/Prefab/FFrameWork/PanelTransitionController.prefab` x 1
## Assets/Scenes/TestMINIs.unity

Direct serialized objects: 2; Legacy Text: 0; TMP UGUI: 0. Nested prefabs excluded.

Prefab sources:

## Assets/Scenes/TestScene.unity

Direct serialized objects: 26; Legacy Text: 0; TMP UGUI: 0. Nested prefabs excluded.

Prefab sources:

- `Assets/Resources/Prefab/MusicVisualizer.prefab` x 1
- `1219d495d6995c547af63dea092114f1` x 1
## Assets/Scenes/TestSonoriX.unity

Direct serialized objects: 2; Legacy Text: 0; TMP UGUI: 0. Nested prefabs excluded.

Prefab sources:

## Assets/Scenes/xxAxxLive.unity

Direct serialized objects: 65; Legacy Text: 0; TMP UGUI: 1. Nested prefabs excluded.

Prefab sources:

- `Assets/Resources/Prefab/FFrameWork/NoteSpeedSettingPanel.prefab` x 1
- `Assets/Resources/Prefab/FFrameWork/ArrowJudgmentView.prefab` x 1
- `Assets/Resources/Prefab/FFrameWork/TutorialPanel.prefab` x 1
- `Assets/Resources/Prefab/MusicVisualizer.prefab` x 1
- `Assets/Resources/Prefab/FFrameWork/OperationGuidePanel.prefab` x 1
- `Assets/Resources/Prefab/FFrameWork/OperationGuidePromptPanel.prefab` x 1
- `Assets/Resources/Prefab/FFrameWork/SelectSongPanel.prefab` x 1
- `Assets/Resources/Prefab/FFrameWork/PanelTransitionController.prefab` x 1
- `Assets/Resources/Prefab/FFrameWork/InGamePanel.prefab` x 1
- `Assets/Resources/Prefab/FFrameWork/StartGamePanel.prefab` x 1
- `Assets/Resources/Prefab/FFrameWork/ResultPanel.prefab` x 1

Same-fileID component changes (Transform blocks excluded):

```diff
--- HEAD 10219555
+++ WORKTREE 10219555
@@ -20,3 +20,3 @@
       Path: event:/SE/se_live_perfect
-    volume: 0.4
+    volume: 0.7
     attachToGameObject: 0
@@ -30,3 +30,3 @@
       Path: event:/SE/se_live_great
-    volume: 0.4
+    volume: 0.7
     attachToGameObject: 0
@@ -40,3 +40,3 @@
       Path: event:/SE/se_live_good
-    volume: 0.4
+    volume: 0.7
     attachToGameObject: 0
@@ -50,3 +50,3 @@
       Path: event:/SE/se_live_flick
-    volume: 0.4
+    volume: 1.5
     attachToGameObject: 0
--- HEAD 263063397
+++ WORKTREE 263063397
@@ -1,7 +1,6 @@
 MonoBehaviour:
-  m_ObjectHideFlags: 0
-  m_CorrespondingSourceObject: {fileID: 0}
-  m_PrefabInstance: {fileID: 0}
+  m_CorrespondingSourceObject: {fileID: 7898285184600260360, guid: ca2753fede37cd34ba109a8487359b6e, type: 3}
+  m_PrefabInstance: {fileID: 5599497664544700849}
   m_PrefabAsset: {fileID: 0}
-  m_GameObject: {fileID: 263063395}
+  m_GameObject: {fileID: 0}
   m_Enabled: 1
@@ -11,19 +10 @@
   m_EditorClassIdentifier: Assembly-CSharp::FFrameWork.ScoreView
-  scoreFillImage: {fileID: 0}
-  scoreText: {fileID: 1188712858}
-  addScoreText: {fileID: 0}
-  rankText: {fileID: 0}
-  rankImage: {fileID: 0}
-  scoreGauge: {fileID: 0}
-  rankSpriteRenderer: {fileID: 0}
-  rankSprites: []
-  rankTextSpriteRenderer: {fileID: 0}
-  gaugeRankS: {fileID: 0}
-  gaugeRankA: {fileID: 0}
-  gaugeRankB: {fileID: 0}
-  gaugeRankC: {fileID: 0}
-  number: {fileID: 0}
-  outlineNumber: {fileID: 0}
-  addScoreRoot: {fileID: 0}
-  addScoreNumber: {fileID: 0}
-  addScoreOutlineNumber: {fileID: 0}
--- HEAD 468347866
+++ WORKTREE 468347866
@@ -1,15 +1,4 @@
 GameObject:
-  m_ObjectHideFlags: 0
-  m_CorrespondingSourceObject: {fileID: 0}
-  m_PrefabInstance: {fileID: 0}
+  m_CorrespondingSourceObject: {fileID: 4447237616452192569, guid: ca2753fede37cd34ba109a8487359b6e, type: 3}
+  m_PrefabInstance: {fileID: 5599497664544700849}
   m_PrefabAsset: {fileID: 0}
-  serializedVersion: 6
-  m_Component:
-  - component: {fileID: 468347867}
-  m_Layer: 5
-  m_Name: InGamePanel
-  m_TagString: Untagged
-  m_Icon: {fileID: 0}
-  m_NavMeshLayer: 0
-  m_StaticEditorFlags: 0
-  m_IsActive: 1
--- HEAD 518130898
+++ WORKTREE 518130898
@@ -7,42 +7,2 @@
     m_Modifications:
-    - target: {fileID: 1293401854175825629, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_AnchoredPosition.y
-      value: 99
-      objectReference: {fileID: 0}
-    - target: {fileID: 1390626733566679778, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_LocalScale.y
-      value: 0.68745
-      objectReference: {fileID: 0}
-    - target: {fileID: 1390626733566679778, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_AnchoredPosition.y
-      value: 21
-      objectReference: {fileID: 0}
-    - target: {fileID: 1581872322449035573, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_AnchoredPosition.y
-      value: 100
-      objectReference: {fileID: 0}
-    - target: {fileID: 1740418186897077715, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_LocalScale.y
-      value: 0.68745
-      objectReference: {fileID: 0}
-    - target: {fileID: 1740418186897077715, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_AnchoredPosition.y
-      value: 21
-      objectReference: {fileID: 0}
-    - target: {fileID: 1870440922144091236, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_Sprite
-      value: 
-      objectReference: {fileID: 21300000, guid: 200466fc4b627824d8725cf0034c53fe, type: 3}
-    - target: {fileID: 1870440922144091236, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_Color.b
-      value: 0.8962264
-      objectReference: {fileID: 0}
-    - target: {fileID: 1870440922144091236, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_Color.g
-      value: 0.8962264
-      objectReference: {fileID: 0}
-    - target: {fileID: 1870440922144091236, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_Color.r
-      value: 0.8962264
-      objectReference: {fileID: 0}
     - target: {fileID: 1935482398289504034, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
@@ -58,14 +18,2 @@
       value: 0.59090906
-      objectReference: {fileID: 0}
-    - target: {fileID: 1998086263383928597, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_Text
-      value: "\u623B\u308B"
-      objectReference: {fileID: 0}
-    - target: {fileID: 2336753236733748360, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_Text
-      value: "\u30B2\u30FC\u30E0\u30B9\u30BF\u30FC\u30C8"
-      objectReference: {fileID: 0}
-    - target: {fileID: 2573999791983870665, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_AnchoredPosition.y
-      value: 100
       objectReference: {fileID: 0}
@@ -159,26 +107,6 @@
       objectReference: {fileID: 0}
-    - target: {fileID: 5394418940506594983, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_SizeDelta.x
-      value: 1214.63
-      objectReference: {fileID: 0}
-    - target: {fileID: 5394418940506594983, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_SizeDelta.y
-      value: 435.2
-      objectReference: {fileID: 0}
-    - target: {fileID: 5394418940506594983, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_AnchoredPosition.y
-      value: 194
-      objectReference: {fileID: 0}
-    - target: {fileID: 5543286654245194175, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_SizeDelta.x
-      value: -25
-      objectReference: {fileID: 0}
-    - target: {fileID: 5543286654245194175, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_AnchoredPosition.x
-      value: -12.5
-      objectReference: {fileID: 0}
-    - target: {fileID: 7129925015183307483, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_AnchoredPosition.x
-      value: 0
-      objectReference: {fileID: 0}
+    - target: {fileID: 7125168957075170622, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
+      propertyPath: m_Sprite
+      value: 
+      objectReference: {fileID: 21300000, guid: acf6eebbe97339549a644db143198e65, type: 3}
     - target: {fileID: 7439119323858315114, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
@@ -190,26 +118,2 @@
       value: 1
-      objectReference: {fileID: 0}
-    - target: {fileID: 7765408515079314813, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_LocalScale.x
-      value: 0.73527384
-      objectReference: {fileID: 0}
-    - target: {fileID: 7765408515079314813, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_LocalScale.y
-      value: 0.73527384
-      objectReference: {fileID: 0}
-    - target: {fileID: 7765408515079314813, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_LocalScale.z
-      value: 0.73527384
-      objectReference: {fileID: 0}
-    - target: {fileID: 7765408515079314813, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_AnchoredPosition.y
-      value: -177
-      objectReference: {fileID: 0}
-    - target: {fileID: 7877640148690014959, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_LocalScale.y
-      value: 0.68745
-      objectReference: {fileID: 0}
-    - target: {fileID: 7877640148690014959, guid: 376672f9dd05dc44d81ea1fe43bbf99c, type: 3}
-      propertyPath: m_AnchoredPosition.y
-      value: 21
       objectReference: {fileID: 0}
--- HEAD 763914760
+++ WORKTREE 763914760
@@ -77,3 +77,3 @@
       propertyPath: m_IsActive
-      value: 0
+      value: 1
       objectReference: {fileID: 0}
--- HEAD 867797832
+++ WORKTREE 867797832
@@ -88,18 +88,2 @@
     - target: {fileID: 352744365300497857, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
-      propertyPath: pageText
-      value: 
-      objectReference: {fileID: 0}
-    - target: {fileID: 352744365300497857, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
-      propertyPath: backButton
-      value: 
-      objectReference: {fileID: 0}
-    - target: {fileID: 352744365300497857, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
-      propertyPath: slideImage
-      value: 
-      objectReference: {fileID: 1921085297}
-    - target: {fileID: 352744365300497857, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
-      propertyPath: finishButton
-      value: 
-      objectReference: {fileID: 0}
-    - target: {fileID: 352744365300497857, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
       propertyPath: flowController
@@ -109,3 +93,3 @@
       propertyPath: slideSprites.Array.size
-      value: 5
+      value: 7
       objectReference: {fileID: 0}
@@ -114,3 +98,3 @@
       value: 
-      objectReference: {fileID: 21300000, guid: 8d9ff4af4a8eb0f4499ae05be3ec4562, type: 3}
+      objectReference: {fileID: 21300000, guid: e8d853f791ff25d499310e19c7a3f600, type: 3}
     - target: {fileID: 352744365300497857, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
@@ -118,3 +102,3 @@
       value: 
-      objectReference: {fileID: 21300000, guid: c64a285facc38594391c583783782416, type: 3}
+      objectReference: {fileID: 21300000, guid: 10c77661d5e06614fa29351496e8db19, type: 3}
     - target: {fileID: 352744365300497857, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
@@ -122,3 +106,3 @@
       value: 
-      objectReference: {fileID: 21300000, guid: 82afbe637a8a67b46b6bd2ff480066f3, type: 3}
+      objectReference: {fileID: 21300000, guid: b5093f0cd689d964ea2f7aa96a73fb35, type: 3}
     - target: {fileID: 352744365300497857, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
@@ -126,3 +110,3 @@
       value: 
-      objectReference: {fileID: 21300000, guid: 1c28e2fd01e71ef4b8c971373a2f887d, type: 3}
+      objectReference: {fileID: 21300000, guid: 4a95946ca7237de4ead7228a9889ae9d, type: 3}
     - target: {fileID: 352744365300497857, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
@@ -130,10 +114,18 @@
       value: 
-      objectReference: {fileID: 21300000, guid: 0f6175279bd2f324b8acd3049fa1fc1a, type: 3}
-    - target: {fileID: 1990877698382087514, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
-      propertyPath: m_AnchoredPosition.x
-      value: -880
-      objectReference: {fileID: 0}
-    - target: {fileID: 2585655939892342967, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
-      propertyPath: m_AnchoredPosition.x
-      value: 880
+      objectReference: {fileID: 21300000, guid: 21964b296af4e614e826821906088190, type: 3}
+    - target: {fileID: 352744365300497857, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
+      propertyPath: 'slideSprites.Array.data[5]'
+      value: 
+      objectReference: {fileID: 21300000, guid: ccec63d334dc2a5458fbf5092fbdd2f7, type: 3}
+    - target: {fileID: 352744365300497857, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
+      propertyPath: 'slideSprites.Array.data[6]'
+      value: 
+      objectReference: {fileID: 21300000, guid: cc7bf5697e6c9fb4b90a6f923fb29eab, type: 3}
+    - target: {fileID: 812736962087806016, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
+      propertyPath: m_Sprite
+      value: 
+      objectReference: {fileID: 21300000, guid: e8d853f791ff25d499310e19c7a3f600, type: 3}
+    - target: {fileID: 2125090519890809982, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
+      propertyPath: m_FontData.m_Alignment
+      value: 1
       objectReference: {fileID: 0}
@@ -147,22 +139,2 @@
       objectReference: {fileID: 0}
-    - target: {fileID: 8373558762467983055, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
-      propertyPath: m_Sprite
-      value: 
-      objectReference: {fileID: 21300000, guid: 217796cd4854d814998dbcb04bdbf806, type: 3}
-    - target: {fileID: 8373558762467983055, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
-      propertyPath: m_Color.a
-      value: 1
-      objectReference: {fileID: 0}
-    - target: {fileID: 8373558762467983055, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
-      propertyPath: m_Color.b
-      value: 1
-      objectReference: {fileID: 0}
-    - target: {fileID: 8373558762467983055, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
-      propertyPath: m_Color.g
-      value: 1
-      objectReference: {fileID: 0}
-    - target: {fileID: 8373558762467983055, guid: 297cce117c26e9741ba05544b68b38c0, type: 3}
-      propertyPath: m_Color.r
-      value: 1
-      objectReference: {fileID: 0}
     m_RemovedComponents: []
--- HEAD 969907335
+++ WORKTREE 969907335
@@ -20,3 +20,3 @@
   operationGuidePanel: {fileID: 1265601394}
-  songSelectMenu: {fileID: 0}
+  songSelectMenu: {fileID: 1186679450}
   defaultSongIndex: 0
@@ -24,2 +24,18 @@
   createDefaultOperationGuidePanels: 1
+  enableTitleIdleAnimation: 1
+  titleIdleWaitSeconds: 60
+  titleIdleAnimationView: {fileID: 0}
+  titleIdleVideoClip: {fileID: 32900000, guid: bff25494c66a2914baf0f1daf09a352e, type: 3}
+  titleIdleAudioEvent:
+    Guid:
+      Data1: -2087688864
+      Data2: 1188568147
+      Data3: 536641446
+      Data4: 1639801897
+    Path: event:/Music/BGM/DemoAnimationBGM
+  titleIdleAudioVolume: 0.8
+  titleIdlePlaybackSeconds: 0
+  muteMenuBgmDuringTitleIdle: 1
+  selectedSongInfoWindowPrefab: {fileID: 8918054847427175318, guid: f76fa58239b4cdb43a4c8a38359b3bb9, type: 3}
+  createDefaultSelectedSongInfoWindow: 0
   panelTransition: {fileID: 108874201}
--- HEAD 973888419
+++ WORKTREE 973888419
@@ -14,5 +14,7 @@
   frameDebugView: {fileID: 1313026319}
-  minWorldX: -270
-  maxWorldX: 270
+  minWorldX: -305
+  maxWorldX: 305
   sensitivity: 1.2
+  centerZoneThreshold: 180
+  centerSensitivityMultiplier: 1.5
   startWorldX: 0
--- HEAD 977374561
+++ WORKTREE 977374561
@@ -1,17 +1,4 @@
 GameObject:
-  m_ObjectHideFlags: 0
-  m_CorrespondingSourceObject: {fileID: 0}
-  m_PrefabInstance: {fileID: 0}
+  m_CorrespondingSourceObject: {fileID: 9182604631513454508, guid: a0e4f952efbc7e949a36a4773f371f65, type: 3}
+  m_PrefabInstance: {fileID: 2253354964707958345}
   m_PrefabAsset: {fileID: 0}
-  serializedVersion: 6
-  m_Component:
-  - component: {fileID: 977374562}
-  - component: {fileID: 977374564}
-  - component: {fileID: 977374563}
-  m_Layer: 5
-  m_Name: SelectSongPanel
-  m_TagString: Untagged
-  m_Icon: {fileID: 0}
-  m_NavMeshLayer: 0
-  m_StaticEditorFlags: 0
-  m_IsActive: 0
--- HEAD 1186679450
+++ WORKTREE 1186679450
@@ -1,7 +1,6 @@
 MonoBehaviour:
-  m_ObjectHideFlags: 0
-  m_CorrespondingSourceObject: {fileID: 0}
-  m_PrefabInstance: {fileID: 0}
+  m_CorrespondingSourceObject: {fileID: 2302602967915263428, guid: a0e4f952efbc7e949a36a4773f371f65, type: 3}
+  m_PrefabInstance: {fileID: 2253354964707958345}
   m_PrefabAsset: {fileID: 0}
-  m_GameObject: {fileID: 1186679449}
+  m_GameObject: {fileID: 0}
   m_Enabled: 1
@@ -11,39 +10 @@
   m_EditorClassIdentifier: Assembly-CSharp::FFrameWork.UI.SongSelectMenuController
-  songDataBase: {fileID: 11400000, guid: 2008f5bfcb43a654d9fd8eb33abacc66, type: 2}
-  flowController: {fileID: 969907335}
-  itemRoot: {fileID: 1186679451}
-  itemPrefab: {fileID: 8918054847427175318, guid: 81dcefe618c36f14d814f9ef8b3257e8, type: 3}
-  itemSize: {x: 1200, y: 300}
-  topPosition: {x: 0, y: 300}
-  itemSpacing: -30
-  deltaThreshold: 2
-  moveRepeatSeconds: 0.12
-  createRuntimeDebugDifficulty: 0
-  visibleItemCount: 3
-  scrollSmoothTime: 0.12
-  viewportPadding: 10
-  songSelectSoundEvent:
-    Guid:
-      Data1: 1729057501
-      Data2: 1121718115
-      Data3: -989647681
-      Data4: -262298971
-    Path: event:/SE/se_select_menu
-  songToDifficultySoundEvent:
-    Guid:
-      Data1: 488349020
-      Data2: 1175335577
-      Data3: 1745878453
-      Data4: -1818914405
-    Path: event:/SE/se_select_music
-  difficultySelectSoundEvent:
-    Guid:
-      Data1: 1729057501
-      Data2: 1121718115
-      Data3: -989647681
-      Data4: -262298971
-    Path: event:/SE/se_select_menu
-  songSelectSoundVolume: 1
-  songToDifficultySoundVolume: 1
-  difficultySelectSoundVolume: 1
-  logMissingUiSound: 1
--- HEAD 1202846755
+++ WORKTREE 1202846755
@@ -11,5 +11,5 @@
       objectReference: {fileID: 0}
-    - target: {fileID: 582311351121779384, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_IsActive
-      value: 0
+    - target: {fileID: 690894657234900161, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
+      propertyPath: m_SizeDelta.y
+      value: 300
       objectReference: {fileID: 0}
@@ -95,18 +95,2 @@
       objectReference: {fileID: 0}
-    - target: {fileID: 1824900075617009263, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Color.b
-      value: 0.5568628
-      objectReference: {fileID: 0}
-    - target: {fileID: 1824900075617009263, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Color.g
-      value: 0.18039216
-      objectReference: {fileID: 0}
-    - target: {fileID: 1824900075617009263, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Color.r
-      value: 0
-      objectReference: {fileID: 0}
-    - target: {fileID: 1897571550977648320, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: backButton
-      value: 
-      objectReference: {fileID: 0}
     - target: {fileID: 1897571550977648320, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
@@ -115,85 +99,5 @@
       objectReference: {fileID: 969907335}
-    - target: {fileID: 2565986555897523146, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_SizeDelta.x
-      value: 840
-      objectReference: {fileID: 0}
-    - target: {fileID: 2565986555897523146, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_AnchoredPosition.y
-      value: -158
-      objectReference: {fileID: 0}
-    - target: {fileID: 2630346947033245200, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_AnchoredPosition.x
-      value: 250
-      objectReference: {fileID: 0}
-    - target: {fileID: 2630346947033245200, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_AnchoredPosition.y
-      value: 164
-      objectReference: {fileID: 0}
-    - target: {fileID: 2698114508549392390, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Sprite
-      value: 
-      objectReference: {fileID: 21300000, guid: 217796cd4854d814998dbcb04bdbf806, type: 3}
-    - target: {fileID: 2698114508549392390, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Color.a
-      value: 1
-      objectReference: {fileID: 0}
-    - target: {fileID: 2698114508549392390, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Color.b
-      value: 1
-      objectReference: {fileID: 0}
-    - target: {fileID: 2698114508549392390, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Color.g
-      value: 1
-      objectReference: {fileID: 0}
-    - target: {fileID: 2698114508549392390, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Color.r
-      value: 1
-      objectReference: {fileID: 0}
-    - target: {fileID: 4215125304749962207, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Text
-      value: "\u64CD\u4F5C\u8AAC\u660E\uFF08\u904A\u3073\u65B9\uFF09\u3092\u898B\u307E\u3059\u304B\uFF1F"
-      objectReference: {fileID: 0}
-    - target: {fileID: 4215125304749962207, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_FontData.m_Font
-      value: 
-      objectReference: {fileID: 12800000, guid: 48ad0c6c4dbc62548ab42e8bd4bd9c81, type: 3}
-    - target: {fileID: 4440534385374978520, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_AnchoredPosition.x
-      value: -250
-      objectReference: {fileID: 0}
-    - target: {fileID: 4440534385374978520, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_AnchoredPosition.y
-      value: 164
-      objectReference: {fileID: 0}
     - target: {fileID: 6122067223200617238, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
       propertyPath: m_AnchoredPosition.y
-      value: -321
-      objectReference: {fileID: 0}
-    - target: {fileID: 6645070762465111786, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Sprite
-      value: 
-      objectReference: {fileID: 21300000, guid: 200466fc4b627824d8725cf0034c53fe, type: 3}
-    - target: {fileID: 6645070762465111786, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Color.b
-      value: 1
-      objectReference: {fileID: 0}
-    - target: {fileID: 6645070762465111786, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Color.g
-      value: 1
-      objectReference: {fileID: 0}
-    - target: {fileID: 6645070762465111786, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Color.r
-      value: 1
-      objectReference: {fileID: 0}
-    - target: {fileID: 8132939210851637938, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Text
-      value: "\u898B\u308B"
-      objectReference: {fileID: 0}
-    - target: {fileID: 8410796531811549267, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_SizeDelta.x
-      value: 1108.3
-      objectReference: {fileID: 0}
-    - target: {fileID: 8410796531811549267, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_SizeDelta.y
-      value: 495.8
+      value: -130
       objectReference: {fileID: 0}
@@ -201,20 +105,8 @@
       propertyPath: m_AnchoredPosition.y
-      value: 184
+      value: 150
       objectReference: {fileID: 0}
-    - target: {fileID: 8544723271350680480, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Color.b
-      value: 0.5568628
-      objectReference: {fileID: 0}
-    - target: {fileID: 8544723271350680480, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Color.g
-      value: 0.17784734
-      objectReference: {fileID: 0}
-    - target: {fileID: 8544723271350680480, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Color.r
-      value: 0
-      objectReference: {fileID: 0}
-    - target: {fileID: 8580798834200157842, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
-      propertyPath: m_Text
-      value: "\u30B9\u30AD\u30C3\u30D7"
-      objectReference: {fileID: 0}
+    - target: {fileID: 9054156233501202526, guid: eec4494232d9a4f4283bc9ada74648d9, type: 3}
+      propertyPath: m_Sprite
+      value: 
+      objectReference: {fileID: 21300000, guid: 98b2adc5a6d0f9d4d91d6be21da77c36, type: 3}
     m_RemovedComponents: []
--- HEAD 1274096449
+++ WORKTREE 1274096449
@@ -1,17 +1,4 @@
 GameObject:
-  m_ObjectHideFlags: 0
-  m_CorrespondingSourceObject: {fileID: 0}
-  m_PrefabInstance: {fileID: 0}
+  m_CorrespondingSourceObject: {fileID: 6739979880355595882, guid: dc4678c03f41b3249844619963aec13e, type: 3}
+  m_PrefabInstance: {fileID: 6560436995050463758}
   m_PrefabAsset: {fileID: 0}
-  serializedVersion: 6
-  m_Component:
-  - component: {fileID: 1274096450}
-  - component: {fileID: 1274096452}
-  - component: {fileID: 1274096453}
-  m_Layer: 5
-  m_Name: ResultPanel
-  m_TagString: Untagged
-  m_Icon: {fileID: 0}
-  m_NavMeshLayer: 0
-  m_StaticEditorFlags: 0
-  m_IsActive: 0
--- HEAD 1279380353
+++ WORKTREE 1279380353
@@ -1,7 +1,6 @@
 MonoBehaviour:
-  m_ObjectHideFlags: 0
-  m_CorrespondingSourceObject: {fileID: 0}
-  m_PrefabInstance: {fileID: 0}
+  m_CorrespondingSourceObject: {fileID: 5349472775570716510, guid: ca2753fede37cd34ba109a8487359b6e, type: 3}
+  m_PrefabInstance: {fileID: 5599497664544700849}
   m_PrefabAsset: {fileID: 0}
-  m_GameObject: {fileID: 1279380350}
+  m_GameObject: {fileID: 0}
   m_Enabled: 1
@@ -11,33 +10 @@
   m_EditorClassIdentifier: Assembly-CSharp::FFrameWork.Live.ComboView
-  numberRoot: {fileID: 1181811117}
-  effectNumberRoot: {fileID: 0}
-  allPerfectEffectRoot: {fileID: 0}
-  digitSpacing: 60
-  digitSize: {x: 64, y: 80}
-  numberSprites:
-  - {fileID: 21300000, guid: 9c422df76b3d1984985a79f3e67438dc, type: 3}
-  - {fileID: 21300000, guid: 4bdb3728f88d2114ab8fc7c5a9010317, type: 3}
-  - {fileID: 21300000, guid: 61295d3934374a14ba911f2420f7c126, type: 3}
-  - {fileID: 21300000, guid: 54168cfbc51e85f4696b5f45e1ba5b50, type: 3}
-  - {fileID: 21300000, guid: 2df296d9113049342b9edc2376336898, type: 3}
-  - {fileID: 21300000, guid: 6a9c18ed31d5e754aa90cc1351277491, type: 3}
-  - {fileID: 21300000, guid: 73b719b9de8acaa438d3d5071d08e620, type: 3}
-  - {fileID: 21300000, guid: 47d9240343bc6454196561bafb8c4bdd, type: 3}
-  - {fileID: 21300000, guid: ff18133510ca2ad4ba65c99a310dd6b0, type: 3}
-  - {fileID: 21300000, guid: eea406334284eb6459223f32fd6a1c0b, type: 3}
-  allPerfectNumberSprites:
-  - {fileID: 21300000, guid: 9c422df76b3d1984985a79f3e67438dc, type: 3}
-  - {fileID: 21300000, guid: 4bdb3728f88d2114ab8fc7c5a9010317, type: 3}
-  - {fileID: 21300000, guid: 61295d3934374a14ba911f2420f7c126, type: 3}
-  - {fileID: 21300000, guid: 54168cfbc51e85f4696b5f45e1ba5b50, type: 3}
-  - {fileID: 21300000, guid: 2df296d9113049342b9edc2376336898, type: 3}
-  - {fileID: 21300000, guid: 6a9c18ed31d5e754aa90cc1351277491, type: 3}
-  - {fileID: 21300000, guid: 73b719b9de8acaa438d3d5071d08e620, type: 3}
-  - {fileID: 21300000, guid: 47d9240343bc6454196561bafb8c4bdd, type: 3}
-  - {fileID: 21300000, guid: ff18133510ca2ad4ba65c99a310dd6b0, type: 3}
-  - {fileID: 21300000, guid: eea406334284eb6459223f32fd6a1c0b, type: 3}
-  allPerfectEffectSprites: []
-  comboTextGraphic: {fileID: 0}
-  apEffectComboGraphic: {fileID: 0}
-  comboTextSprite: {fileID: 0}
-  allPerfectComboTextSprite: {fileID: 0}
--- HEAD 1291648749
+++ WORKTREE 1291648749
@@ -46,3 +46,3 @@
   m_SortingLayer: 0
-  m_SortingOrder: -99
+  m_SortingOrder: 51
   m_MaskInteraction: 0
--- HEAD 1349217677
+++ WORKTREE 1349217677
@@ -1,16 +1,4 @@
 GameObject:
-  m_ObjectHideFlags: 0
-  m_CorrespondingSourceObject: {fileID: 0}
-  m_PrefabInstance: {fileID: 0}
+  m_CorrespondingSourceObject: {fileID: 4132060024006901189, guid: 7b1b230022254d84aba981a41fab674b, type: 3}
+  m_PrefabInstance: {fileID: 6469529540597727820}
   m_PrefabAsset: {fileID: 0}
-  serializedVersion: 6
-  m_Component:
-  - component: {fileID: 1349217678}
-  - component: {fileID: 1349217680}
-  m_Layer: 5
-  m_Name: StartGamePanel
-  m_TagString: Untagged
-  m_Icon: {fileID: 0}
-  m_NavMeshLayer: 0
-  m_StaticEditorFlags: 0
-  m_IsActive: 0
--- HEAD 1604871499
+++ WORKTREE 1604871499
@@ -1,7 +1,6 @@
 MonoBehaviour:
-  m_ObjectHideFlags: 0
-  m_CorrespondingSourceObject: {fileID: 0}
-  m_PrefabInstance: {fileID: 0}
+  m_CorrespondingSourceObject: {fileID: 5545412174118010681, guid: ca2753fede37cd34ba109a8487359b6e, type: 3}
+  m_PrefabInstance: {fileID: 5599497664544700849}
   m_PrefabAsset: {fileID: 0}
-  m_GameObject: {fileID: 1604871497}
+  m_GameObject: {fileID: 0}
   m_Enabled: 1
@@ -11,7 +10 @@
   m_EditorClassIdentifier: Assembly-CSharp::FFrameWork.Live.ClearGaugeView
-  gaugeFillImage: {fileID: 1524503396}
-  clearMarker: {fileID: 0}
-  clearGaugeText: {fileID: 0}
-  pauseButton: {fileID: 0}
-  normalColor: {r: 0.2, g: 0.8, b: 1, a: 1}
-  clearColor: {r: 0.23584905, g: 0.9433962, b: 0.6315917, a: 1}
--- HEAD 1750966573
+++ WORKTREE 1750966573
@@ -46,6 +46,6 @@
   m_SortingLayer: 0
-  m_SortingOrder: -99
+  m_SortingOrder: 50
   m_MaskInteraction: 0
   m_Sprite: {fileID: 21300000, guid: f9e6006b654ef9c49ad4f4cbab828b7c, type: 3}
-  m_Color: {r: 0.24313727, g: 0.5294118, b: 0.9960785, a: 0.48235294}
+  m_Color: {r: 0.10377358, g: 0.22196019, b: 0.41509432, a: 0.48235294}
   m_FlipX: 0
--- HEAD 1872847406
+++ WORKTREE 1872847406
@@ -49,3 +49,3 @@
   m_Sprite: {fileID: 0}
-  m_Color: {r: 1, g: 1, b: 1, a: 1}
+  m_Color: {r: 1, g: 1, b: 1, a: 0.80784315}
   m_FlipX: 0
--- HEAD 2065161708
+++ WORKTREE 2065161708
@@ -49,3 +49,3 @@
   m_Sprite: {fileID: 21300000, guid: ac91b51dae3a10b47bea2d8eece8e1c5, type: 3}
-  m_Color: {r: 0.7830189, g: 0.7830189, b: 0.7830189, a: 1}
+  m_Color: {r: 0, g: 0.807, b: 0.811, a: 1}
   m_FlipX: 0
--- HEAD 2635950389464677582
+++ WORKTREE 2635950389464677582
@@ -91,2 +91,6 @@
       objectReference: {fileID: 0}
+    - target: {fileID: 3880795888521936056, guid: a52cd8023023e0f4f8b0ce3d74409342, type: 3}
+      propertyPath: m_IsActive
+      value: 1
+      objectReference: {fileID: 0}
     m_RemovedComponents: []
```

## Song source/runtime chart comparison

- Database: 12 entries, 11 unique GUIDs.
  - `Assets/Scripts/FFrameWork/Songs/Song4/Song4 Easy.asset`
  - `Assets/Scripts/FFrameWork/Songs/Song1/Song1 Easy.asset`
  - `Assets/Scripts/FFrameWork/Songs/Song1/Song1 Normal.asset`
  - `Assets/Scripts/FFrameWork/Songs/Song1/Song1 Hard.asset`
  - `Assets/Scripts/FFrameWork/Songs/Song2/Song2 Easy.asset`
  - `Assets/Scripts/FFrameWork/Songs/Song2/Song2 Normal.asset`
  - `Assets/Scripts/FFrameWork/Songs/Song2/Song2 Hard.asset`
  - `Assets/Scripts/FFrameWork/Songs/Song2/Song2 Extra.asset`
  - `Assets/Scripts/FFrameWork/Songs/Song3/Song3 Easy.asset` (DUPLICATE)
  - `Assets/Scripts/FFrameWork/Songs/Song3/Song3 Easy.asset` (DUPLICATE)
  - `Assets/Scripts/FFrameWork/Songs/Song3/Song3 Normal.asset`
  - `Assets/Scripts/FFrameWork/Songs/Song3/Song3 Hard.asset`
- `Song1 Easy.asset`: IDENTICAL; `Assets/Charts/Trance1/Easy.txt` -> `Assets/StreamingAssets/Charts/Trance1/Easy.txt`
- `Song1 Extra.asset`: IDENTICAL; `Assets/Charts/Trance1/Extra.txt` -> `Assets/StreamingAssets/Charts/Trance1/Extra.txt`
- `Song1 Hard.asset`: IDENTICAL; `Assets/Charts/Trance1/Hard.txt` -> `Assets/StreamingAssets/Charts/Trance1/Hard.txt`
- `Song1 Normal.asset`: IDENTICAL; `Assets/Charts/Trance1/Normal.txt` -> `Assets/StreamingAssets/Charts/Trance1/Normal.txt`
- `Song2 Easy.asset`: IDENTICAL; `Assets/Charts/Dream/Easy.txt` -> `Assets/StreamingAssets/Charts/Dream/Easy.txt`
- `Song2 Extra.asset`: IDENTICAL; `Assets/Charts/Dream(Original Mix)/Extra.txt` -> `Assets/StreamingAssets/Charts/Dream(Original Mix)/Extra.txt`
- `Song2 Hard.asset`: IDENTICAL; `Assets/Charts/Dream/Hard.txt` -> `Assets/StreamingAssets/Charts/Dream/Hard.txt`
- `Song2 Normal.asset`: IDENTICAL; `Assets/Charts/Dream/Normal.txt` -> `Assets/StreamingAssets/Charts/Dream/Normal.txt`
- `Song3 Easy.asset`: IDENTICAL; `Assets/Charts/C_Match/Easy.txt` -> `Assets/StreamingAssets/Charts/C_Match/Easy.txt`
- `Song3 Hard.asset`: IDENTICAL; `Assets/Charts/C_Match/Hard.txt` -> `Assets/StreamingAssets/Charts/C_Match/Hard.txt`
- `Song3 Normal.asset`: IDENTICAL; `Assets/Charts/C_Match/Normal.txt` -> `Assets/StreamingAssets/Charts/C_Match/Normal.txt`
- `Song4 Easy.asset`: IDENTICAL; `Assets/Charts/NITO/Easy.txt` -> `Assets/StreamingAssets/Charts/NITO/Easy.txt`
