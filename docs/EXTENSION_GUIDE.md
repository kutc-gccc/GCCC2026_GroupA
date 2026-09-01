# 拡張ガイド

この文書は、機能追加・機能変更で更新する場所と手順の一次情報源です。ゲームルールそのものは[ゲームルール](GAME_RULES.md)、設計上の責務は[アーキテクチャ](ARCHITECTURE.md)、テストの実行方法は[テストガイド](TESTING.md)を参照してください。

## 1. 拡張時の共通原則

- CoreはUnityに依存させません。座標には`GridPosition`を使用します。
- 状態を直接変更せず、`GameCommand`を`GameSession.Execute`へ送ります。
- 操作の結果は`GameEvent`として返します。
- Presentationは最新`GameSnapshot`とEventだけで表示を更新します。
- 差し替え可能なRuleやResolverを実ゲームで使うには、Coreへの実装だけでなく、Composition Rootである`BoardGameBootstrap`から生成・注入します。
- 新しい共通契約は、利用側の機能PRより先に共有PRとして確定します。

`BoardGameBootstrap.Awake`は現在すべての実装差し替えが集中する唯一の注入口で、Resolverを選ぶSerializeFieldもFactoryもありません。移動・戦闘・合体・特殊マス・CPUのどの担当者も同じメソッドを編集するため、担当PRではCore側の実装とテストを先に確定し、`Awake`への配線は最後の1コミットにまとめてください。恒久的な解決は[§13](#13-将来の構造改善候補)のSession Factoryです。

### 変更影響マトリクス

実装前に対象行を確認し、Coreから文書更新までを同じ変更として扱います。

| 変更種別 | Core実装・契約 | 本番への配線 | Config・Asset | テスト | 更新する文書 |
|---|---|---|---|---|---|
| 標準移動プロファイル | `PowerMovementProfile.CreateStandard` | Config未設定時のFallbackを確認 | `BoardGameConfig.CreateDefaultMovementProfiles`、`StandardBoardGameConfig.asset` | EditModeの方向・境界・整合性 | `GAME_RULES.md`、必要に応じてREADME |
| 独自移動Rule | `IMovementRule`、必要に応じて`IMoveDirectionResolver` | `BoardGameBootstrap`でRuleを生成して`GameSession`へ注入 | RuleSetを選択可能にする場合はConfigを追加 | EditMode、候補表示のPlayMode | `GAME_RULES.md`、`ARCHITECTURE.md` |
| 戦闘 | `ICombatResolver`、`CombatResolution`、関連Event | `BoardGameBootstrap`から`GameSession`へ注入 | Rule選択が必要な場合だけ追加 | EditMode、駒表示のPlayMode | `GAME_RULES.md`、`CORE_API.md` |
| 合体 | `IFusionResolver`、`FusionResolution`、Command・Event | `BoardGameBootstrap`から`fusionResolver`へ注入（UI・View・音声は配線済み） | Ruleや確率を設定化する場合だけ追加 | EditMode、2駒選択とView更新のPlayMode | `GAME_RULES.md`、READMEの実装状況 |
| 特殊マス | `ICellEffectHandler`、`CellEffectResult`、Event | `CellEffectConfig`派生Assetが定義とHandlerを生成し、Bootstrapが`GameSession`へ登録 | 効果Asset作成 → `cellEffectDefinitions`登録 → 対象座標の`cellEffects`設定 | EditMode、表示がある場合はPlayMode | `GAME_RULES.md`、Config説明 |
| パワーランダム化 | `RandomizePowerCommand`、`RandomizePowerEvent`、`IRandomSource` | `GameHudView`のボタンと`GameCoordinator` | 範囲を設定化する場合だけ追加 | 固定乱数での範囲・禁止条件・手番消費 | `GAME_RULES.md`、`CORE_API.md` |
| リザーブ配置・駒上限 | `DeployReservePieceCommand`、`ReservePieceDeployed`、`GameDefinition`の上限と配置範囲 | `ReservePanelView`の個別カード、`GameHudView`の配置ボタン、`GameCoordinator`の配置モード（配線済み） | `maxPiecesPerPlayer`、`reserveDeploymentDepth` | 配置範囲・上限・手番消費のEditMode、カード選択と候補表示のPlayMode | `GAME_RULES.md`、`CORE_API.md` |
| CPU | `IPlayerAgent`実装 | Agentを生成して`GameCoordinator`へ注入 | 対戦形式を選択する場合に追加 | Agent単体、Human対CPU／CPU対CPUのPlayMode | READMEの実装状況、必要に応じて設計文書 |
| 新Command・Event | 派生型、Handler、状態更新、Event | `GameSession`のdispatchとPresentationの送受信経路 | 操作を設定化する場合だけ追加 | 拒否時不変性、Event値・順序、View反映 | `CORE_API.md`、`ARCHITECTURE.md` |
| UI・入力 | ルール変更がなければ変更しない | `GameCoordinator`、Input、View、Prefab | 表示値を設定化する場合だけ追加 | PlayMode、実機入力は手動確認 | `GAME_RULES.md`の表示・入力、`CODE_WALKTHROUGH.md` |
| 音声 | 変更しない | `BoardGameAudioManager.PlayEvents`とClip参照 | Prefab／SceneへのAudioClip割り当て | PlayModeの再生経路と音量 | `ARCHITECTURE.md`のAudio責務 |
| Scene遷移 | 変更しない | `BoardGameSceneNames`、`TitleScreenController`、`BoardGameBootstrap` | Build SettingsへのScene登録 | 起動Sceneとタイトル復帰のPlayMode | `ARCHITECTURE.md` §8、README |

### 共通の実装順序

1. **Core実装**: 状態、Command、Event、Ruleの契約を先に確定します。
2. **本番への配線**: `BoardGameBootstrap`または`GameCoordinator`で実装を生成・注入します。
3. **設定**: ConfigのSerializeFieldと実際にSceneが参照するAssetを同時に更新します。
4. **テスト**: 成功、拒否、境界値、状態不変性をEditModeで、入力・View・Scene統合をPlayModeで検証します。
5. **文書更新**: [開発ガイドの一次情報源表](DEVELOPMENT.md#11-文書の担当範囲)に従い、同じ情報を複数の文書へ再掲しません。

## 2. 移動ルールを追加・変更する

### Core実装

`IMovementRule`は駒とSnapshotから合法な移動先を返します。

```csharp
public interface IMovementRule
{
    IReadOnlyList<GridPosition> GetLegalDestinations(
        GameSnapshot snapshot,
        PieceState piece);
}
```

現在の標準実装は`DirectionalMovementRule`です。盤外、自分の陣地、自分の駒、ゲーム終了状態を考慮します。

戦闘力に応じた方向だけを変更する場合は`IMovementRule`を作り直さず、`PowerMovementProfile`の`PowerMovementBand`を変更します。別系統の移動特性には新しい`MovementProfileId`を割り当てます。距離変更やジャンプなど、方向以外の規則を変える場合だけ新しい`IMovementRule`を実装します。

### 本番への配線

標準プロファイルは`GameDefinition.MovementProfiles`から自動的に使用されます。独自`IMovementRule`を実ゲームで使う場合は、`BoardGameBootstrap`でRuleを生成し、`GameSession`の`movementRule`へ渡します。

```csharp
var session = new GameSession(
    definition,
    movementRule: new CustomMovementRule());
```

RuleSetをUnity上で選択できるようにする場合は、ConfigからRuleを生成するFactoryをComposition Rootへ追加します。移動条件を`GameCoordinator`や`BoardView`へ複製してはいけません。

### 設定

標準移動プロファイルを変更するときは、次を同じ変更で同期します。

1. `PowerMovementProfile.CreateStandard`のCore標準定義。
2. `BoardGameConfig.CreateDefaultMovementProfiles`の新規Config既定値。
3. `Assets/Config/BoardGame/StandardBoardGameConfig.asset`の実行時設定。
4. `GameSessionTests`の方向、戦闘力境界、未登録ID・不連続帯域の検証。
5. 標準ルールの一次情報源である[ゲームルール §5](GAME_RULES.md#5-移動ルール)。

`BoardGameBootstrap`にConfigが設定されている通常のSceneではAsset側が使われ、Config未設定時は`GameDefinition.CreateStandard()`のCore標準定義が使われます。片方だけを変更してはいけません。

### テスト

- 許可された全方向・距離。
- 戦闘力境界での方向変化。
- プロファイル帯域の隙間・重複・未登録IDの拒否。
- 禁止方向、盤外、自陣、自駒上。
- 敵駒マスが戦闘候補になること。
- `GetLegalCommands`と候補表示の一致。
- Config使用時とCore Fallbackで標準ルールが一致すること。

### 文書更新

正確な方向表は`GAME_RULES.md`だけに置きます。ゲーム概要が変わる場合はREADME、設計上の差し替え口が変わる場合は`ARCHITECTURE.md`も更新します。

## 3. 戦闘ルールを追加・変更する

### Core実装

`ICombatResolver`は攻撃側と防御側を受け取り、それぞれが受ける0以上のダメージ量を返します。滞在中戦闘力を先に消費する処理は`GameSession`が担当します。

```csharp
public interface ICombatResolver
{
    CombatResolution Resolve(PieceState attacker, PieceState defender);
}
```

標準の`SimultaneousCombatResolver`は双方の戦闘力を同時に減算します。状態異常や追加Eventが既存interfaceで表現できない場合は、Core契約の変更を独立PRにします。

### 本番への配線

Resolverを作成しただけでは実ゲームへ反映されません。`BoardGameBootstrap`でResolverを生成し、`GameSession`の`combatResolver`へ渡します。

```csharp
var session = new GameSession(
    definition,
    combatResolver: new CustomCombatResolver());
```

複数Ruleを選べるようにする場合は、選択値をConfigへ追加し、Bootstrap側のFactoryでResolverへ変換します。

### 設定

固定RuleであればConfig変更は不要です。Rule選択、倍率、上限などを調整可能にする場合だけSerializeFieldを追加し、`StandardBoardGameConfig.asset`へ標準値を保存します。

### テスト

- 攻撃側生存、防御側生存、相打ち。
- 戦闘力の境界値。
- 生存位置と`PiecePowerChanged`。
- `CombatResolved`、`PieceDestroyed`、`PieceMoved`の値と順序。
- 相手陣地上の戦闘と勝利判定。
- 注入したResolverがPlayModeの駒ViewとHUDへ反映されること。

### 文書更新

計算式と勝敗規則は`GAME_RULES.md`、新しい型やEvent値は`CORE_API.md`、差し替え構造が変わる場合は`ARCHITECTURE.md`を更新します。

## 4. 合体ルールを変更する

### Core実装

Coreには`FusePiecesCommand`、`IFusionResolver`、`PiecesFused`があります。`GameSession`の型switchがCommandを合体処理へdispatchし、標準では隣接自駒を25%大成功、50%成功、25%失敗で処理する`AdjacentFusionResolver`が使用されます。

```csharp
public interface IFusionResolver
{
    bool IsEnabled { get; }

    IReadOnlyList<FusionPair> GetLegalFusions(GameSnapshot snapshot, PlayerId player);

    bool TryResolve(PieceState first, PieceState second, out FusionResolution resolution);
}
```

1. 標準以外の判定が必要な場合は`IFusionResolver.IsEnabled`を`true`にするResolverを追加します。
2. `GetLegalFusions`で手番プレイヤーの合法な駒ペアを`FusionPair`の一覧として返します。
3. `TryResolve`で合体後の`PieceState`を含む`FusionResolution`を返します。
4. 合体後の`PieceState`には、重複しないID、有効な位置、正の戦闘力、登録済み`MovementProfileId`を設定します。

`FusionPair`と`FusionResolution`が保持する値は[Core APIリファレンス §8](CORE_API.md#8-rule差し替えで使う型)を参照してください。

合体後の戦闘力と`MovementProfileId`の決定はResolverへ閉じ込めます。`MoveDirections`を`PieceState`やViewへ直接保存してはいけません。

手順4に違反した結果は`CommandResult`の失敗ではなく例外になります。未登録の`MovementProfileId`を返すと`GameSession.ExecuteFusion`が`InvalidOperationException`を送出し、既存の駒とIDまたは位置が重複する駒を返すと`GameSession`内部の`AddPiece`が`ArgumentException`を送出します。Resolver側で合法性を確定させてから返してください。

標準の`AdjacentFusionResolver`は`internal`のため、Core外から参照・継承・ラップできません。EditModeテストからも直接生成できないので、標準判定の挙動を固定したい場合は`GameSession`の`randomSource`へテスト用`IRandomSource`を渡します。判定そのものを変える場合は`IFusionResolver`を新規実装してください。

### 本番への配線

合体は既に本番へ配線済みです。UI、View、音声はそのまま使えるため、**Resolverを差し替えるだけなら触る場所は1箇所です**。

1. `BoardGameBootstrap.Awake`の`new GameSession(...)`へ`fusionResolver:`引数を追加し、生成したResolverを渡します。

次の経路は既に存在するので、作り直してはいけません。

| 経路 | 実装場所 |
|---|---|
| 「合体」ボタンと2駒選択 | `GameHudView.FuseRequested` → `GameCoordinator.ToggleFusionMode` → `HandleFusionModeClick` |
| 合体後のView差し替え | `PieceViewManager.ApplyEvents`が実行後Snapshotとreconcile |
| 合体成功時のSFX | `BoardGameAudioManager.PlayEvents`の`PiecesFused` |
| 成功・失敗のメッセージ | `GameCoordinator.ShowFusionResultMessage` |

Resolverが新しいEventを発生させる場合だけ、[§8](#8-新しいcommandやeventを追加する)と[§11](#11-音声を変更する)に従ってViewと音声へ分岐を追加します。

### 設定

固定Ruleであれば、標準の`AdjacentFusionResolver`が既定で使われるためConfig変更は不要です。Ruleや確率を選択可能にする場合だけSerializeFieldを追加し、BootstrapのFactoryでResolverへ変換します。合体後に使用する`MovementProfileId`は、同じ`GameDefinition`へ登録済みでなければなりません。

### テスト

- 合法・不正な組み合わせ。
- 他プレイヤーの駒を含む要求の拒否。
- 合体後ID、位置、戦闘力、`MovementProfileId`。
- 合体後の手番交代・自動パス。
- 2駒選択、元Viewの削除、新Viewの生成。
- 固定`IRandomSource`による成功・大成功・失敗の再現。
- 合体後の永続効果履歴の和集合と、第一駒だけの滞在中効果継承。

### 文書更新

合体条件と計算規則は`GAME_RULES.md`、Command・Eventの値は`CORE_API.md`、実装状況はREADMEを更新します。

## 5. 特殊マスを追加する

### Core実装

特殊マスは文字列の`EffectId`と`ICellEffectHandler`を対応させます。

```csharp
public interface ICellEffectHandler
{
    string EffectId { get; }
    bool BlocksPowerRandomization { get; }
    CellEffectResult Apply(CellEffectContext context);
}
```

1. 効果ごとに`ICellEffectHandler`を実装します。
2. `EffectId`を重複しない固定値にします。
3. `BlocksPowerRandomization`で、この効果が適用中または適用済みの駒でパワーランダム化を禁止するかを宣言します。戦闘力を書き換える効果は`true`、盤面外へ駒を増やすような効果は`false`にします。標準では`CombatPowerBoostCellEffectHandler`が`true`、`ReservePieceGrantCellEffectHandler`が`false`です。
4. `Apply`から更新後の`PieceState`、追加Event、必要なら`ReservePieceGrant`を返します。

更新後の`PieceState`は元の駒から`With`系メソッドで作ります。使えるメソッドの一覧は[Core APIリファレンス §2](CORE_API.md#2-piecestate)、`ReservePieceGrant`の引数は[同 §8](CORE_API.md#8-rule差し替えで使う型)を参照してください。

効果は`CellEffectDefinition`で`WhileOccupied`または`PermanentOncePerPiece`を宣言します。同じセルに異なるLifetimeは設定できません。ID、所有者、位置を変更する結果は`GameSession`に拒否されます。

### 本番への配線

Handlerを作成しただけでは発動しません。Unity側では`CellEffectConfig`派生AssetがCore定義とHandlerを生成し、`BoardGameBootstrap`が`GameSession`へ登録します。

```csharp
var session = new GameSession(
    definition,
    cellEffectHandlers: new ICellEffectHandler[] { new PowerUpEffect() });
```

効果定義またはHandlerが未登録の場合は`GameDefinition`／`GameSession`生成時に拒否されるため、Handler登録とAsset設定を同じ変更に含めます。

### 設定

1. `CombatPowerBoostEffectConfig`、`ReservePieceGrantEffectConfig`、または新しい`CellEffectConfig`派生Assetを作成し、固有の`EffectId`とLifetime、効果値を設定します。
2. 作成したAssetを`StandardBoardGameConfig.asset`の`cellEffectDefinitions`へ登録します。
3. 対象座標の`cellEffects`へ同じ`EffectId`を実行順に設定します。
4. 見た目が必要な場合は`BoardView`へ表示を追加しますが、効果計算はCoreに残します。

標準Configには`reserve-piece-grant`の効果定義が1件あり、`(1,4)`と`(4,5)`で共有されています。別の効果を追加するときも、効果定義と座標のIDを必ず同じ変更に含めてください。どちらか片方だけを設定した状態ではゲーム定義の生成時に拒否されます。

### テスト

- Handlerの呼び出し順とLifetime混在の拒否。
- 滞在中効果の退出解除、ダメージ消費、再進入。
- 永続効果の一度だけの適用とランダム化拒否。
- 戦闘力・`MovementProfileId`の更新、リザーブ追加。
- 複数効果の累積。
- 未登録IDと不正な結果の拒否。
- `CellEffectTriggered`、`PiecePowerChanged`、追加Eventの値と順序。
- Bootstrapで登録したHandlerがConfig上のセルで発動するPlayMode統合。
- リザーブ獲得時に盤上＋リザーブが`MaxPiecesPerPlayer`を超えないこと。
- `DeployReservePieceCommand`が`ReserveDeploymentDepth`内の空きマスだけを合法手として返すこと。

### 文書更新

効果の発動条件と順序は`GAME_RULES.md`、Config項目は`DEVELOPMENT.md`、新しいEvent値は`CORE_API.md`を更新します。

## 6. CPUを追加する

### Core実装

HumanとCPUは`IPlayerAgent`を共通契約として使用します。

```csharp
public interface IPlayerAgent
{
    PlayerId Player { get; }
    void BeginTurn(
        GameSnapshot snapshot,
        IReadOnlyList<GameCommand> legalCommands,
        Action<GameCommand> submitCommand);
    void EndTurn();
}
```

CPU実装は将来の`GCCC.BoardGame.AI`アセンブリへ置き、Coreだけを参照します。`BeginTurn`で受け取ったSnapshotと合法Commandだけを評価し、選択したCommandをcallbackへ1回だけ渡します。

このアセンブリはまだ存在しないため、新しい`GCCC.BoardGame.AI.asmdef`を作成し、`references`に`GCCC.BoardGame.Core`だけを指定します。Agentを生成するのはPresentation側の`BoardGameBootstrap`なので、`GCCC.BoardGame.Presentation.asmdef`の`references`へも`GCCC.BoardGame.AI`を追加してください。asmdefの変更は複数領域に影響するため、[開発ガイド §4](DEVELOPMENT.md#4-フォルダと担当境界)のとおり統合変更として扱います。

### 本番への配線

1. BootstrapでHumanまたはCPU Agentを生成します。
2. `GameCoordinator`の`player1Agent`、`player2Agent`へ注入します。
3. 対戦形式をUnity上で選べるようにする場合はConfigへ選択項目を追加します。
4. `EndTurn`で保留中の思考とcallbackを破棄し、手番終了後の送信を防ぎます。

#### Coordinatorの駆動モデル

`GameCoordinator`は人間の入力を前提としたイベント駆動で書かれています。CPUを載せる前に次の3点を確認してください。

- **`GameCoordinator`は`MonoBehaviour`ではありません。** `Update`もCoroutineも持たず、Presentationで毎フレーム動くのは`BoardInputController`だけです。時間をかけて思考するCPUには、Agentを毎フレーム進める`MonoBehaviour`を`BoardGameBootstrap`で別途生成する必要があります。
- **同期的に`submitCommand`を呼ぶCPUは再帰します。** `BeginCurrentTurn`が`BeginTurn`を呼び、その中で即座にsubmitすると`ExecuteSubmittedCommand`から再び`BeginCurrentTurn`へ戻ります。CPU対CPUでは決着まで1本のコールスタックが伸びるため、1手ごとに制御を戻す設計にしてください。
- **盤面入力はAgentの種別を見ていません。** `GameCoordinator.HandleCellClick`の選択判定は`clickedPiece.Owner == snapshot.CurrentPlayer`だけなので、CPUの手番でも人間がCPUの駒を選択でき、合法手がハイライトされます。Command送信は同メソッド内の`is HumanPlayerAgent`で無視されるため不正な手は成立しませんが、表示上は選択できてしまいます。CPUを導入する変更では、この選択判定にAgent種別のガードを追加してください。

### 設定

固定の対戦形式ならConfig変更は不要です。Human対CPU、CPU対CPU、難易度を選択可能にする場合だけ設定を追加し、BootstrapでAgent生成へ変換します。

### テスト

- 合法Commandだけを1回送ること。
- 手番外やゲーム終了後に送らないこと。
- 合法手0件で停止すること。
- 同じSnapshotで決定的に動くテスト用Agent。
- `EndTurn`後に古いcallbackを送信しないこと。
- Human対CPU、CPU対CPUのPlayMode統合。

### 文書更新

実装状況と操作方法はREADME、Agent契約や依存方向が変わる場合は`ARCHITECTURE.md`を更新します。

## 7. 新しいCommandやEventを追加する

### Core実装

新しい操作が既存の`MovePieceCommand`、`FusePiecesCommand`、`RandomizePowerCommand`、`DeployReservePieceCommand`で表現できない場合にだけCommandを追加します。

Commandの追加はCoreアセンブリ内でのみ可能です。`GameSession`は公開Facadeで、`Execute`の型switchから各操作実行メソッドへdispatchします。外部アセンブリからdispatch処理を登録する口はありません。他の拡張と違って`GameSession`本体の変更を伴うため、[§1](#1-拡張時の共通原則)のとおり契約だけを先行PRとして確定してから進めます。

1. Core Commandsへ`GameCommand`派生型を追加します。
2. `GameSession.Execute`の型switchへ分岐を追加します。
3. 対応する内部状態更新処理を追加し、状態更新がSnapshotと合法手キャッシュを無効化することを確認します。
4. 成功時に必要な`GameEvent`派生型をCore Eventsへ追加します。
5. 状態変更は必ず`GameSession`内部で行います。

### 本番への配線

1. Human入力またはCPUが新Commandを生成できる経路を追加します。
2. `GameCoordinator`はCommandを`GameSession.Execute`へ送ります。
3. Presentationは新Eventと最新Snapshotを使ってViewを更新します。
4. Command型だけを追加して`GameSession`の固定dispatchへ登録し忘れないようにします。

### 設定

操作の有効化やパラメーターを選択可能にする場合だけConfigを追加します。設定値から直接状態を変更せず、CommandとRuleへ変換します。

### テスト

- 手番外、所有権違反、ゲーム終了後、無効な対象の拒否。
- 失敗時にSnapshotが変化しないこと。
- Eventの発生条件、値、順序。
- 新EventによるView生成・更新・削除。
- HumanまたはCPUから1回の操作で1Commandだけ実行されること。

### 文書更新

Command・Eventの引数と値は`CORE_API.md`、実行順と責務は`ARCHITECTURE.md`、入力フローが変わる場合は`CODE_WALKTHROUGH.md`を更新します。

## 8. UI・入力を変更する

### Core実装

表示や入力方法だけの変更ではCoreを変更しません。ルール上の新しい操作が必要な場合は、先にCommand・Event契約を追加します。

### 本番への配線

入力は`BoardInputController`から`GameCoordinator`へ渡し、ViewはSnapshotとEventから更新します。ルール判定をInputやViewへ複製してはいけません。

### 設定

色、Prefab、表示値を調整可能にする場合だけSerializeFieldとAssetを更新します。Runtime生成された子要素をPlay中に変更してもAssetには保存されません。

### テスト

- 選択、選択解除、合法手表示。
- 1操作につき1Commandだけ実行されること。
- UI上の入力が盤面へ伝播しないこと。
- Eventに応じたViewとHUDの更新。
- マウス・タッチの実機確認と複数解像度の手動スモークテスト。

### 文書更新

プレイヤーから見える表示・入力は`GAME_RULES.md`、処理フローは`CODE_WALKTHROUGH.md`、Prefab編集手順は`DEVELOPMENT.md`を更新します。

## 9. パワーランダム化を変更する

### Core実装

パワーランダム化は`RandomizePowerCommand`、`GameSession.ExecuteRandomizePower`、`RandomizePowerEvent`で構成されます。`GameSession`が`randomSource.NextInt(1, 4)`で新しい通常戦闘力（1〜3）を決め、手番を消費します。

変更する内容ごとに触る場所が違います。

| 変えたいこと | 変更場所 |
|---|---|
| 値の範囲 | `GameSession`のランダム化処理にある`NextInt`の引数 |
| 禁止条件 | 各`ICellEffectHandler`の`BlocksPowerRandomization`（[§5](#5-特殊マスを追加する)） |
| 乱数の出方 | `IRandomSource`実装を`GameSession`の`randomSource`へ注入 |
| 手番を消費するか | `GameSession`のランダム化処理の`ResolveNextTurn`呼び出し |

一時戦闘力ではなく通常戦闘力だけを書き換える点に注意してください。滞在中効果を持つ駒の実効戦闘力は`EffectiveCombatPower`で決まります。

### 本番への配線

配線済みです。ボタンは`GameHudView.OnRandomizePowerButtonClicked`から`GameCoordinator.HandleRandomizePowerButtonClicked`へ渡り、合法な`RandomizePowerCommand`があるときだけ`GameHudView.SetRandomizeButtonInteractable`で有効になります。乱数を差し替える場合だけ`BoardGameBootstrap.Awake`の`new GameSession(...)`へ`randomSource:`引数を追加します。

### 設定

固定仕様ならConfig変更は不要です。範囲や禁止条件を調整可能にする場合だけ`BoardGameConfig`へSerializeFieldを追加し、`StandardBoardGameConfig.asset`へ標準値を保存します。

### テスト

- 固定`IRandomSource`で下限・上限・同値になる場合を再現すること。
- `BlocksPowerRandomization`を持つ効果の適用中・適用済みで拒否されること。
- 成否にかかわらず手番が進むこと。
- 変更後の実効戦闘力で移動方向が更新されること。
- ボタンの有効・無効がPlayModeで合法手と一致すること。

### 文書更新

プレイヤーから見た挙動は`GAME_RULES.md`、Command・Eventの値は`CORE_API.md`、実装状況はREADMEを更新します。

## 10. リザーブ配置・駒上限を変更する

### Core実装

リザーブ駒は盤外に保管され、`DeployReservePieceCommand`で盤上へ出します。`GameSession.ExecuteDeployReservePiece`と`ReserveDeploymentRules`、`ReservePieceDeployed`、失敗理由の`ReservePieceNotFound`・`PieceLimitReached`・`InvalidDeploymentPosition`で構成されます。獲得側は特殊マスの`ReservePieceGrant`が担当します（[§5](#5-特殊マスを追加する)）。

| 変えたいこと | 変更場所 |
|---|---|
| 所有できる駒の上限 | `GameDefinition`の`maxPiecesPerPlayer`（既定は`StandardMaxPiecesPerPlayer` = 6） |
| 配置できる行数 | `GameDefinition`の`reserveDeploymentDepth`（既定は`StandardReserveDeploymentDepth` = 2） |
| 配置できるマスの条件 | `GameSession`の`GetLegalReserveDeploymentPositions` |
| 配置時の処理順 | `GameSession`の`ExecuteDeployReservePiece` |

配置候補は、自陣の行から相手陣地の向きへ`ReserveDeploymentDepth`行ぶんのうち、盤内で空いていて相手陣地ではないマスです。陣地行が各プレイヤー1行に定まらない盤面では候補が空になります。配置は`ApplyCellEffects`のあと`ResolveNextTurn`を呼ぶため、配置先に特殊マスがあればその場で発動し、手番を消費します。

**上限の判定は2か所にあり、数え方が違います。**

| 判定 | 数える対象 | 場所 |
|---|---|---|
| 配置できるか | 盤上の駒だけ（`GetBoardPieceCount`） | `ExecuteDeployReservePiece`と合法手の生成 |
| リザーブを獲得できるか | 盤上＋リザーブ（`GetOwnedPieceCount`） | `AddReservePiece` |

上限に達した状態でのリザーブ獲得は、例外にもCommandの失敗にもならず、`ReservePieceAdded`を出さずに黙って捨てられます。獲得側の上限挙動を変えるときはこの非対称を壊さないでください。

### 本番への配線

リザーブ配置は既に本番へ配線済みです。上限や配置範囲を変えるだけならPresentationは触りません。

| 経路 | 実装場所 |
|---|---|
| 「リザーブ配置」ボタン | `GameHudView.ReserveDeployRequested` → `GameCoordinator.ToggleReserveDeployMode` |
| 個別カードの選択 | `ReservePieceCardView` → `ReservePanelView.ReservePieceSelected` → `GameHudView.ReservePieceSelected` → `GameCoordinator.ToggleReservePieceSelection` |
| 配置先の選択 | `GameCoordinator.HandleReserveDeploymentClick` |
| 候補マスの表示 | `GameCoordinator.RenderReserveDeployment` → `BoardView.ShowSelection`の移動候補（緑） |
| 配置後のView生成 | `PieceViewManager.ApplyEvents`が実行後Snapshotとreconcile |
| ボタン・カードの有効状態 | `GameHudView.SetReserveDeployButtonInteractable`と`SetDeployableReservePieces` |

配置モードは合体モードと排他で、`InteractionState`が同時状態を表現できないようにします。新しいモードを足す場合はStateのFactoryと`GameCoordinator.HandleCellClick`冒頭の分岐を同時に更新してください。

### 設定

`BoardGameConfig`の`maxPiecesPerPlayer`と`reserveDeploymentDepth`を変更し、`StandardBoardGameConfig.asset`にも同じ値を保存します。Config未設定時は`GameDefinition`の既定値が使われるため、§2の標準移動プロファイルと同じく片方だけを変更してはいけません。

初期駒の数が`maxPiecesPerPlayer`を超える設定にすると`GameDefinition`の生成時に`ArgumentException`になります。`reserveDeploymentDepth`は0以上`rows`未満でなければなりません。

### テスト

- 自陣から前方`ReserveDeploymentDepth`行だけが候補になり、自陣・相手陣地・使用中のマスが除外されること。
- 盤上の駒が上限のとき`PieceLimitReached`で拒否されること。
- 他プレイヤーのリザーブ駒を指定すると`NotPieceOwner`、存在しないIDだと`ReservePieceNotFound`になること。
- 配置が手番を消費し、配置先の特殊効果が発動すること。
- 盤上＋リザーブが上限のとき獲得が行われず、`ReservePieceAdded`も出ないこと。
- 戦闘力・移動プロファイル・所有者Spriteを持つカード表示、先頭以外の選択、候補表示、配置後のカード削除と駒View生成、ボタン状態のPlayMode検証。

`CreateDefinitionWithEffectsAndLimits`で上限と配置範囲を指定した定義を作れます（[テストガイド §1](TESTING.md#1-editmodeテスト)）。

### 文書更新

プレイヤーから見た配置範囲と上限は`GAME_RULES.md`、Command・Event・`GameDefinition`の引数は`CORE_API.md`、Config項目は`DEVELOPMENT.md`を更新します。

## 11. 音声を変更する

### Core実装

Coreは変更しません。音声はEventを受け取って鳴らすだけで、CoreはUnityのAudio APIを参照しません。

### 本番への配線

音は[`BoardGameAudioManager`](../Assets/Scripts/BoardGame/Presentation/Audio/BoardGameAudioManager.cs)に閉じています。新しいEventへ音を付ける手順は次のとおりです。

1. `AudioClip`の`SerializeField`を追加します。
2. 純粋な`GameEventAudioResolver`へEvent型と`AudioCue`の対応を追加します。
3. Prefabまたは`SampleScene`上のAudioManagerへClipを割り当てます。

現在音が付いているのは`PieceMoved`、`CombatResolved`、`PieceDestroyed`、`PiecesFused`、`FusionAttemptFailed`、`GameEnded`の6種です。`GameEventAudioResolver`はEvent列の順序を保った`AudioCue`列を返すため、戦闘後の破壊音など複数SFXの既存順序も維持されます。

音量は`SetBgmVolume`と`SetSfxVolume`をHUDのスライダーが呼びます。BGMは`BgmMaximumVolume`で上限が掛かるため、スライダーの1.0がAudioSourceの1.0ではありません。

### 設定

Clipの割り当てはPrefabとSceneで行い、コードにパスを埋め込みません。AudioSourceは実行時に生成されるため、Play中の変更はAssetへ保存されません。

### テスト

- Eventに対応するClipが再生経路へ渡ること。
- Clip未設定のEventで例外にならないこと。
- スライダーの値が`BgmVolume`／`SfxVolume`へ反映されること。
- 「スタート画面に戻る」でBGMが停止すること。

### 文書更新

Audioレイヤーの責務は`ARCHITECTURE.md`、音量UIの操作は`GAME_RULES.md`の表示・入力、実装状況はREADMEを更新します。

## 12. Scene遷移を変更する

### Core実装

Coreは変更しません。CoreはScene名も`SceneManager`も参照しません。

### 本番への配線

Scene名は`BoardGameSceneNames`の定数に集約されています。

| 定数 | 値 |
|---|---|
| `BoardGameSceneNames.Title` | `TitleScene` |
| `BoardGameSceneNames.Game` | `SampleScene` |

遷移の向きごとに担当が分かれます。

- タイトル → ゲーム: `TitleScreenController`が「ゲーム開始」を受け取って`BoardGameSceneNames.Game`を読み込みます。
- ゲーム → タイトル: `GameHudView.StartScreenRequested`から`BoardGameBootstrap.ReturnToTitleScreen`が呼ばれます。

Sceneを増やす場合は、定数の追加とBuild Settingsへの登録を同じ変更に含めます。Scene名の文字列をコードへ直接書いてはいけません。

### 設定

Build SettingsのScene一覧を更新します。Scene自体の編集方針は[開発ガイド §6](DEVELOPMENT.md#6-sceneとprefabの編集)を参照してください。

### テスト

- 起動時に`TitleScene`が表示されること。
- 「ゲーム開始」で新しいゲームが構築されること。
- リザルトから「スタート画面に戻る」でタイトルへ戻り、BGMが停止すること。
- Build SettingsにScene登録漏れがないこと。

### 文書更新

Scene構成とBootstrapの責務は`ARCHITECTURE.md` §8、起動手順はREADMEと`DEVELOPMENT.md`を更新します。

## 13. 将来の構造改善候補

次は文書変更とは分離し、専用の実装PRで扱います。

- Core標準プロファイルと`StandardBoardGameConfig.asset`の一致を検証するEditModeテスト。
- Movement／Combat／Fusion Resolver、Cell Effect Handler、Player Agentを一か所で組み立てるSession Factory。
- Markdownリンク、Unityテスト、標準Config整合性を検証するCI。
- Unity Assetを自動生成する前に、整合性テストで重複定義の差異を検出する仕組み。
