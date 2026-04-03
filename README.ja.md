# applanch

[English](README.md) | [日本語](README.ja.md)

Windows でアプリ、ファイル、フォルダをすばやく起動するための軽量ランチャーです。

applanch は、カテゴリ管理、クイック追加サジェスト、更新確認機能を備えた WPF デスクトップアプリです。

## 主な機能

- 実行ファイル、フォルダ、登録済みアプリパスのクイック起動
- カテゴリ別の整理とフィルタリング
- サジェスト付きクイック追加
- 起動引数と表示名の編集
- ドラッグ＆ドロップによる並び替えと保存
- ライト / ダーク / システムテーマ対応
- ダークテーマ対応のアプリ内ダイアログ
- GitHub Releases を使った自動更新確認
- 起動時更新確認の ON/OFF
- Windows コンテキストメニュー登録対応
- リソースベースの日本語 / 英語ローカライズ

## 動作要件

- Windows 10/11
- .NET 10 ランタイム（自己完結実行はリリース成果物を利用）

## インストール（推奨）

1. GitHub の Releases を開く
2. 最新の applanch-<version>-<rid>.zip をダウンロード
3. 任意のフォルダに展開
4. applanch.exe を実行

## 使い方

- メイン画面のクイック追加入力から項目を追加
- 項目をクリックして起動
- コンテキストメニューで表示名、カテゴリ、引数を編集
- Settings で次を設定
  - テーマ（System / Light / Dark）
  - 起動後にウィンドウを閉じる
  - 起動時に更新を確認
  - デバッグ更新モード

## 開発

### ビルド

```powershell
dotnet build
```

### テスト

```powershell
dotnet test
```

### フォーマット確認

```powershell
dotnet format applanch.slnx --verify-no-changes --no-restore --verbosity minimal
```

### 実行

```powershell
dotnet run --project src/applanch/applanch.csproj
```

### スパース MSIX（コンテキストメニュー用）の作成

Windows 11 の簡易コンテキストメニュー連携を使う場合に実行します。

1. 先にアプリ本体をビルド

```powershell
dotnet build applanch.slnx -c Debug
```

2. スパース MSIX を作成

```powershell
.\scripts\build-sparse-package.ps1
```

3. 既定の出力先は `artifacts/sparse-package/applanch.msix`

ローカル登録テスト用に exe と同じ場所へ出したい場合は `-OutputMsix` を指定します。

```powershell
.\scripts\build-sparse-package.ps1 -OutputMsix .\src\applanch\bin\Debug\net10.0-windows10.0.22000.0\applanch.msix
```

### 新しい開発環境で一から MSIX 作成を有効化する手順

新しい PC で初回のみ実施してください。

1. 前提ツールをインストール
- .NET SDK 10
- Windows SDK（`makeappx.exe` と `signtool.exe` を含む）

2. まずは通常ビルドで環境確認

```powershell
dotnet build applanch.slnx -c Debug
```

3. 開発用コード署名証明書をセットアップ（管理者 PowerShell で実行）

```powershell
.\scripts\setup-dev-signing.ps1
```

4. スパース MSIX を作成

```powershell
.\scripts\build-sparse-package.ps1
```

補足:
- `build-sparse-package.ps1` は `CurrentUser\My` に `CN=applanch` 証明書がある場合、自動で MSIX に署名します。
- OV/EV 証明書を使ったリリース署名は `scripts/sign-msix.ps1` と `MSIX_SIGNING_CERT_BASE64` / `MSIX_SIGNING_CERT_PASSWORD` を利用します。
- 署名なしでは、端末ポリシーによってスパースパッケージ登録が失敗する場合があります。

## プロジェクト構成

- src/applanch: WPF アプリ本体
- src/applanch/Infrastructure: 責務別サービス群
- src/applanch/ViewModels: UI ViewModel
- src/applanch/Controls: カスタムコントロール
- tests/applanch.Tests: xUnit テストプロジェクト

## バージョニングとリリース

- バージョンは MinVer により Git タグ（接頭辞 v）から決定
- 通常のリリース手順

```powershell
git tag v0.3.1
git push origin master
git push origin v0.3.1
```

