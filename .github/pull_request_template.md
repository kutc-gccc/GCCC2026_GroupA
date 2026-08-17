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
- [ ] 新しい挙動にEditModeテストがある
- [ ] 表示・入力変更にPlayModeテストがある
- [ ] SampleSceneの通常再生でConsole Errorが0件
- [ ] Game Viewで盤面とHUDが欠けていない
- [ ] Package・ProjectSettings変更が意図したものだけである

<!--
テストの実行方法: docs/TESTING.md
ブランチとPRの運用: docs/DEVELOPMENT.md
-->
