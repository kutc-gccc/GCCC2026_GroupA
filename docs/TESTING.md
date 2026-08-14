# テストガイド

## 1. 現在のテスト結果

Unity `6000.3.11f1`での最新ローカル確認結果です。

| Suite | 件数 | 結果 |
|---|---:|---|
| EditMode | 15 | 15成功、0失敗 |
| PlayMode | 8 | 8成功、0失敗 |
| SampleScene通常再生 | 1 | Console Error・Warning 0件 |

## 2. EditModeテスト

`Assets/Tests/EditMode/GameSessionTests.cs`はUnity表示に依存せず、Coreの状態とルールを検証します。

| テスト | 検証内容 |
|---|---|
| `StandardGameStartsWithTwelveDirectionalPiecesOutsideTerritories` | 6×10、12駒、陣地が空、初期方向 |
| `PieceMoveDirectionsRestrictLegalCommands` | 駒ごとの方向制限 |
| `ValidMoveChangesPositionAndSwitchesTurn` | 正常移動と手番交代 |
| `InvalidPlayerAndDestinationAreRejectedWithoutChangingSnapshot` | 不正プレイヤー・2マス移動の拒否 |
| `PlayersCannotMoveIntoTheirOwnTerritory` | 自陣進入禁止 |
| `EqualCombatPowerDestroysBothPieces` | 同戦闘力の相打ち |
| `StrongerAttackerMovesWithRemainingPower` | 攻撃側生存と残り戦闘力 |
| `StrongerDefenderStaysWithRemainingPower` | 防御側生存と残り戦闘力 |
| `ReachingOpponentTerritoryWinsAndLocksCommands` | 到達勝利と終了後拒否 |
| `DefeatingEveryOpponentDoesNotWinAndPassesTurnBack` | 全滅が勝利ではないこととパス |
| `NoLegalActionsForEitherPlayerIsDraw` | 両者行動不能の引き分け |
| `FusionCommandIsExplicitlyRejectedWhileFeatureIsDisabled` | 合体無効の失敗理由 |
| `CellEffectsRunInDefinitionOrder` | 特殊効果の実行順と累積 |
| `OldSnapshotDoesNotChangeAfterExecutingACommand` | Snapshotの不変性 |
| `ResetRestoresStandardPositionAndFirstTurn` | リセットの完全復元 |

## 3. PlayModeテスト

`Assets/Tests/PlayMode/BoardGameBootstrapTests.cs`はBootstrap、View、HUD、Scene統合を検証します。

| テスト | 検証内容 |
|---|---|
| `AwakeBuildsSeparatedBoardPiecesTerritoriesAndHud` | 60セル、12駒、分割View、陣地、HUD |
| `PieceViewsRenderOwnersAndCombatPower` | 青・赤の所有者色と戦闘力表示 |
| `OnlyCurrentPlayerCanSelectAndLegalMovesAreHighlighted` | 選択制限、解除、候補表示 |
| `ValidInputExecutesOneCommandAndUpdatesViews` | 1操作1Command、Viewと手番更新 |
| `EqualCombatPowerCollisionRemovesBothPieceViews` | 相打ち時の状態とView削除 |
| `ReachingOpponentTerritoryWinsAndLocksInput` | 到達勝利、HUD、入力停止 |
| `ResetButtonRestoresInitialStateAndViews` | UIリセット後の状態と表示 |
| `SampleSceneLoadsWithBootstrapOnlyCompositionRoot` | SampleSceneとBootstrap統合 |

## 4. Unity Test Runnerで実行する

1. Unity Editorでプロジェクトを開きます。
2. `Window > General > Test Runner`を開きます。
3. EditModeタブで`Run All`を実行します。
4. PlayModeタブで`Run All`を実行します。
5. 失敗が0件であることを確認します。

PlayMode実行中は一時Sceneが生成・破棄されます。Test Runnerが停止した場合は、Play Modeを終了してから再実行してください。

## 5. Windowsバッチモードで実行する

同じプロジェクトを開いているUnity Editorを先に閉じます。プロジェクトルートのPowerShellで次を実行します。

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe'
$projectRoot = (Get-Location).Path

& $unityEditor `
  -batchmode `
  -nographics `
  -projectPath $projectRoot `
  -runTests `
  -testPlatform editmode `
  -testResults "$projectRoot\Temp\EditModeResults.xml" `
  -logFile "$projectRoot\Temp\EditModeTests.log"

& $unityEditor `
  -batchmode `
  -nographics `
  -projectPath $projectRoot `
  -runTests `
  -testPlatform playmode `
  -testResults "$projectRoot\Temp\PlayModeResults.xml" `
  -logFile "$projectRoot\Temp\PlayModeTests.log"
```

成功件数はXMLの`total`、`passed`、`failed`で確認します。コンパイルエラーはlogを検索します。

```powershell
Select-String -Path Temp\*ModeTests.log -Pattern 'error CS|Unhandled|Exception'
```

Test Runnerの結果保存を知らせるUnity内部ログが表示される場合があります。最終判定はTest Result XMLの`result="Passed"`、失敗件数0、通常再生時Console Error 0の組み合わせで行います。

## 6. 手動スモークテスト

- [ ] Game Viewを16:9にして盤面全体と両陣地ラベルが表示される
- [ ] 60マス、青6駒、赤6駒が表示される
- [ ] 全駒の初期戦闘力が1と表示される
- [ ] 手番中の自駒だけ選択できる
- [ ] 同じ駒で選択解除、別の自駒で選択変更できる
- [ ] 空き合法手が緑、敵駒の合法手がオレンジになる
- [ ] 8方向へ1マスだけ動ける
- [ ] 自陣、盤外、自駒上へ移動できない
- [ ] 同戦闘力の衝突で両駒が消滅する
- [ ] 相手陣地到達で勝利表示になり、その後の盤面操作が止まる
- [ ] リセットで12駒、先手、選択、候補、勝敗が復元される
- [ ] リセットボタン押下で背後の盤面が反応しない
- [ ] 通常再生中のConsole Error・Warningが0件

## 7. 新機能に必要なテスト

- Coreのルール変更には、成功・拒否・境界値・状態不変性のEditModeテストを追加します。
- 入力、Prefab、HUD、演出変更にはPlayModeテストを追加します。
- 新しいCommandは、手番外、所有権違反、ゲーム終了後、無効な対象を検証します。
- 新しいEventは、発生条件、値、順序、Viewへの反映を検証します。
- Config項目を増やした場合は、標準値と無効値の検証を追加します。

## 8. 現在の未検証領域

- 具体的な合体Resolverと合体UI
- 実ゲームで使用する特殊マスHandler
- CPU Agentと非同期思考
- 実機のタッチデバイス入力
- 複数解像度・縦長画面の網羅的な表示確認
- Standalone PlayerのBuildと実行
- CI上での自動テスト

現行PlayModeテストは`HandleCellClick`からの操作経路を検証していますが、Input Systemへ物理Mouse・Touchイベントを注入するテストではありません。実機入力は手動確認が必要です。
