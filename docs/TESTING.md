# テストガイド

このプロジェクトには、Unity表示に依存しないEditModeテストと、Scene・View・入力を含むPlayModeテストがあります。

テスト名と件数の一次情報源は**テストコード自身**です。この文書は「何を検証する方針か」と「どう実行するか」を扱います。

## 1. EditModeテスト

EditModeテストは責務別に分割しています。共通fixtureは[`GameSessionTests.cs`](../Assets/Tests/EditMode/GameSessionTests.cs)、盤面生成は[`GameSessionTestBuilder.cs`](../Assets/Tests/EditMode/GameSessionTestBuilder.cs)へ集約し、移動・戦闘・合体・セル効果・リザーブ・状態復元をそれぞれ`GameSession*Tests.cs`で検証します。[`GameSessionCachingTests.cs`](../Assets/Tests/EditMode/GameSessionCachingTests.cs)は合法手キャッシュと`WithLegalCommands`の不変データ共有を独立して検証します。

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
- 注入乱数による戦闘力ランダム化と手番消費
- 滞在中効果の退出解除、ダメージ優先消費、再進入時の回復
- 永続効果の一度だけの適用とランダム化拒否
- リザーブ追加、所有駒上限6、両プレイヤーの配置範囲、リセット、合体時の効果履歴継承
- セル効果の実行順、累積、Lifetime混在拒否
- 過去のSnapshotが後続のCommandで変化しないこと
- リセットによる完全復元

### 共通Session Builder

新しいEditModeテストを書くときは、`GameSessionTestBuilder`へ集約した次のヘルパーを再利用します。盤面生成やDefinition組み立てを書き直す必要はありません。

| ヘルパー | 用途 |
|---|---|
| `CreateSession(firstPlayer, params pieces)` | 標準プロファイルの6×10盤面でSessionを作る |
| `CreateDefinition(firstPlayer, cellEffects, params pieces)` | セル効果を指定した`GameDefinition`を作る |
| `CreateDefinitionWithProfiles(firstPlayer, profiles, cellEffects, params pieces)` | 独自の移動プロファイルを差し込む |
| `CreateDefinitionWithEffects(firstPlayer, cellEffects, definitions, params pieces)` | Lifetimeを指定した特殊マス定義を作る |
| `CreateDefinitionWithEffectsAndLimits(firstPlayer, cellEffects, definitions, maxPiecesPerPlayer, reserveDeploymentDepth, params pieces)` | 駒上限とリザーブ配置範囲を指定した定義を作る |
| `InitialPiece(id, column, row, owner, power, movementProfileId)` | 初期駒定義を1行で書く。戦闘力とプロファイルIDは省略可 |
| `GetPiece(snapshot, position)` | 位置を指定して駒を取得する |
| `AssertPiece(snapshot, position, owner, combatPower)` | 位置・所有者・戦闘力をまとめて検証する |
| `RecordingPowerEffect` | 呼び出し順を記録するテスト用`ICellEffectHandler` |

`GameSessionTests`のpartial fixtureからは薄いラッパー経由で利用し、別fixtureからは`GameSessionTestBuilder`を直接利用できます。表示固有の`GetPiece`／`AssertPiece`とテスト用fakeは共通fixtureに残します。

## 2. PlayModeテスト

PlayModeテストは共通fixtureを[`BoardGameBootstrapTests.cs`](../Assets/Tests/PlayMode/BoardGameBootstrapTests.cs)に置き、Board、HUD、入力、Bootstrap／Scene、Audio、遊び方を`BoardGame*Tests.cs`へ分割しています。[`PresentationRefactoringTests.cs`](../Assets/Tests/PlayMode/PresentationRefactoringTests.cs)は任意陣地行、Piece reconcile、Interaction State、Config検証、Audio Resolverを独立して検証します。

検証方針:

- Bootstrapが盤面セル、駒View、陣地、HUDを分離して構築すること
- 所有者ごとの駒スプライト（向き）と戦闘力の表示
- 手番プレイヤーだけが選択でき、合法手が強調されること
- 1操作で1Commandだけ実行され、Viewと手番が更新されること
- 相打ち時に両方の駒Viewが削除されること
- 起動時の`TitleScene`表示、遊び方ページの表示・復帰、ゲーム開始による新規ゲーム生成
- 勝利・引き分けのリザルト表示と、表示中の盤面入力遮断
- リザルトから`TitleScene`へ戻れること
- リセットボタンによる状態とViewの復元
- `SampleScene`がBootstrap、単一の`BoardGameAudioManager`、EventSystem、BGM／SFX用AudioSourceを生成すること
- BGM／SFXスライダーがAudioManagerとAudioSourceの音量へ反映されること
- 特殊マスのオーバーレイ、凡例、リザーブカードの個数・Sprite・戦闘力・移動プロファイルがSnapshotどおりに表示されること
- 手番側カードだけの有効化、先頭以外の個別選択・解除・選択変更、リザーブ配置候補、配置後のカード削除と駒View生成
- リザーブパネル上のクリックが盤面入力へ貫通せず、勝敗確定後は全カードが操作不能になること
- ランダム化ボタンが対象駒の選択中だけ有効になること
- 任意の陣地行でもSnapshotどおりに外周が描かれること
- 一時効果中の駒が`EffectiveCombatPower`を表示すること
- 変更のない`PieceView`インスタンスがreconcile後も保持されること
- HUDを再初期化してもListenerとUI階層が重複しないこと
- Configのnull、重複、範囲外、最小行数、未登録移動プロファイルを明確に拒否すること
- Audio ResolverがEvent列の再生順を保つこと

### 遊び方の既存テスト

[`BoardGameHowToPlayTests.cs`](../Assets/Tests/PlayMode/BoardGameHowToPlayTests.cs)には次のテストがあります。テストの存在と実行済みであることは別なので、実行結果はその都度Test Runnerで確認します。

| テスト名 | 検証内容 |
|---|---|
| `HowToPageBuildsEverySectionAndSwitchesBetweenThem` | 各節の生成、ナビでの切り替え |
| `HowToPageReturnsToFirstSectionWhenReopened` | 開き直したときの先頭節への復帰 |
| `HowToPageSectionsFitInsideTheContentArea` | 生成された要素の表示領域への収まり |
| `HowToPlayViewStopsWithAnErrorWhenReferencesAreMissing` | 必須参照不足時のエラーと生成停止 |
| `HowToPageUsesTheSameWordsAsTheGameScreen` | 操作ボタン名・凡例の用語の一致 |

タイトルからの接続は[`BoardGameBootstrapSceneTests.cs`](../Assets/Tests/PlayMode/BoardGameBootstrapSceneTests.cs)の`TitleSceneShowsHowToAndStartsFreshGame`が担当します。用語一致テストだけで説明文のルール上の正しさを保証するわけではありません。方向図とCore標準プロファイル、合体条件、特殊マスの獲得条件はソースと実画面でも照合します。

## 3. Unity Test Runnerで実行する

1. Unity Editorでプロジェクトを開きます。
2. `Window > General > Test Runner`を開きます。
3. Core変更はEditMode、表示・入力・Scene変更はPlayModeで該当テストを選んで実行します。
4. 両層にまたがる変更では両方を実行します。全体の回帰確認が必要なときに`Run All`を使います。
5. 失敗が0件であることを確認します。

PlayMode実行中は一時Sceneが生成・破棄されます。Test Runnerが停止した場合は、Play Modeを終了してから再実行してください。

## 4. Windowsバッチモードで実行する

同じプロジェクトを開いているUnity Editorを先に閉じます。開いたままだとプロジェクトが排他ロックされ、バッチモードは起動できません。

**結果の出力先はプロジェクトの`Temp`以外にします。** Unityは終了時に`Temp`を消去するため、`Temp`へ保存すると実行後にXMLとlogが残りません。ここではOSの一時フォルダを使います。

プロジェクトルートのPowerShellで次を実行します。

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe'
$projectRoot = (Get-Location).Path
$output = Join-Path $env:TEMP 'GCCC-Tests'
New-Item -ItemType Directory -Force $output | Out-Null

& $unityEditor `
  -batchmode `
  -nographics `
  -projectPath $projectRoot `
  -runTests `
  -testPlatform editmode `
  -testResults "$output\EditModeResults.xml" `
  -logFile "$output\EditModeTests.log"

& $unityEditor `
  -batchmode `
  -nographics `
  -projectPath $projectRoot `
  -runTests `
  -testPlatform playmode `
  -testResults "$output\PlayModeResults.xml" `
  -logFile "$output\PlayModeTests.log"
```

成功件数はXMLの`total`、`passed`、`failed`で確認します。コンパイルエラーはlogを検索します。

```powershell
Select-String -Path "$output\*ModeResults.xml" -Pattern '<test-run .*result='
Select-String -Path "$output\*ModeTests.log" -Pattern 'error CS|Unhandled|Exception'
```

`-runTests`の終了コードは、全件成功で`0`、1件でも失敗すると`2`になります。

Test Runnerの結果保存を知らせるUnity内部ログが表示される場合があります。最終判定はTest Result XMLの`result="Passed"`、失敗件数0、通常再生時Console Error 0の組み合わせで行います。

## 5. 手動スモークテスト

- [ ] Game Viewを16:9にして盤面全体と両陣地ラベルが表示される
- [ ] 起動時にタイトル、「ゲーム開始」、「遊び方」が表示される
- [ ] 大理石背景を隠さず、濃紺のSerifタイトルと副題「6×10 の陣地到達型ボードゲーム」が読める
- [ ] 「ゲーム開始」が塗りの主操作、「遊び方」が線のみの副操作として判別できる
- [ ] 「遊び方」で説明ページへ切り替わり、「戻る」でタイトルへ復帰できる
- [ ] 左ナビの「勝ち方／自分の駒／1手の行動／動ける向き／戦闘／あとで」がすべて表示され、選択した節だけが本文に出る
- [ ] 第6節から閉じて開き直すと第1節へ戻り、ナビや本文が重複しない
- [ ] 全6節の見出し・本文・図・注記がパネル内に収まり、ゲーム画面と操作名・▲▼・点・枠の意味が一致する
- [ ] 「ゲーム開始」でゲーム本体へ遷移できる
- [ ] 60マス、上向き▲6駒、下向き▼6駒が表示される
- [ ] 全駒の初期戦闘力が1と表示される
- [ ] 手番中の自駒だけ選択できる
- [ ] 同じ駒で選択解除、別の自駒で選択変更できる
- [ ] 選択中が琥珀の枠、空き合法手が白い点、敵駒が赤い枠、合体候補が青い枠になる
- [ ] 左側の凡例に、選択・移動・戦闘・合体・永続効果・滞在中効果の6項目が盤面と同じ形で表示される
- [ ] 選択中の合法な自駒だけ「パワーランダム化」が有効になり、1〜3への変更後に手番が進む
- [ ] 隣接する自駒で「合体」が有効になり、青い候補の選択後に結果表示と手番更新が行われる
- [ ] 戦闘力1の駒が、占有・陣地などの制約を除き8方向へ1マス動ける。戦闘力変更後は対応する方向に変わる
- [ ] 自陣、盤外、自駒上へ移動できない
- [ ] 同戦闘力の衝突で両駒が消滅する
- [ ] 相手陣地到達でリザルトが表示され、その後の盤面操作が止まる
- [ ] 「スタート画面に戻る」でBGMが停止し、タイトルへ戻る
- [ ] BGM／SFXスライダーが音量へ反映され、UI操作が盤面へ伝播しない
- [ ] 右側のリザーブ見出しに両プレイヤーの`駒 n / 6`が常時表示される
- [ ] リセットで12駒、先手、選択、候補、勝敗が復元される
- [ ] リセットが右下に単独配置され、右上の通常操作ボタンと隣接しない
- [ ] リセットが透明背景・赤枠・赤文字で、ほかの操作と区別できる
- [ ] リセットボタン押下で背後の盤面が反応しない
- [ ] 通常再生中にゲーム由来のConsole Errorがない。Warningは内容と発生元を確認し、未解決事項を記録する

実画面の基準画像と2026-09-02の確認範囲は[画面・操作ガイド §6](SCREEN_GUIDE.md#6-確認記録と更新方法)にあります。画像を更新する際は、実際のGame View全体を16:9で撮影し、HUDを含めます。カメラだけの描画ではScreen Space OverlayのUIが欠落するため、画面合成後のキャプチャを使用してください。

## 6. 新機能に必要なテスト

- Coreのルール変更には、成功・拒否・境界値・状態不変性のEditModeテストを追加します。
- 入力、Prefab、HUD、演出変更にはPlayModeテストを追加します。
- 新しいCommandは、手番外、所有権違反、ゲーム終了後、無効な対象を検証します。
- 新しいEventは、発生条件、値、順序、Viewへの反映を検証します。
- Config項目を増やした場合は、標準値と無効値の検証を追加します。
- Core標準定義、Config既定値、実際のAssetに同じ設定がある場合は、それらの整合性を検証します。
- Rule、Resolver、Handler、Agentを差し替えた場合は、Bootstrapから注入した実装が標準Sceneで使われることをPlayModeで検証します。

## 7. 現在の未検証領域

- 特殊マス配置を含む長時間プレイでのゲームバランス
- CPU Agentと非同期思考
- 実機のタッチデバイス入力
- 縦長画面を含む全アスペクト比の網羅的な表示確認
- Standalone PlayerのBuildと実行
- CI上での自動テスト

現行PlayModeテストは`HandleCellClick`からの操作経路を検証していますが、Input Systemへ物理Mouse・Touchイベントを注入するテストではありません。実機入力は手動確認が必要です。

## 8. PR前の検証

機能変更では[拡張ガイドの変更影響マトリクス](EXTENSION_GUIDE.md#変更影響マトリクス)を確認し、対象となるCore、Bootstrap、Config・Asset、テスト、文書が同じPRに揃っていることを確認します。

- EditModeとPlayModeの必要なテストが成功している。
- ConfigとCore Fallbackが共有する標準移動プロファイル・基本値が一致している。特殊マスは標準Assetだけが持つ差分として確認する。
- 差し替えた実装がBootstrapから本番経路へ注入されている。
- Markdownの相対リンクと見出しリンクがGitHub Previewで開ける。
- `git status --short`に意図しないScene、Package、ProjectSettings、生成ファイルがない。
- `git diff --check`が成功する。

```powershell
git status --short
git diff --check
```

文書だけを変更した場合はUnity Test Runnerの再実行を必須としません。ただし、文書中の型名、メソッド名、コンストラクタ引数を現行ソースと照合し、変更したリンクを確認します。
