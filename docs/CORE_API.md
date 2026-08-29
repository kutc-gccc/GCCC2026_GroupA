# Core APIリファレンス

この文書は`GCCC.BoardGame.Core`の型の**使い方**を扱います。型ごとのコンストラクタ引数とコード例をまとめています。

- 設計判断とレイヤーの責務は[アーキテクチャ](ARCHITECTURE.md)を参照してください。
- 機能を追加する手順は[拡張ガイド](EXTENSION_GUIDE.md)を参照してください。
- ゲームルールそのものは[ゲームルール](GAME_RULES.md)を参照してください。

Coreは`noEngineReferences: true`のため、ここに登場する型はすべてUnity APIへ依存しません。

## 1. 座標、ID、プレイヤー、移動方向

| 型 | 役割 | 生成・使用例 |
|---|---|---|
| `GridPosition` | `Column`（x）と`Row`（y）で盤面座標を表す | `new GridPosition(2, 1)` |
| `PieceId` | 駒を一意に識別する正の整数ID | `new PieceId(1)` |
| `PlayerId` | 所有者や手番を`Player1`／`Player2`で表す | `PlayerId.Player1` |
| `MoveDirections` | 移動可能な8方向をフラグで表す | `MoveDirections.North` |
| `MovementProfileId` | 戦闘力別移動設定を識別する文字列ID | `new MovementProfileId("standard")` |

`GridPosition`はUnityの`Vector2Int`を使わないCore専用座標です。同じ列・行なら同じ座標として比較でき、`+`演算子で方向オフセットを加算できます。ただし、座標が盤内かどうかは`GridPosition`自身ではなく`GameSnapshot.IsInside`やRuleが判定します。

`PieceId`は数値が同じなら同じ駒として比較されます。0以下は指定できません。`PlayerId.Other()`は、`Player1`なら`Player2`、`Player2`なら`Player1`を返します。

`MoveDirections`はフラグ列挙型で、`None`と`All`を含みます。複数方向はビットOR演算子`|`で組み合わせます。

```csharp
MoveDirections directions = MoveDirections.North | MoveDirections.East;
```

各フラグと相対座標の対応は[ゲームルール §5 移動ルール](GAME_RULES.md#5-移動ルール)を参照してください。`MoveDirections`は`PieceState`へ直接保存せず、移動プロファイルから戦闘力ごとに解決されます。

## 2. `PieceState`

盤面に存在する駒1個につき、1個の`PieceState`インスタンスを作成します。コンストラクタの引数は次の順番です。

| 引数 | 型 | 意味 |
|---|---|---|
| `id` | `PieceId` | 駒を一意に識別するID |
| `owner` | `PlayerId` | 駒を所有するプレイヤー |
| `position` | `GridPosition` | 盤面上の現在位置 |
| `combatPower` | `int` | 現在の戦闘力。1以上を指定する |
| `movementProfileId` | `MovementProfileId` | 駒が使用する戦闘力別移動プロファイル |
| `appliedPermanentEffectIds` | `IEnumerable<string>` | 適用済み永続効果ID。省略可 |
| `activeCellEffects` | `IEnumerable<ActiveCellEffectState>` | 現在の滞在中効果と一時戦闘力。省略可 |

たとえば、座標`(2, 1)`にいる「プレイヤー1が所有する、戦闘力2で標準プロファイルを使う駒」は次のように生成します。

```csharp
var piece = new PieceState(
    new PieceId(1),                    // 駒番号1
    PlayerId.Player1,                  // プレイヤー1の駒
    new GridPosition(2, 1),            // 座標(2, 1)
    2,                                 // 戦闘力2
    PowerMovementProfile.StandardId    // 標準移動プロファイル
);
```

`PieceState`は不変です。`CombatPower`は通常戦闘力、`TemporaryCombatPower`は滞在中効果の残量、`EffectiveCombatPower`は両者の合計です。表示・戦闘・移動方向は`EffectiveCombatPower`を使用します。変更は`WithPosition`、`WithCombatPower`、`WithMovementProfile`、効果用メソッドで新しいインスタンスを作成します。

```csharp
PieceState movedPiece = piece.WithPosition(new GridPosition(2, 2));
PieceState damagedPiece = movedPiece.WithCombatPower(1);
```

この例でも元の`piece`と`movedPiece`は変更されません。`combatPower`に0以下を渡すと`ArgumentOutOfRangeException`になります。戦闘力が0以下になった駒は、戦闘力0の`PieceState`として残すのではなく、`GameSession`の管理対象から削除されます。

### 戦闘力別移動プロファイル

`PowerMovementBand`は、戦闘力の範囲とその範囲で許可する方向を定義します。`PowerMovementProfile`は複数の帯域をまとめ、戦闘力1から`int.MaxValue`までを隙間・重複なく覆う必要があります。

```csharp
var profile = new PowerMovementProfile(
    new MovementProfileId("vertical-only"),
    new[]
    {
        new PowerMovementBand(
            1,
            int.MaxValue,
            MoveDirections.North | MoveDirections.South)
    });

MoveDirections directions = profile.GetDirections(3); // North | South
```

帯域に隙間・重複がある、戦闘力1から始まらない、最後が`int.MaxValue`まで届かない場合は`ArgumentException`になります。`ProfileMoveDirectionResolver`は`PieceState.MovementProfileId`でプロファイルを選び、現在の`EffectiveCombatPower`に対応する方向を返します。標準プロファイルの正確な対応表は[ゲームルール §5](GAME_RULES.md#5-移動ルール)を参照してください。

## 3. 盤面定義と実行時状態

| クラス | 役割 | 主な内容 |
|---|---|---|
| `CellDefinition` | 1マスの固定設定 | 座標、陣地所有者、特殊効果ID |
| `InitialPieceDefinition` | リセット時に生成する1個の駒の初期設定 | ID、所有者、初期位置、初期戦闘力、移動プロファイルID |
| `GameDefinition` | ゲーム開始前の設計図 | 盤面サイズ、全セル、全初期駒、先手、移動プロファイル、効果定義 |
| `CellEffectDefinition` | 特殊効果の固定設定 | 効果ID、`WhileOccupied`／`PermanentOncePerPiece` |
| `PlayerState` | プレイヤーの実行時状態 | 盤外に保管されている`ReservePieceState` |
| `GameSnapshot` | ある時点のゲーム状態を外部へ見せる読み取り専用コピー | 現在の全駒、セル、効果定義、リザーブ、手番、勝敗 |

### `CellDefinition`

通常マスと、プレイヤー1の陣地マスは次のように定義できます。`territoryOwner`が`null`なら、どちらの陣地でもない通常マスです。`effectIds`は省略でき、指定した場合は配列の順番で特殊効果が処理されます。

```csharp
var normalCell = new CellDefinition(
    new GridPosition(2, 5));

var player1TerritoryCell = new CellDefinition(
    new GridPosition(2, 0),
    PlayerId.Player1,
    new[] { "power-up" });
```

### `InitialPieceDefinition`

現在盤上にいる駒ではなく「開始時やリセット時に、どの駒を作るか」という設定です。引数の意味と順番は`PieceState`と同じです。

```csharp
var initialPiece = new InitialPieceDefinition(
    new PieceId(1),
    PlayerId.Player1,
    new GridPosition(0, 1),
    1,
    PowerMovementProfile.StandardId);

PieceState initialState = initialPiece.CreateState();
```

### `GameDefinition`

`GameDefinition.CreateStandard()`を使うと、6×10盤面、12個の初期駒、プレイヤー1先手、標準移動プロファイルという定義をまとめて作れます。

```csharp
GameDefinition definition = GameDefinition.CreateStandard();
```

Unity上では`BoardGameConfig.CreateDefinition()`が設定アセットから同等の定義を生成します。設定項目は[開発ガイド §5](DEVELOPMENT.md#5-standardboardgameconfig)を参照してください。

### `GameSnapshot`

`GameSnapshot`は通常`GameSession.Snapshot`から取得します。公開コンストラクタはテストや独立したView描画にも利用できますが、生成したSnapshotから`GameSession`内部状態を変更することはできません。

```csharp
GameSnapshot snapshot = session.Snapshot;

bool isInside = snapshot.IsInside(new GridPosition(2, 1));
bool found = snapshot.TryGetPiece(new PieceId(1), out PieceState currentPiece);
int player1PieceCount = snapshot.GetPieceCount(PlayerId.Player1);
int reserveCount = snapshot.GetPlayer(PlayerId.Player1).ReservePieces.Count;
bool isGameOver = snapshot.IsGameOver;
```

### 定義と実行時状態の関係

```text
GameDefinition（ゲーム全体の設計図）
├─ CellDefinition（各マスの固定設定）
├─ PowerMovementProfile（戦闘力別移動設定）
├─ CellEffectDefinition（特殊効果IDとLifetime）
└─ InitialPieceDefinition（各駒の初期設定とプロファイルID）
           ↓ GameSessionの開始・Reset
       PieceState（各駒の現在状態）
       PlayerState（各プレイヤーのリザーブ状態）
           ↓ Snapshot取得
       GameSnapshot（盤面・効果・プレイヤー状態の読み取り専用コピー）
```

## 4. `GameSession`

コンストラクタではゲーム定義と、必要に応じて差し替えるRuleを受け取ります。

| 引数 | 型 | 省略時 |
|---|---|---|
| `definition` | `GameDefinition` | 省略不可 |
| `movementRule` | `IMovementRule` | `DirectionalMovementRule` |
| `combatResolver` | `ICombatResolver` | `SimultaneousCombatResolver` |
| `fusionResolver` | `IFusionResolver` | `AdjacentFusionResolver` |
| `cellEffectHandlers` | `IEnumerable<ICellEffectHandler>` | 効果なし |
| `randomSource` | `IRandomSource` | `SystemRandomSource` |

公開APIは次の4つです。

```csharp
GameSnapshot Snapshot { get; }
CommandResult Execute(GameCommand command);
IReadOnlyList<GameCommand> GetLegalCommands(PlayerId player);
void Reset();
```

標準ルールでゲームを開始し、移動Commandを実行する最小例は次のとおりです。

```csharp
var session = new GameSession(GameDefinition.CreateStandard());

var command = new MovePieceCommand(
    PlayerId.Player1,
    new PieceId(1),
    new GridPosition(0, 2));

CommandResult result = session.Execute(command);
GameSnapshot latestSnapshot = session.Snapshot;
```

`GetLegalCommands`は現在の手番プレイヤーが実行できるCommandを返します。`Execute`はCommandを検証して状態を更新し、結果を`CommandResult`として返します。`Reset`は`InitialPieceDefinition`から全駒を作り直し、手番と勝敗も初期状態へ戻します。

Ruleの差し替え例は[拡張ガイド](EXTENSION_GUIDE.md)を参照してください。

## 5. Command

| Command | コンストラクタ引数 | 意味 |
|---|---|---|
| `MovePieceCommand` | `player`, `pieceId`, `destination` | 指定した自分の駒を目的地へ動かす要求 |
| `FusePiecesCommand` | `player`, `firstPieceId`, `secondPieceId` | 隣接する自駒2個の確率合体を試みる要求 |
| `RandomizePowerCommand` | `player`, `pieceId` | 効果のない自駒の通常戦闘力を1〜3へ変更する要求 |

```csharp
var move = new MovePieceCommand(
    PlayerId.Player1,
    new PieceId(1),
    new GridPosition(0, 2));

var fusion = new FusePiecesCommand(
    PlayerId.Player1,
    new PieceId(1),
    new PieceId(3));

var randomize = new RandomizePowerCommand(
    PlayerId.Player1,
    new PieceId(1));
```

Commandの検証順序は[アーキテクチャ §4](ARCHITECTURE.md#4-commandとresult)を参照してください。

## 6. `CommandResult`

呼び出し側は次のように成功・失敗を確認します。Presentationは成功時の`Events`と最新の`Snapshot`を使って表示を更新します。

```csharp
CommandResult result = session.Execute(move);

if (result.Success)
{
    IReadOnlyList<GameEvent> events = result.Events;
}
else
{
    CommandFailureReason reason = result.FailureReason;
}
```

失敗理由の一覧と意味は[アーキテクチャ §4](ARCHITECTURE.md#4-commandとresult)を参照してください。

## 7. Eventが保持する値

| Event | 主な値 |
|---|---|
| `PieceMoved` | `PieceId`, `From`, `To` |
| `CombatResolved` | 攻撃・防御のID、戦闘前後の戦闘力 |
| `PiecePowerChanged` | `PieceId`, `PreviousPower`, `CurrentPower` |
| `PieceDestroyed` | `PieceId`, `Position` |
| `PiecesFused` | 合体元2個と合体後の`PieceId`、`Bonus` |
| `FusionAttemptFailed` | 合体を試みた2個の`PieceId` |
| `CellEffectTriggered` | `EffectId`, `PieceId`, `Position` |
| `CellEffectExpired` | `EffectId`, `PieceId`, `Position` |
| `ReservePieceAdded` | 追加された`ReservePieceState` |
| `RandomizePowerEvent` | `PieceId`, `PreviousPower`, `NewPower` |
| `TurnChanged` | 交代前後の`PlayerId`, `TurnWasPassed` |
| `GameEnded` | `Winner`, `IsDraw` |

各Eventがどういうときに発生するかは[アーキテクチャ §5](ARCHITECTURE.md#5-event)を参照してください。

## 8. Rule差し替えで使う型

`ICombatResolver`、`IFusionResolver`、`ICellEffectHandler`を実装するときに受け取る型と返す型です。差し替え口そのものの一覧は[アーキテクチャ §7](ARCHITECTURE.md#7-ruleとplayeragentの差し替え口)、実装手順は[拡張ガイド](EXTENSION_GUIDE.md)を参照してください。

| 型 | 用途 | 生成方法・引数 |
|---|---|---|
| `CombatResolution` | `ICombatResolver.Resolve`の戻り値 | `damageToAttacker`, `damageToDefender` |
| `FusionPair` | `IFusionResolver.GetLegalFusions`が返す一覧の要素 | `firstPieceId`, `secondPieceId` |
| `FusionResolution` | `IFusionResolver.TryResolve`の`out`値 | `Success(resultingPiece, bonus)`／`Attempted()` |
| `CellEffectContext` | `ICellEffectHandler.Apply`の引数 | `snapshot`, `piece`, `cell`, `definition` |
| `CellEffectResult` | `ICellEffectHandler.Apply`の戻り値 | `piece`, `events`, `reservePieceGrants`（後二者は省略可） |

`CombatResolution`と`FusionPair`は値型（`readonly struct`）、残りは参照型です。

```csharp
var resolution = new CombatResolution(
    defender.EffectiveCombatPower,
    attacker.EffectiveCombatPower);

var pair = new FusionPair(new PieceId(1), new PieceId(3));

var result = new CellEffectResult(context.Piece.WithCombatPower(3));
```

`CombatResolution`のダメージ量は0以上でなければなりません。`GameSession`が一時戦闘力、通常戦闘力の順にダメージを適用し、通常戦闘力が0以下になった駒を削除します。

`CellEffectResult.Piece`は、ID・所有者・位置が`CellEffectContext.Piece`と同一で、`MovementProfileId`が`GameDefinition`へ登録済みでなければなりません。戦闘力、効果状態、登録済みプロファイルを変更でき、`ReservePieceGrant`でリザーブ追加を要求できます。

`FusionResolution.ResultingPiece`が満たすべき条件と、違反した場合に送出される例外は[拡張ガイド §4](EXTENSION_GUIDE.md#4-合体ルールを変更する)にあります。
