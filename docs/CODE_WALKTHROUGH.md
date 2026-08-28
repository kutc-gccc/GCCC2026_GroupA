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

規模はおおよそ次のとおりです（`29e6adf`時点の概数）。

| 層 | ファイル | 行数 |
|---|---:|---:|
| Core | 約47 | 約1,900 |
| Presentation | 約10 | 約1,200 |
| テスト | 2 | 約680 |

Coreのほうが大きいことが、この構成の性格をよく表しています。

## 2. コードを読む順番

初めて読むなら、この順番が最短です。

1. **[ゲームルール](GAME_RULES.md)** — 何を作っているかを先に掴む
2. **`Core/Model/`** — 語彙を覚える。`GridPosition`、`PieceId`、`PlayerId`、`PieceState`、`GameSnapshot` の5つで十分
3. **`Core/Commands/` と `Core/Events/`** — Coreへの入力（要求）と出力（起きた事実）の形
4. **`Core/GameSession.cs`** — 中心。まず `Execute`、次に `ExecuteMove` を読む
5. **`Core/Rules/`** — 移動・戦闘・手番の実装。どれも短い
6. **`Presentation/GameCoordinator.cs`** — CoreとUnityの唯一の接続点
7. **`Presentation/Views/`** — 表示。Coreを理解した後なら素直に読める

`GameSession.ExecuteMove` と `GameCoordinator.HandleCellClick` の2つを読めば、全体の8割が分かります。

## 3. 起動時に何が起きるか

Sceneのルートには **Main Camera と Bootstrap しかありません**。残りは全部コードが実行時に組み立てます。

`Presentation/Bootstrap/BoardGameBootstrap.cs` の `Awake()` が順に行うことは次のとおりです。

1. `BoardGameConfig.CreateDefinition()` で設定アセットから `GameDefinition` を作る（未設定なら `GameDefinition.CreateStandard()`）
2. `new GameSession(definition)` でゲーム本体を作る
3. `new RuntimeSpriteFactory()` で画像を実行時に生成する
4. `ConfigureCamera()` で盤面が収まる正投影サイズを計算してカメラに設定する
5. `BoardView` / `PieceViewManager` / `GameHudView` を生成して `Initialize()` する
6. `GameCoordinator` を作り、`GameHudView.ResetRequested` を接続する
7. `BoardInputController` を作って配線する

依存関係の組み立てがこの1メソッドに集まっています（Composition Root）。Prefab参照が未設定でも、同じComponentを持つGameObjectを実行時に作るフォールバックがあります。

独自のRule、Resolver、Handler、AgentをCoreへ追加しただけでは、標準Sceneの実行経路には接続されません。実ゲームで使う実装はこのComposition Rootで生成し、`GameSession`または`GameCoordinator`へ注入します。変更箇所の一覧は[拡張ガイドの変更影響マトリクス](EXTENSION_GUIDE.md#変更影響マトリクス)を参照してください。

なお、ここで生成される`GameCoordinator`と`GameSession`は`MonoBehaviour`ではありません。Presentationで毎フレーム動くのは`BoardInputController`だけで、それ以外は入力とEventで駆動されます。CPUのように自分から時間をかけて動く実装を足すときはこの前提が効いてくるので、[拡張ガイド §6](EXTENSION_GUIDE.md#6-cpuを追加する)を先に読んでください。

## 4. 1手を指すと何が起きるか

このプロジェクトを理解する中心です。クリックから画面更新までを実際のメソッド名で追います。

```
① BoardInputController.Update()
     毎フレーム、Touchscreen または Mouse の押下を検出
          ↓
② GameHudView.IsPointerOverControl()
     リセットボタンの上なら中断（UIクリックが盤面へ貫通しない）
          ↓
③ BoardView.TryScreenToCell()
     スクリーン座標 → ワールド → ローカル → (列, 行)
          ↓
④ GameCoordinator.HandleCellClick(cell)
     ├─ 自分の駒だった      → 選択 / 選択解除して RenderSelection()
     │                          合法手を緑（空きマス）とオレンジ（敵駒）で表示
     └─ 合法な移動先だった  → HumanPlayerAgent.TrySubmit(move)
          ↓
⑤ HumanPlayerAgent が BeginTurn で預かった callback を呼ぶ
     = GameCoordinator.ExecuteSubmittedCommand
          ↓
⑥ GameSession.Execute(command)
     null? / ゲーム終了? / 手番一致? / Handlerある? の4段階を検証
          ↓
⑦ GameSession.ExecuteMove()
     駒の存在と所有権を確認し、DirectionalMovementRule で合法性を判定
     ├─ 空きマス → ResolveUnoccupiedMove()
     └─ 敵駒     → ResolveCombatMove() → SimultaneousCombatResolver
          ↓
⑧ ApplyCellEffects() → 相手陣地に到達していれば winner を確定
          ↓
⑨ ResolveNextTurn() → TurnResolver が交代 / 自動パス / 引き分けを決める
          ↓
⑩ CommandResult に GameEvent のリストを詰めて返す
          ↓
⑪ PieceViewManager.ApplyEvents(events, snapshot)
     Eventの型で switch し、View を生成 / 更新 / 削除
          ↓
⑫ GameHudView.Render(snapshot) で手番・勝敗テキストを更新
```

### 読むときの要点

**Viewは「なぜそうなったか」を判断しません。** `PieceDestroyed` が来たから駒を消すのであって、戦闘力を見て「0以下だから消そう」とは考えません。ルールがView側に二重実装されるのを防ぐためです。

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
PowerMovementProfile.GetDirections(piece.CombatPower)
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

### 画像アセットが1枚もない

`Presentation/Views/RuntimeSpriteFactory.cs` が起動時にテクスチャを生成しています。

- **マスの四角** — 1×1ピクセルの白テクスチャを引き伸ばして使う
- **駒の丸** — 64×64のテクスチャに、中心からの距離でアルファ値を計算して円を描く。境界でアルファを補間しているのでアンチエイリアスが効く

どちらも `HideFlags.DontSave` 付きで生成し、`Dispose()` で破棄します。

### Sceneをほぼ空にしてある

盤面・駒・HUDがそれぞれ別Prefabに分かれ、Sceneのルートには2つしか置きません。複数人が同じScene YAMLを同時に編集してコンフリクトするのを避けるためです（[アーキテクチャ §9](ARCHITECTURE.md#9-設定とasset)）。

### カメラの自動調整

`BoardGameBootstrap.ConfigureCamera()` が盤面の行数・列数と画面アスペクト比から正投影サイズを計算します。設定で盤面サイズを変えてもカメラを触る必要がありません。

### 日本語フォントの自動選択

`GameHudView.CreateUiFont()` が `Yu Gothic UI` → `Meiryo UI` → `Hiragino Sans` → `Noto Sans CJK JP` → `Arial` の順にOSフォントを探し、見つからなければUnity組み込みフォントに落とします。Windows以外でも文字化けしません。

## 7. 今のゲームで実際に起きること

コードを追うと分かる、現在の標準設定の挙動です。仕様の欠陥ではなく、**拡張の土台が先に用意されている**状態だと理解してください。

**移動プロファイルの帯域2〜8は、現時点では発動しません。** 理由は次のとおりです。

1. `StandardBoardGameConfig.asset` の `initialCombatPower` が `1`
2. `SimultaneousCombatResolver` は双方に `攻撃力 - 防御力` を適用するため、`1 vs 1` は両方0になって相打ちになる
3. 戦闘力を変えられるのはセル効果と合体だが、セル効果はHandlerが未登録、合体は `DisabledFusionResolver` で無効
4. したがって戦闘力は常に1のままで、`PowerMovementProfile.GetDirections(1)` は常に全8方向を返す

つまり今のゲームは「全8方向へ動ける駒どうしが相打ちしながら、8マス先の相手陣地を目指す競走」になります。合体か特殊マスが実装されると、戦闘力に差が生まれて移動プロファイルが初めて意味を持ちます。

同じく骨組みだけある機能は次のとおりです。

| 機能 | 用意されているもの | 足りないもの |
|---|---|---|
| 合体 | Command、Handler、Event、`IFusionResolver` | 有効なResolverと2駒選択UI |
| 特殊マス | `CellDefinition.EffectIds`、`ICellEffectHandler` | Handlerの実装と設定への登録 |
| CPU | `IPlayerAgent`、Agentの注入口 | 実装そのもの |

追加手順は[拡張ガイド](EXTENSION_GUIDE.md)にあります。
