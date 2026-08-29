# テストガイド

このプロジェクトには、Unity表示に依存しないEditModeテストと、Scene・View・入力を含むPlayModeテストがあります。

テスト名と件数の一次情報源は**テストコード自身**です。この文書は「何を検証する方針か」と「どう実行するか」を扱います。

## 1. EditModeテスト

[`Assets/Tests/EditMode/GameSessionTests.cs`](../Assets/Tests/EditMode/GameSessionTests.cs)はUnity表示に依存せず、Coreの状態とルールを検証します。

検証方針:

- 標準定義での盤面サイズ、駒数、初期配置、初期移動プロファイル
- 移動プロファイルによる戦闘力別方向制限と、戦闘力変更後の即時反映
- プロファイル帯域の境界値、隙間・重複、未登録IDの拒否
- 正常な移動と手番交代
- 不正なプレイヤー、距離、自陣進入の拒否と、拒否時にSnapshotが変わらないこと
- 戦闘の3パターン（相打ち、攻撃側生存、防御側生存）と残り戦闘力
- 相手陣地到達での勝利と、勝敗確定後のCommand拒否
- 相手の全滅が勝利にならないことと自動パス
- 両者行動不能での引き分け
- 隣接する自駒が合体を試行でき、成功・失敗のどちらでも手番を消費すること
- セル効果の実行順と累積
- 過去のSnapshotが後続のCommandで変化しないこと
- リセットによる完全復元

### 既存のテストヘルパー

新しいEditModeテストを書くときは、`GameSessionTests`が持つ次のヘルパーを再利用します。盤面生成やDefinition組み立てを書き直す必要はありません。

| ヘルパー | 用途 |
|---|---|
| `CreateSession(firstPlayer, params pieces)` | 標準プロファイルの6×10盤面でSessionを作る |
| `CreateDefinition(firstPlayer, cellEffects, params pieces)` | セル効果を指定した`GameDefinition`を作る |
| `CreateDefinitionWithProfiles(firstPlayer, profiles, cellEffects, params pieces)` | 独自の移動プロファイルを差し込む |
| `InitialPiece(id, column, row, owner, power, movementProfileId)` | 初期駒定義を1行で書く。戦闘力とプロファイルIDは省略可 |
| `GetPiece(snapshot, position)` | 位置を指定して駒を取得する |
| `AssertPiece(snapshot, position, owner, combatPower)` | 位置・所有者・戦闘力をまとめて検証する |
| `RecordingPowerEffect` | 呼び出し順を記録するテスト用`ICellEffectHandler` |

これらは`private static`なので、別のテストクラスを追加する場合は同等のヘルパーをそちらにも用意するか、共有するヘルパークラスへ切り出してください。

## 2. PlayModeテスト

[`Assets/Tests/PlayMode/BoardGameBootstrapTests.cs`](../Assets/Tests/PlayMode/BoardGameBootstrapTests.cs)はBootstrap、View、HUD、Scene統合を検証します。

検証方針:

- Bootstrapが盤面セル、駒View、陣地、HUDを分離して構築すること
- 所有者色と戦闘力の表示
- 手番プレイヤーだけが選択でき、合法手が強調されること
- 1操作で1Commandだけ実行され、Viewと手番が更新されること
- 相打ち時に両方の駒Viewが削除されること
- 起動時の`TitleScene`表示と、ゲーム開始による新規ゲーム生成
- 勝利・引き分けのリザルト表示と、表示中の盤面入力遮断
- リザルトから`TitleScene`へ戻れること
- リセットボタンによる状態とViewの復元
- `SampleScene`がBootstrap、単一の`BoardGameAudioManager`、EventSystem、BGM／SFX用AudioSourceを生成すること
- BGM／SFXスライダーがAudioManagerとAudioSourceの音量へ反映されること

## 3. Unity Test Runnerで実行する

1. Unity Editorでプロジェクトを開きます。
2. `Window > General > Test Runner`を開きます。
3. EditModeタブで`Run All`を実行します。
4. PlayModeタブで`Run All`を実行します。
5. 失敗が0件であることを確認します。

PlayMode実行中は一時Sceneが生成・破棄されます。Test Runnerが停止した場合は、Play Modeを終了してから再実行してください。

## 4. Windowsバッチモードで実行する

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

## 5. 手動スモークテスト

- [ ] Game Viewを16:9にして盤面全体と両陣地ラベルが表示される
- [ ] 起動時にタイトルと「ゲーム開始」が表示され、ゲーム本体へ遷移できる
- [ ] 60マス、青6駒、赤6駒が表示される
- [ ] 全駒の初期戦闘力が1と表示される
- [ ] 手番中の自駒だけ選択できる
- [ ] 同じ駒で選択解除、別の自駒で選択変更できる
- [ ] 空き合法手が緑、敵駒の合法手がオレンジになる
- [ ] 8方向へ1マスだけ動ける
- [ ] 自陣、盤外、自駒上へ移動できない
- [ ] 同戦闘力の衝突で両駒が消滅する
- [ ] 相手陣地到達でリザルトが表示され、その後の盤面操作が止まる
- [ ] 「スタート画面に戻る」でBGMが停止し、タイトルへ戻る
- [ ] リセットで12駒、先手、選択、候補、勝敗が復元される
- [ ] リセットボタン押下で背後の盤面が反応しない
- [ ] 通常再生中のConsole Error・Warningが0件

## 6. 新機能に必要なテスト

- Coreのルール変更には、成功・拒否・境界値・状態不変性のEditModeテストを追加します。
- 入力、Prefab、HUD、演出変更にはPlayModeテストを追加します。
- 新しいCommandは、手番外、所有権違反、ゲーム終了後、無効な対象を検証します。
- 新しいEventは、発生条件、値、順序、Viewへの反映を検証します。
- Config項目を増やした場合は、標準値と無効値の検証を追加します。
- Core標準定義、Config既定値、実際のAssetに同じ設定がある場合は、それらの整合性を検証します。
- Rule、Resolver、Handler、Agentを差し替えた場合は、Bootstrapから注入した実装が標準Sceneで使われることをPlayModeで検証します。

## 7. 現在の未検証領域

- 具体的な合体Resolverと合体UI
- 実ゲームで使用する特殊マスHandler
- CPU Agentと非同期思考
- 実機のタッチデバイス入力
- 複数解像度・縦長画面の網羅的な表示確認
- Standalone PlayerのBuildと実行
- CI上での自動テスト

現行PlayModeテストは`HandleCellClick`からの操作経路を検証していますが、Input Systemへ物理Mouse・Touchイベントを注入するテストではありません。実機入力は手動確認が必要です。

## 8. PR前の検証

機能変更では[拡張ガイドの変更影響マトリクス](EXTENSION_GUIDE.md#変更影響マトリクス)を確認し、対象となるCore、Bootstrap、Config・Asset、テスト、文書が同じPRに揃っていることを確認します。

- EditModeとPlayModeの必要なテストが成功している。
- Config使用時とCore Fallbackで標準設定が一致している。
- 差し替えた実装がBootstrapから本番経路へ注入されている。
- Markdownの相対リンクと見出しリンクがGitHub Previewで開ける。
- `git status --short`に意図しないScene、Package、ProjectSettings、生成ファイルがない。
- `git diff --check`が成功する。

```powershell
git status --short
git diff --check
```

文書だけを変更した場合はUnity Test Runnerの再実行を必須としません。ただし、文書中の型名、メソッド名、コンストラクタ引数を現行ソースと照合し、変更したリンクを確認します。
