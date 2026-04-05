# applanch

[English](README.md) | [日本語](README.ja.md)

**[ドキュメント](https://docs.applanch.com/ja/)**

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
- .NET 10 ランタイム（リリース版は自己完結形式なので、別途インストールは不要です）

## インストール（推奨）

1. GitHub の Releases を開く
2. 最新アセットから次のいずれかを選択
  - ポータブル版: applanch-<version>-<rid>.zip
  - インストーラー版: applanch-<version>-<rid>-installer.exe
3. ZIP の場合は任意フォルダに展開して applanch.exe を実行
4. インストーラー EXE の場合は起動してセットアップウィザードに従う

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

### ドキュメント

プロジェクトのドキュメントは `docs/` 配下で管理し、MkDocs + Material for MkDocs で公開します。

ローカル確認:

```powershell
python -m pip install -r docs/requirements.txt
python -m mkdocs serve
```

GitHub Pages 用 workflow により、`master` への push、リリース公開時、手動実行時にサイトを公開します。

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

クイックスタート（推奨）:

```powershell
.\scripts\setup-dev-environment.ps1 -SetupDevSigning -BuildSparseMsix
```

このスクリプトは、前提チェック、初回 Debug ビルド、開発用証明書セットアップ、
スパース MSIX 作成までをまとめて実行します。
必要に応じて、開発用証明書セットアップ時に UAC 昇格確認が表示されます。

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
- ローカル署名や外部証明書での署名には `scripts/sign-msix.ps1` と `MSIX_SIGNING_CERT_BASE64` / `MSIX_SIGNING_CERT_PASSWORD` を利用できます。GitHub Actions のリリース workflow では、一時的な自己署名証明書 (`CN=applanch`) を生成して CI 署名します。
- 署名なしでは、端末ポリシーによってスパースパッケージ登録が失敗する場合があります。

### コンテキストメニューのポリシー要件

Windows 11 の簡易コンテキストメニューには、スパース MSIX 登録（`Add-AppxPackage -ExternalLocation`）が必要です。
この操作は**システム全体のポリシー**で制御されており、アプリ単位の例外は存在しません。

エラー `0x80073D2E` で登録が失敗する場合は、以下のいずれかの方法を使用してください。

**方法 1 — 開発者モード（個人開発者向けの最も簡単な方法）**

Windows の設定 → システム → 開発者向け → 開発者モード → オン

**方法 2 — AppModelUnlock レジストリキー（MDM 非管理端末、管理者権限が必要）**

```powershell
$path = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock'
Set-ItemProperty -Path $path -Name AllowAllTrustedApps               -Value 1 -Type DWord
Set-ItemProperty -Path $path -Name AllowDevelopmentWithoutDevLicense  -Value 1 -Type DWord
```

「開発者モード」をオンにすると内部的にこれらのキーが設定されます。
MDM 管理端末では `HKLM:\SOFTWARE\Policies\Microsoft\Windows\Appx` のポリシーキーがこれらを上書きするため、IT 部門の対応なしでは方法 2 は効果がありません。

**方法 3 — グループポリシー / MDM（企業・管理端末向け）**

IT 管理者に Intune（または非管理端末ではローカルグループポリシー）で以下を設定してもらってください。

`コンピューターの構成 → 管理用テンプレート → Windows コンポーネント → アプリパッケージの展開`

- *すべての信頼されたアプリのインストールを許可する* → **有効**
- *統合開発環境 (IDE) からの Windows ストア アプリの開発とインストールを許可する* → **有効**

**フォールバック — 従来のコンテキストメニュー**

上記のいずれも適用できない場合でも、COM ベースの HKCU レジストリ登録は有効なため、
「その他のオプションを確認」（従来のコンテキストメニュー）からコマンドを実行できます。

## プロジェクト構成

- src/applanch: WPF アプリ本体
- src/applanch/Infrastructure: 責務別サービス群
- src/applanch/ViewModels: UI ViewModel
- src/applanch/Controls: カスタムコントロール
- docs: MkDocs ドキュメントソース
- tests/applanch.Tests: xUnit テストプロジェクト

## バージョニングとリリース

- バージョンは MinVer により Git タグ（接頭辞 v）から決定
- リリース成果物はランタイムごとに 2 形式で公開
  - ポータブル ZIP
  - インストーラー EXE
- 通常のリリース手順

```powershell
git tag v0.3.1
git push origin master
git push origin v0.3.1
```

## ライセンス

このプロジェクトは MIT License のもとで提供されます。
[LICENSE](LICENSE) を参照してください。

