# 拡張ガイド

## 1. 拡張時の共通原則

- CoreはUnityに依存させません。座標には`GridPosition`を使用します。
- 状態を直接変更せず、`GameCommand`を`GameSession.Execute`へ送ります。
- 操作の結果は`GameEvent`として返します。
- Presentationは最新`GameSnapshot`とEventだけで表示を更新します。
- 新しい共通契約は、利用側の機能PRより先に共有PRとして確定します。

## 2. 移動ルールを追加する

`IMovementRule`を実装し、駒とSnapshotから合法な移動先を返します。

```csharp
public interface IMovementRule
{
    IReadOnlyList<GridPosition> GetLegalDestinations(
        GameSnapshot snapshot,
        PieceState piece);
}
```

実装時は盤外、自分の陣地、自分の駒、ゲーム終了状態を考慮します。現在の標準実装は`DirectionalMovementRule`です。

差し替え例:

```csharp
var session = new GameSession(
    definition,
    movementRule: new CustomMovementRule());
```

Bootstrapは現在標準Resolverを暗黙に使用しているため、Unity上で選択できるRuleSetを追加する場合は、設定からRuleを生成するFactoryをBootstrapの組み立て処理へ追加します。移動条件を`GameCoordinator`や`BoardView`へ複製しないでください。

必要なテスト:

- 許可された全方向・距離
- 禁止方向、盤外、自陣、自駒上
- 敵駒マスが戦闘候補になること
- `GetLegalCommands`と候補表示の一致

## 3. 戦闘ルールを追加する

`ICombatResolver`は攻撃側と防御側を受け取り、それぞれの残り戦闘力を返します。

```csharp
public interface ICombatResolver
{
    CombatResolution Resolve(PieceState attacker, PieceState defender);
}
```

標準の`SimultaneousCombatResolver`は双方の戦闘力を同時に減算します。新ルールで状態異常や複数Eventが必要になる場合は、既存interfaceだけで表現できるかを先に検討し、必要ならCore契約変更を独立PRにします。

差し替え例:

```csharp
var session = new GameSession(
    definition,
    combatResolver: new CustomCombatResolver());
```

必要なテスト:

- 攻撃側勝利、防御側勝利、相打ち
- 戦闘力の境界値
- 生存位置と`PiecePowerChanged`
- `CombatResolved`、`PieceDestroyed`、`PieceMoved`の整合性
- 相手陣地上の戦闘と勝利判定

## 4. 合体を有効化する

Coreには`FusePiecesCommand`、`FusePiecesCommandHandler`、`IFusionResolver`、`PiecesFused`があります。標準では`DisabledFusionResolver`が使用されます。

実装手順:

1. `IFusionResolver.IsEnabled`をtrueにするResolverを追加します。
2. `GetLegalFusions`で手番プレイヤーの合法な駒ペアを返します。
3. `TryResolve`で合体後の`PieceState`を含む`FusionResolution`を返します。
4. `GameSession`生成時にResolverを注入します。
5. Presentationに「移動」「合体」の操作モードと、2駒選択UIを追加します。
6. `PiecesFused`を`PieceViewManager`で表示へ反映します。

合体後のID、位置、戦闘力、移動方向の計算式はResolverへ閉じ込めます。Viewで計算してはいけません。

必要なテスト:

- 合法・不正な組み合わせ
- 他プレイヤーの駒を含む要求の拒否
- 合体後ID、位置、戦闘力、移動方向
- 合体後の手番交代・自動パス
- 2駒Viewの削除と新Viewの生成

## 5. 特殊マスを追加する

特殊マスは文字列の`EffectId`と`ICellEffectHandler`を対応させます。

```csharp
public interface ICellEffectHandler
{
    string EffectId { get; }
    CellEffectResult Apply(CellEffectContext context);
}
```

実装手順:

1. 効果ごとに`ICellEffectHandler`を実装します。
2. `EffectId`を重複しない固定値にします。
3. Handlerを`GameSession`の`cellEffectHandlers`へ登録します。
4. `StandardBoardGameConfig`の対象座標へ同じIDを設定します。
5. 必要な見た目をBoardViewへ追加しますが、効果計算はCoreに残します。

効果はセルに登録されたID順に実行されます。現在の契約で変更できるのは同じ駒の戦闘力と移動方向です。ID、所有者、位置を変更する結果は`GameSession`に拒否されます。

未登録のEffectIdが発動すると例外になるため、Configだけを先に変更しないでください。

必要なテスト:

- Handlerの呼び出し順
- 戦闘力・移動方向の更新
- 複数効果の累積
- 未登録IDと不正な結果の拒否
- `CellEffectTriggered`と追加Eventの順序

## 6. CPUを追加する

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

CPU実装は将来の`GCCC.BoardGame.AI`アセンブリへ置き、Coreだけを参照します。

実装手順:

1. `IPlayerAgent`を実装します。
2. `BeginTurn`で受け取ったSnapshotと合法Commandだけを評価します。
3. 選んだCommandを`submitCommand`へ1回だけ渡します。
4. `EndTurn`で保留中の思考やcallbackを破棄します。
5. `GameCoordinator`生成時にHumanまたはCPU Agentを注入します。
6. Unity上の選択用設定をBootstrapへ追加します。

CPUはSnapshotを変更したり、`GameSession`の内部状態へアクセスしたりしてはいけません。非同期思考を実装する場合は、手番終了後の古いcallback送信を防止します。

必要なテスト:

- 合法Commandだけを1回送ること
- 手番外やゲーム終了後に送らないこと
- 合法手0件で停止すること
- 同じSnapshotで決定的に動くテスト用Agent
- Human対CPU、CPU対CPUのPlayMode統合

## 7. 新しいCommandやEventを追加する

新しい操作が既存のMoveまたはFuseで表現できない場合にだけCommandを追加します。

1. Core Commandsへ`GameCommand`派生型を追加します。
2. 対応するHandlerを追加し、`GameSession`のdispatchへ登録します。
3. 成功時に必要なEventをCore Eventsへ追加します。
4. 状態変更は必ず`GameSession`内部で行います。
5. Presentationは新Eventを表示へ反映します。
6. 不正Commandで状態が変わらないテストを追加します。

Command、Event、SnapshotはHuman、CPU、テストの共有契約です。破壊的な変更は独立PRにし、利用側を同時に更新します。
