# コントリビューション

applanch へのコントリビューションにご興味をお持ちいただきありがとうございます。

## 事前準備

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 または Windows 11
- C# IDE（Visual Studio 2022 以降または Rider を推奨）

## クローンとビルド

```powershell
git clone https://github.com/ChanyaVRC/applanch.git
cd applanch
dotnet build
```

## テストの実行

```powershell
dotnet test
```

変更箇所に関連するテストを先に実行し、その後プルリクエスト提出前に全テストスイートを実行してください。

## フォーマット確認

コミット前に、すべてのコードがフォーマッターを通過する必要があります。

```powershell
dotnet format applanch.slnx --verify-no-changes --no-restore --verbosity minimal
```

確認が失敗した場合は自動フォーマットを適用します。

```powershell
dotnet format applanch.slnx --no-restore --verbosity minimal
```

その後、再度確認手順を実行してください。

## バージョン管理

applanch は [MinVer](https://github.com/adamralph/minver) を使用し、Git タグからバージョンを自動計算します。
タグは `v` プレフィックス形式に従います（例：`v0.3.1`）。

## ブランチとコミット

- `master` から作業ブランチを切ります。
- 1 コミットにつき 1 つの論理的な変更に留めます。
- コミットメッセージは簡潔な命令形で記述します（例：`Fix launch fallback for Riot VALORANT`）。
- 動作変更と無関係なフォーマット修正を同一コミットに混在させないでください。

## コード規約

- 1 ファイルにつき 1 型。
- `ref`/`out` パラメーターは避け、戻り値を使用する。
- ユーザー向けの文字列はすべてローカライズリソースファイル（`Properties/Resources.resx` および `Properties/Resources.ja.resx`）から提供する。
- XAML や C# に UI テキストをハードコードしない。
- 既存の命名規則に従う：型・メンバーは `PascalCase`、プライベートフィールドは `_camelCase`。

## テストの追加

- バグ修正や動作変更には必ずテストを追加・更新します。
- WPF コントロールをインスタンス化するテストは STA スレッドで実行する必要があります。
- コントロールのテストはグローバルなアプリケーションリソースから切り離して記述します。

## プルリクエスト

1. `dotnet build` と `dotnet test` の両方が通ることを確認します。
2. `dotnet format --verify-no-changes` が通ることを確認します。
3. `master` ブランチに対してプルリクエストを作成します。
4. PR の説明には何を変更したか・その理由を記載します。

## ローカライズ

applanch は英語と日本語を対象としています。
ユーザー向けの文字列を追加する場合：

1. `Properties/Resources.resx` にキーと英語の値を追加します。
2. `Properties/Resources.ja.resx` に日本語訳を追加します。
3. XAML または C# では `LocalizedStrings.Instance` 経由でそのキーを参照します。

## ドキュメント

プロジェクトのドキュメントは `docs/` 以下にあり、MkDocs + Material for MkDocs でビルドされます。

ローカルでのプレビュー：

```powershell
python -m pip install -r docs/requirements.txt
python -m mkdocs serve
```

各ドキュメントページには英語版（`<ページ>.md`）と日本語版（`<ページ>.ja.md`）があります。
ページを追加・更新する際は両方更新してください。
