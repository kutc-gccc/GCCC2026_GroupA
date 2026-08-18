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

`PieceState`は不変です。変更は`WithPosition`、`WithCombatPower`、`WithMovementProfile`、`WithAttributes`で新しいインスタンスを作成します。

```csharp
PieceState movedPiece = piece.WithPosition(new GridPosition(2, 2));
PieceState damagedPiece = movedPiece.WithCombatPower(1);
```

この例でも元の`piece`と`movedPiece`は変更されません。`combatPower`に0以下を渡すと`ArgumentOutOfRangeException`になります。戦闘力が0以下になった駒は、戦闘力0の`PieceState`として残すのではなく、`GameSession`の管理対象から削除されます。

### 戦闘力別移動プロファイル

`PowerMovementBand`は、戦闘力の範囲とその範囲で許可する方向を定義します。`PowerMovementProfile`は複数の帯域をまとめ、戦闘力1から`int.MaxValue`までを隙間・重複なく覆う必要があります。

```csharp
var profile = new PowerMovementProfile(
    new MovementProfileId("standard"),
    new[]
    {
        new PowerMovementBand(1, 1, MoveDirections.All),
        new PowerMovementBand(
            2,
            2,
            MoveDirections.All & ~MoveDirections.NorthEast),
        new PowerMovementBand(
            3,
            3,
            MoveDirections.All & ~MoveDirections.SouthEast),
        new PowerMovementBand(
            4,
            4,
            MoveDirections.All & ~MoveDirections.NorthWest),
        new PowerMovementBand(
            5,
            5,
            MoveDirections.All & ~MoveDirections.SouthWest),
        new PowerMovementBand(
            6,
            6,
            MoveDirections.All & ~MoveDirections.West),
        new PowerMovementBand(
            7,
            7,
            MoveDirections.All & ~MoveDirections.East),
        new PowerMovementBand(8, int.MaxValue, MoveDirections.All)
    });

MoveDirections power1Directions = profile.GetDirections(1); // All
MoveDirections power2Directions = profile.GetDirections(2); // 右上以外
MoveDirections power7Directions = profile.GetDirections(7); // 右以外
```

帯域に隙間・重複がある、戦闘力1から始まらない、最後が`int.MaxValue`まで届かない場合は`ArgumentException`になります。`ProfileMoveDirectionResolver`は`PieceState.MovementProfileId`でプロファイルを選び、現在の`CombatPower`に対応する方向を返します。

## 3. 盤面定義と実行時状態

| クラス | 役割 | 主な内容 |
|---|---|---|
| `CellDefinition` | 1マスの固定設定 | 座標、陣地所有者、特殊効果ID |
| `InitialPieceDefinition` | リセット時に生成する1個の駒の初期設定 | ID、所有者、初期位置、初期戦闘力、移動プロファイルID |
| `GameDefinition` | ゲーム開始前の設計図 | 盤面サイズ、全セル、全初期駒、先手、移動プロファイル |
| `GameSnapshot` | ある時点のゲーム状態を外部へ見せる読み取り専用コピー | 現在の全駒、セル、手番、勝者、引き分け |

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

`GameSnapshot`は`GameSession.Snapshot`から取得します。コンストラクタはCore内部専用なので、Presentationや将来のCPUが直接生成することはありません。

```csharp
GameSnapshot snapshot = session.Snapshot;

bool isInside = snapshot.IsInside(new GridPosition(2, 1));
bool found = snapshot.TryGetPiece(new PieceId(1), out PieceState currentPiece);
int player1PieceCount = snapshot.GetPieceCount(PlayerId.Player1);
bool isGameOver = snapshot.IsGameOver;
```

### 4クラスの関係

```text
GameDefinition（ゲーム全体の設計図）
├─ CellDefinition（各マスの固定設定）
├─ PowerMovementProfile（戦闘力別移動設定）
└─ InitialPieceDefinition（各駒の初期設定とプロファイルID）
           ↓ GameSessionの開始・Reset
       PieceState（各駒の現在状態）
           ↓ Snapshot取得
       GameSnapshot（盤面全体の読み取り専用コピー）
```

## 4. `GameSession`

コンストラクタではゲーム定義と、必要に応じて差し替えるRuleを受け取ります。

| 引数 | 型 | 省略時 |
|---|---|---|
| `definition` | `GameDefinition` | 省略不可 |
| `movementRule` | `IMovementRule` | `DirectionalMovementRule` |
| `combatResolver` | `ICombatResolver` | `SimultaneousCombatResolver` |
| `fusionResolver` | `IFusionResolver` | `DisabledFusionResolver` |
| `cellEffectHandlers` | `IEnumerable<ICellEffectHandler>` | 効果なし |

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
| `FusePiecesCommand` | `player`, `firstPieceId`, `secondPieceId` | 指定した2個の駒を合体する要求。標準ルールでは無効 |

```csharp
var move = new MovePieceCommand(
    PlayerId.Player1,
    new PieceId(1),
    new GridPosition(0, 2));

var fusion = new FusePiecesCommand(
    PlayerId.Player1,
    new PieceId(1),
    new PieceId(3));
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
| `PiecesFused` | 合体元2個と合体後の`PieceId` |
| `CellEffectTriggered` | `EffectId`, `PieceId`, `Position` |
| `TurnChanged` | 交代前後の`PlayerId`, `TurnWasPassed` |
| `GameEnded` | `Winner`, `IsDraw` |

各Eventがどういうときに発生するかは[アーキテクチャ §5](ARCHITECTURE.md#5-event)を参照してください。
