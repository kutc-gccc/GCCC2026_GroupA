# コード読解ガイド

この文書は**ソースコードを読む人への道案内**です。どのファイルからどの順に読み、1手を指したときに処理がどう流れるかを扱います。

内容そのものは、それぞれの一次情報源にあります。

| 知りたいこと | 読む文書 |
|---|---|
| ゲームのルール | [ゲームルール](GAME_RULES.md) |
| なぜこの設計なのか | [アーキテクチャ](ARCHITECTURE.md) |
| 型の使い方とコード例 | [Core APIリファレンス](CORE_API.md) |
| 機能の追加手順 | [拡張ガイド](EXTENSION_GUIDE.md) |
| テストの方針と実行方法 | [テストガイド](TESTING.md) |

## 1. 全体像

このプロジェクトは、**Unityに依存しないゲームルールのエンジン**と、**その状態を画面に出すUnityの層**の2つでできています。

```
Presentation（Unity依存）    入力をマス座標へ変換 / SnapshotとEventを絵にする
        ↓ Command                    ↑ Snapshot と Event
Core（Unity非依存）          盤面状態・ルール・勝敗判定
```

`GCCC.BoardGame.Core.asmdef` の `noEngineReferences: true` によって、Coreに`UnityEngine`を書くとコンパイルが通りません。この境界はコンパイラが守っています。

Coreに状態・Command・Event・Ruleを集め、PresentationはScene、入力、View、音声、Composition Rootに分かれています。ファイル数や行数ではなく、この依存方向を起点に読むと機能追加後も迷いにくくなります。

## 2. コードを読む順番

初めて読むなら、この順番が最短です。

1. **[ゲームルール](GAME_RULES.md)** — 何を作っているかを先に掴む
2. **`Core/Model/`** — 語彙を覚える。`GridPosition`、`PieceId`、`PlayerId`、`PieceState`、`GameSnapshot` の5つで十分
3. **`Core/Commands/` と `Core/Events/`** — Coreへの入力（要求）と出力（起きた事実）の形
4. **`Core/GameSession.cs`** — 中心。まず `Execute`、次に `ExecuteMove` を読む
5. **`Core/Rules/`** — 移動・戦闘・手番の実装。どれも短い
6. **`Presentation/GameCoordinator.cs`** — Human入力、Agent、Command、View、音声を仲介する中心
7. **`Presentation/Views/`** — 表示。Coreを理解した後なら素直に読める

`GameSession.ExecuteMove` と `GameCoordinator.HandleCellClick` の2つを読めば、全体の8割が分かります。

## 3. 起動時に何が起きるか

通常の入口は`TitleScene`です。`TitleScreenController`が「ゲーム開始」「遊び方」「戻る」を配線し、タイトルと説明ページを同じScene内で切り替えます。「ゲーム開始」だけが`SampleScene`を読み込みます。

遊び方は`How To Content`の`HowToPlayView`が初めて表示されたときに6節を生成します。文章・図解データは`HowToPlayContent`、寸法・ナビ・図の生成は`HowToPlayView`です。開き直すと`ResetToFirstSection()`で先頭へ戻り、子要素は重複生成しません。固定の背景・見出し・戻るボタンはScene側にあります。実際の見た目は[画面・操作ガイド](SCREEN_GUIDE.md#2-遊び方)を参照してください。

ゲーム本体の`SampleScene`には **Main Camera、Bootstrap、EventSystem** があり、HUD階層は`GameHud.prefab`に保存されています。

`Presentation/Bootstrap/BoardGameBootstrap.cs` の `Awake()` が順に行うことは次のとおりです。

1. `BoardGameConfig.CreateDefinition()` で設定アセットから `GameDefinition` を作る（未設定なら `GameDefinition.CreateStandard()`）
2. Configからセル効果Handlerを作り、`new GameSession(definition, cellEffectHandlers: ...)`でゲーム本体を作る
3. `new RuntimeSpriteFactory()` で画像を実行時に生成する
4. `ConfigureCamera()`で盤面が収まる正投影サイズを設定し、`BoardGameAudioManager`を取得または生成する
5. `BoardView` / `PieceViewManager` / 必須の`GameHudView` Prefabを生成して`Initialize()`する。HUDの固定階層はPrefab、リザーブカードだけはSnapshotに応じて生成する
6. `GameCoordinator`を作り、リセット、合体、パワーランダム化、リザーブカード選択、タイトル復帰、音声の経路を接続する
7. `BoardInputController`を作って配線する

依存関係の組み立ては`Awake`から呼ぶ小さな生成・配線メソッドへ分かれていますが、所有者はBootstrap 1つです（Composition Root）。HUD PrefabまたはSceneのEventSystemが未設定なら、不完全な代替UIを作らずエラーで停止します。

独自のRule、Resolver、Handler、AgentをCoreへ追加しただけでは、標準Sceneの実行経路には接続されません。実ゲームで使う実装はこのComposition Rootで生成し、`GameSession`または`GameCoordinator`へ注入します。変更箇所の一覧は[拡張ガイドの変更影響マトリクス](EXTENSION_GUIDE.md#変更影響マトリクス)を参照してください。

なお、ここで生成される`GameCoordinator`と`GameSession`は`MonoBehaviour`ではありません。Presentationで毎フレーム動くのは`BoardInputController`だけで、それ以外は入力とEventで駆動されます。CPUのように自分から時間をかけて動く実装を足すときはこの前提が効いてくるので、[拡張ガイド §6](EXTENSION_GUIDE.md#6-cpuを追加する)を先に読んでください。

## 4. 1手を指すと何が起きるか

このプロジェクトを理解する中心です。クリックから画面更新までを実際のメソッド名で追います。

```
① BoardInputController.Update()
     毎フレーム、Touchscreen または Mouse の押下を検出
          ↓
② GameHudView.IsPointerOverControl()
     操作ボタン、リザーブパネル、音量UI、リザルト上なら中断（UIクリックが盤面へ貫通しない）
          ↓
③ BoardView.TryScreenToCell()
     スクリーン座標 → ワールド → ローカル → (列, 行)
          ↓
④ GameCoordinator.HandleCellClick(cell)
     モードを先に見る。合体・リザーブ配置は互いに排他
     ├─ 合体モード中        → HandleFusionModeClick()        → FusePiecesCommand
     ├─ 配置モード中        → HandleReserveDeploymentClick() → DeployReservePieceCommand
     ├─ 自分の駒だった      → 選択 / 選択解除して RenderSelection()
     │                          琥珀の選択枠、白い点（空きマス）、赤い枠（敵駒）で表示
     └─ 合法な移動先だった  → HumanPlayerAgent.TrySubmit(move)

   「パワーランダム化」ボタンは盤面クリックを経ずに
   GameCoordinator.HandleRandomizePowerButtonClicked から RandomizePowerCommand を送る
          ↓
⑤ HumanPlayerAgent が BeginTurn で預かった callback を呼ぶ
     = GameCoordinator.ExecuteSubmittedCommand
          ↓
⑥ GameSession.Execute(command)
     null? / ゲーム終了? / 手番一致? / 対応Command型? の4段階を検証
          ↓
⑦ Command の型に対応する実行メソッドへ分岐（以下は移動の場合）
     ExecuteMove / ExecuteFusion / ExecuteRandomizePower / ExecuteDeployReservePiece

   GameSession.ExecuteMove()
     駒の存在と所有権を確認し、DirectionalMovementRule で合法性を判定
     移動元の滞在中効果を失効してから処理する
     ├─ 空きマス → ResolveUnoccupiedMove()
     └─ 敵駒     → ResolveCombatMove() → SimultaneousCombatResolver
          ↓
⑧ ApplyCellEffects() → 相手陣地に到達していれば winner を確定
          ↓
⑨ ResolveNextTurn() → TurnResolver が交代 / 自動パス / 引き分けを決める
          ↓
⑩ CommandResult に GameEvent のリストを詰めて返す
          ↓
⑪ PieceViewManager.ApplyEvents(events, snapshot) / BoardGameAudioManager.PlayEvents(events)
     PieceViewをSnapshotとreconcileし、GameEventAudioResolverの順序でSFXを再生
          ↓
⑫ BoardViewの選択表示を解除し、GameHudView.Render(snapshot)からReservePanelViewを含む手番・勝敗・リザーブ表示を更新
```

### 読むときの要点

**Viewは「なぜそうなったか」を判断しません。** 現在の`PieceViewManager.ApplyEvents`はEventを個別処理せず、実行後Snapshotとの照合で駒を生成・更新・削除します。Viewが戦闘力から生死を再計算することはありません。音声側はEvent列を使います。

**失敗しても状態は変わりません。** 不正な手を送ると `CommandResult.Success` が `false` になり、盤面は一切変更されません。これはテストや将来のCPUが手を安全に試せる前提でもあります。

**選択の表示もCoreに聞いています。** `GameCoordinator.RenderSelection()` は自前で移動先を計算せず、`GameSession.GetLegalCommands()` の結果を絞り込んで表示しています。

## 5. 移動方向が決まる仕組み

ここは読まないと分かりにくいので、流れだけ示します。

**`PieceState` は移動方向を持っていません。** 代わりに `MovementProfileId` という文字列IDを持ち、方向は現在の戦闘力から毎回計算されます。

```
DirectionalMovementRule.GetLegalDestinations(snapshot, piece)
        ↓ piece を渡す
IMoveDirectionResolver.Resolve(piece)
        ↓ 標準実装は ProfileMoveDirectionResolver
piece.MovementProfileId でプロファイルを引く
        ↓
PowerMovementProfile.GetDirections(piece.EffectiveCombatPower)
        ↓ 戦闘力が入る帯域を探す
MoveDirections（実効的な移動方向）
```

`PowerMovementProfile` は `PowerMovementBand`（戦闘力の範囲 + 許可する方向）の集合です。コンストラクタが「戦闘力1から`int.MaxValue`までを隙間も重複もなく覆っているか」を検証し、満たさなければ`ArgumentException`を投げます。

方向を状態として保存していないため、**戦闘や特殊効果で戦闘力が変われば、次に合法手を計算した時点で移動方向も自動的に変わります。**

- 標準プロファイルの帯域表 → [ゲームルール §5](GAME_RULES.md#5-移動ルール)
- 設定アセットでの指定方法 → [開発ガイド §5](DEVELOPMENT.md#5-standardboardgameconfig)
- 独自プロファイルの追加手順 → [拡張ガイド](EXTENSION_GUIDE.md)

## 6. 実装上の工夫

コードを読むと気づく、細かいが効いている部分です。

### 盤面の印と説明図のSpriteは実行時生成

`Presentation/Views/RuntimeSpriteFactory.cs` が起動時にテクスチャを生成しています。

- **四角** — `SquareSprite`。1×1ピクセルの白テクスチャをセル・効果表示などに使う
- **円** — `CircleSprite`。移動・リザーブ配置候補の白い点に使う
- **中抜きの枠** — `FrameSprite`。選択・戦闘・合体の色付き枠に使う
- **三角形** — `TriangleSprite`。陣地所有者の向きの印に使う

いずれも`HideFlags.DontSave`付きで生成し、所有する`RuntimeSpriteFactory.Dispose()`で破棄します。`HowToPlayView`も同じFactoryと`BoardView`の色定義を使い、ゲームと説明図の印を揃えています。

駒そのものは`Assets/Art/Pieces/`の三角形スプライトを`BoardGameBootstrap`のSerializeFieldから受け取ります。遊び方にも▲▼の駒スプライトと盤面Configを割り当て、盤の図を標準配置から生成します。

タイトル背景、駒、フォント、BGM、SFXはAssetとして保持します。固定Assetと、繰り返し要素の実行時生成を使い分けています。

### 固定UIと実行時生成の境界

ゲーム本体のSceneにはCamera、Bootstrap、EventSystemがあり、盤面・駒・HUDを別Prefabから組み立てます。HUDの固定階層はPrefab、盤面セル・駒・リザーブカードは実行時生成です。タイトル・遊び方の外枠は`TitleScene`に保存し、説明のナビ・各節だけを実行時生成します（[アーキテクチャ §9](ARCHITECTURE.md#9-設定とasset)）。

### カメラの自動調整

`BoardGameBootstrap.ConfigureCamera()` が盤面の行数・列数と画面アスペクト比から正投影サイズを計算します。設定で盤面サイズを変えてもカメラを触る必要がありません。

### UIフォントは明示的に割り当てる

`GameHudView`と`HowToPlayView`には`uiFont`を割り当てます。OSフォントを自動探索する`CreateUiFont()`は現行実装にはありません。標準HUDと遊び方は同じフォントAssetを参照し、必須参照が不足するとエラーで停止します。駒上の数字は`PieceView`が生成する`TextMesh`です。

## 7. 今のゲームで実際に起きること

コードを追うと分かる、現在の標準設定の挙動です。仕様の欠陥ではなく、**拡張の土台が先に用意されている**状態だと理解してください。

初期戦闘力は1ですが、パワーランダム化・合体・戦闘・特殊マスで実効戦闘力と合法方向が変化します。標準Configにはリザーブ獲得2マスと戦闘力アップ2マスがあります。正確な座標と条件は[ゲームルール §8](GAME_RULES.md#8-パワーランダム化合体特殊マス)を参照してください。Configなしの`GameDefinition.CreateStandard()`は特殊マスを持たない点に注意してください。

現在の拡張状況は次のとおりです。

| 機能 | 用意されているもの | 足りないもの |
|---|---|---|
| 合体 | `AdjacentFusionResolver`、確率判定、2駒選択UI | 追加ルールが必要な場合だけResolverを差し替える |
| 特殊マス | 2種のLifetime、戦闘力増加、リザーブ獲得、盤面・HUD表示、標準盤面の特殊マス4個 | 追加効果や配置を増やす場合だけConfigを拡張する |
| CPU | `IPlayerAgent`、Agentの注入口 | 実装そのもの |

追加手順は[拡張ガイド](EXTENSION_GUIDE.md)にあります。
