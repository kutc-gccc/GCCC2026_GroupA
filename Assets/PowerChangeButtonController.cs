using UnityEngine;
using GCCC.BoardGame.Core;

public class PowerChangeButtonController : MonoBehaviour
{
    // 現在選択されている駒のIDを保持する（クリック処理などで更新する想定）
    public int selectedPieceId = 1; 

    // ゲーム管理オブジェクトへの参照（環境に合わせて設定）
    // 例: GameSessionを保持しているコンポーネント
    
    // ボタンが押されたときに呼ぶメソッド
    public void OnClickRandomizePower()
    {
        // 現在のGameSessionを取得する処理（プロジェクトの構造に合わせて呼び出し）
        // GameSession session = ...;
        
        // コマンドを作成して実行
        // var command = new ChangeCombatPowerRandomlyCommand(session.Snapshot.CurrentTurn, new PieceId(selectedPieceId));
        // session.Execute(command);

        Debug.Log($"駒ID: {selectedPieceId} の戦闘力変更ボタンが押されました！");
    }
}
