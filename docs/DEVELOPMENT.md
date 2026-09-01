# 開発ガイド

## 1. 開発環境

| 項目 | 値 |
|---|---|
| Unity | `6000.3.11f1` |
| Render Pipeline | Universal Render Pipeline `17.3.0`、2D Renderer |
| Input | Input System `1.19.0` |
| UI | uGUI `2.0.0` |
| Test | Unity Test Framework `1.6.0` |
| Build Scenes | `TitleScene`（起動）、`SampleScene`（ゲーム本体） |

Unity Hubで同じEditorバージョンをインストールしてください。異なるUnityバージョンで開くとScene、Prefab、ProjectSettingsが自動更新され、意図しない差分が発生する可能性があります。

## 2. プロジェクトの取得と起動

```powershell
git clone https://github.com/kutc-gccc/GCCC2026_GroupA
Set-Location GCCC2026_GroupA
```

1. Unity Hubの「Add project from disk」でプロジェクトルートを選びます。
2. Unity `6000.3.11f1`で開き、Packageの解決とコンパイル完了を待ちます。
3. ConsoleのErrorが0件であることを確認します。
4. `Assets/Scenes/TitleScene.unity`を開いてPlayし、「ゲーム開始」を押します。

Editorメニューの`GCCC > タイトルから再生`でも起動できます。通常のPlayは現在開いているSceneから始まるため、Build Settingsの先頭がタイトルでも自動でタイトルへ切り替わるわけではありません。未保存Sceneは保存・破棄を自分で判断してから切り替えてください。

初回起動時に生成される`Library`、`Temp`、`Logs`、`UserSettings`はGit管理しません。

## 3. 操作確認

1. Game Viewを16:9にし、タイトルと「ゲーム開始」「遊び方」が表示されることを確認します。「遊び方」の6節を左ナビで切り替え、「戻る」で閉じて開き直すと先頭節になることも確認します。
2. 「ゲーム開始」で`SampleScene`へ移動します。
3. プレイヤー1の上向き▲の駒を選択し、琥珀の選択枠と白い点の移動候補を確認して移動します。
4. 「パワーランダム化」で選択駒の通常戦闘力が1〜3に設定され、結果が同じ値でも手番を消費することを確認します。
5. 隣接する自駒を選び、「合体」から青表示の候補を選ぶと成功・大成功・失敗の結果が表示され、手番を消費することを確認します。
6. 赤い枠の敵駒マスへ移動すると戦闘が発生することを確認します。
7. 移動・戦闘・破壊・合体で対応する効果音が再生され、BGM／SFXスライダーが音量へ反映されることを確認します。
8. シアン色の戦闘力アップマス`(2,2)(3,7)`に駒が到達すると、駒の戦闘力が+2され、退出時に元に戻ることを確認します。
9. 戦闘・合体で所有駒を6個未満にしてから、未獲得の駒を紫色のリザーブ獲得マス`(1,4)(4,5)`へ進め、戦闘力1の駒がリザーブへ追加されることを確認します。初期状態は上限6個のため獲得できません。
10. 獲得した駒が右側のリザーブ一覧へ表示され、手番側のカードだけを選んで白い点の候補マスへ配置できることを確認します。
11. リセットで12駒、リザーブと駒の効果履歴が空、シアン色2個・紫色2個の特殊マス、プレイヤー1の手番へ戻ることを確認します。
12. 勝敗確定後にリザルトが表示され、背後の操作、リザーブカード、音量スライダーが無効になることを確認します。
13. 「スタート画面に戻る」でBGMが停止し、タイトルへ戻ることを確認します。

実画面と確認済み範囲は[画面・操作ガイド](SCREEN_GUIDE.md)を参照してください。音量は左下、リセットは右下にあります。

## 4. フォルダと担当境界

| 領域 | 主な変更場所 | 担当例 |
|---|---|---|
| 状態・共通契約 | `Core/Model`、`Core/Commands`、`Core/Events` | Core担当 |
| 移動 | `Core/Rules/Movement` | 移動担当 |
| 戦闘 | `Core/Rules/Combat` | 戦闘担当 |
| 合体 | `Core/Rules/Fusion` | 合体担当 |
| 特殊マス | `Core/Rules/CellEffects`、Config | 盤面担当 |
| 入力 | `Presentation/Input`、Coordinator | 入力担当 |
| 表示 | `Presentation/Views`、Prefab | UI担当 |
| 音声 | `Presentation/Audio`、`Resources/Audio` | 音声担当 |
| Scene遷移 | `Presentation/Bootstrap`、`Scenes` | UI・統合担当 |
| CPU | 将来の`AI`アセンブリ | AI担当 |
| 統合 | Bootstrap、Scene、asmdef | 担当者間で調整 |

`GameSession`、`GameCoordinator`、`BoardGameBootstrap`、asmdef、`SampleScene`は複数領域を接続するため、同時編集の衝突が起きやすいファイルです。必要な統合変更を小さくし、担当PRの最後に追加してください。

## 5. `StandardBoardGameConfig`

設定アセットは`Assets/Config/BoardGame/StandardBoardGameConfig.asset`です。

| 設定 | 標準値 | 用途 |
|---|---:|---|
| `columns` | 6 | 盤面の列数 |
| `rows` | 10 | 盤面の行数 |
| `firstPlayer` | Player1 | 先手 |
| `player1TerritoryRow` | 0 | プレイヤー1の陣地行 |
| `player2TerritoryRow` | 9 | プレイヤー2の陣地行 |
| `player1StartRow` | 1 | プレイヤー1の初期配置行 |
| `player2StartRow` | 8 | プレイヤー2の初期配置行 |
| `initialCombatPower` | 1 | 全初期駒の戦闘力 |
| `maxPiecesPerPlayer` | 6 | 盤上とリザーブを合わせたプレイヤーごとの所有駒上限 |
| `reserveDeploymentDepth` | 2 | 自陣行から前方へリザーブ配置を許可する行数 |
| `initialMovementProfileId` | standard | 全初期駒が参照する移動プロファイルID |
| `movementProfiles` | standard 1件 | 戦闘力範囲と移動方向の対応表 |
| `cellEffects` | `(1,4)(4,5)`に`reserve-piece-grant`、`(2,2)(3,7)`に`combat-power-boost` | 座標ごとの効果ID |
| `cellEffectDefinitions` | リザーブ獲得・戦闘力アップの2件 | 効果定義とHandlerを生成する`CellEffectConfig` Asset |

行設定は盤面内でなければなりません。両陣地を同じ行にする、自分の陣地と初期配置を同じ行にする、両プレイヤーの初期配置を同じ行にすると、`CreateDefinition`が`InvalidOperationException`を送出します。

各`movementProfiles`は戦闘力1から`int.MaxValue`までを隙間・重複なく覆う必要があります。`initialMovementProfileId`が未登録、IDが重複、帯域が不連続の場合はCore定義の生成時に例外になります。標準設定の正確な方向表は[ゲームルール §5](GAME_RULES.md#5-移動ルール)を参照してください。

この表は標準Sceneが参照するAssetの値です。Coreの`GameDefinition.CreateStandard()`は特殊マス・効果定義を持たないため、Config未設定時のFallbackは標準Sceneと同じ特殊マスを生成しません。

特殊効果を追加する場合は、`CombatPowerBoostEffectConfig`または`ReservePieceGrantEffectConfig` Assetを作り、`cellEffectDefinitions`へ登録してから対象座標の`cellEffects`へ同じIDを設定します。`BoardGameBootstrap`が定義とHandlerを同時に生成します。異なるLifetimeを同じセルに設定する、未登録IDを参照する、リザーブ獲得へ`WhileOccupied`を指定すると生成時に例外になります。

## 6. SceneとPrefabの編集

- `TitleScene`にはタイトルUIとScene遷移用Controllerを置きます。
- 遊び方の固定パネルと「戻る」は`TitleScene`、6節の文言・図解データは`HowToPlayContent`、ナビと本文・図の実行時生成は`HowToPlayView`が担当します。生成済みの子オブジェクトを手編集せず、内容はContent、寸法や生成処理はViewを変更します。
- `HowToPlayView`にはフォント、▲▼のSprite、`BoardGameConfig`が必須です。`TitleScreenController`のView参照は省略可能ですが、標準Sceneでは割り当てて再表示時に先頭節へ戻します。
- `SampleScene`にはMain Camera、Bootstrap、`EventSystem`と`InputSystemUIInputModule`を明示配置し、Bootstrapと同じGameObjectへ`BoardGameAudioManager`を追加します。AudioSourceだけが実行時に生成されます。
- 盤面表示は`BoardView.prefab`、駒管理は`PieceViews.prefab`、HUDは`GameHud.prefab`を編集します。
- Prefabの公開設定を増やす場合は、Bootstrapの参照が維持されているかSampleSceneで確認します。
- HUDの固定階層は`GameHud.prefab`で編集します。Runtime生成されるセル、駒、リザーブカードをPlay中に変更してもAssetには保存されません。
- 16:9のHUDは、手番を左上、通常操作を右上、リザーブを盤面右脇、凡例を盤面左脇、音量を左下、破壊的なリセットを右下へアンカー配置します。リセットを通常操作バーへ戻さず、リザーブ見出しの`駒 n / 6`が常時読める幅を確保します。
- タイトルは大理石背景を隠さず、濃紺のSerif見出しと「6×10 の陣地到達型ボードゲーム」の副題で可読性を確保します。「ゲーム開始」を塗りの主操作、「遊び方」を線のみの副操作として扱います。
- Scene変更を含むPRでは、意図しないProjectSettings差分がないか必ず確認します。

## 7. Package管理

依存Packageは`Packages/manifest.json`で管理します。

- Package追加・更新は専用PRに分けます。
- `Packages/manifest.json`と`packages-lock.json`は同じPRで更新します。
- Git URLの`main`参照は将来内容が変わる可能性があるため、安定運用時は検証済みtagまたはcommitへの固定を検討します。

## 8. Gitブランチ運用

ブランチは人ではなく機能・修正単位で作ります。

| 用途 | 例 |
|---|---|
| 新機能 | `feature/fusion-rule` |
| 不具合修正 | `fix/combat-turn-order` |
| 文書 | `docs/architecture-update` |

推奨手順は次のとおりです。

1. 共有元ブランチを最新化します。
2. 1つの目的に限定したブランチを作ります。
3. 自分の担当フォルダを中心に変更します。
4. Core契約の変更が必要なら、その契約だけを先行PRとして分離します。
5. EditModeとPlayModeを実行します。
6. `git status`でScene、Package、ProjectSettingsの意図しない差分を除外します。
7. 小さなPRを作り、担当外のレビューを1人以上受けます。

人ごとの恒久ブランチは、複数機能が混ざりやすく、レビューとマージが難しくなるため使用しません。

## 9. コミットとPRの確認項目

確認項目は[`.github/pull_request_template.md`](../.github/pull_request_template.md)にあります。PRを作成すると本文へ自動で挿入されるので、各項目を確認してチェックを入れてください。

項目を追加・変更する場合はテンプレート側だけを編集します。二重管理を避けるため、この文書へは転記しません。

### 変更同期チェック

実装を始める前に、[拡張ガイドの変更影響マトリクス](EXTENSION_GUIDE.md#変更影響マトリクス)で変更範囲を決めます。PRを作る前に、次の対応が揃っていることを確認します。

- Coreの標準定義、PresentationのConfig既定値、Sceneが参照するAssetに同じ設定が重複する場合、すべてを同期する。
- 差し替え可能なRule、Resolver、Handler、Agentを追加した場合、`BoardGameBootstrap`または`GameCoordinator`から本番経路へ接続する。
- 新しいCommandを`GameSession`のdispatchへ登録し、新しいEventをPresentationのViewへ反映する。
- 振る舞いの一次情報源と、変更種別に対応するEditMode／PlayModeテストを更新する。
- 関連する`.meta`、Prefab、Config Assetだけを明示的に含め、PackageやProjectSettingsの無関係な差分を除外する。

## 10. コンフリクトを減らす方法

- 戦闘、合体、特殊効果、View、Inputは対応するフォルダ内で完結させます。
- SceneではなくPrefabを編集します。
- 大量の名前変更と機能追加を同じPRで行いません。
- 共通interfaceを変更する場合は、利用側の実装より先に合意します。
- Unity Editorを異なるバージョンで開きません。
- マージ直前に共有元の変更を取り込み、テストを再実行します。

## 11. 文書の担当範囲

同じ情報を複数の文書へ書くと、片方だけが更新されて食い違います。情報ごとに一次情報源を1つ決め、他の文書からはリンクします。

| 情報 | 一次情報源 |
|---|---|
| 実画面、画面遷移、操作手順、画面文言の注意点 | [`docs/SCREEN_GUIDE.md`](SCREEN_GUIDE.md) |
| ゲームルール、8方向、勝敗条件 | [`docs/GAME_RULES.md`](GAME_RULES.md) |
| 設計方針、レイヤー責務、データフロー | [`docs/ARCHITECTURE.md`](ARCHITECTURE.md) |
| コードの読み方、処理の流れ | [`docs/CODE_WALKTHROUGH.md`](CODE_WALKTHROUGH.md) |
| Coreの型の使い方、コード例、Rule差し替えで使う型 | [`docs/CORE_API.md`](CORE_API.md) |
| 開発環境、Config標準値、Git運用 | `docs/DEVELOPMENT.md`（この文書） |
| 機能の追加手順、変更影響範囲 | [`docs/EXTENSION_GUIDE.md`](EXTENSION_GUIDE.md) |
| テスト方針、実行手順 | [`docs/TESTING.md`](TESTING.md) |
| テストの一覧と件数 | テストコード本体 |
| PRの確認項目 | [`.github/pull_request_template.md`](../.github/pull_request_template.md) |
| 実装状況 | [`README.md`](../README.md) |

文書を追加・変更するときは、書こうとしている内容の一次情報源がすでに他にないかを先に確認してください。標準ルールの正確な数値表は`GAME_RULES.md`だけに置き、API例や概要からはリンクします。
