## 変更内容

<!-- 何を、なぜ変更したかを簡潔に書いてください -->

## 関連Issue

<!-- ある場合のみ。例: Closes #12 -->

## 確認項目

- [ ] 変更目的が1つに限定されている
- [ ] 対応する`.meta`ファイルを含めている
- [ ] `Library`、`Temp`、`Logs`、生成`.csproj`を含めていない
- [ ] Coreに`UnityEngine`や`UnityEditor`参照を追加していない
- [ ] Presentationでゲームルールを重複実装していない
- [ ] 仕様変更に対応する一次情報源の文書を更新した
- [ ] 該当する場合、Core既定値・Config既定値・実際のAssetに重複する設定を同期した
- [ ] 該当する場合、追加したRule・Resolver・Handler・Agentを本番のBootstrapへ接続した
- [ ] 該当する場合、新しいCommandをdispatchへ登録し、新しいEventをViewへ反映した
- [ ] 新しい挙動にEditModeテストがある
- [ ] 表示・入力変更にPlayModeテストがある
- [ ] SampleSceneの通常再生でConsole Errorが0件
- [ ] Game Viewで盤面とHUDが欠けていない
- [ ] Package・ProjectSettings変更が意図したものだけである

<!--
テストの実行方法: docs/TESTING.md
ブランチとPRの運用: docs/DEVELOPMENT.md
-->
