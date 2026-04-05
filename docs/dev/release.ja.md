# リリースガイド

## バージョン管理

applanch は [MinVer](https://github.com/adamralph/minver) を使用し、Git タグからバージョンを自動計算します。
タグは `vMAJOR.MINOR.PATCH` または `vMAJOR.MINOR.PATCH-suffix` の形式に従う必要があります（例：`v0.3.1`、`v1.0.0-rc1`）。

プレリリースサフィックス（`-rc1`、`-beta.1` など）を含むタグは、GitHub Releases でプレリリースとして公開されます。

## 通常のリリース手順

```powershell
git tag v0.3.1
git push origin master
git push origin v0.3.1
```

タグをプッシュすると、CD Release ワークフロー（`.github/workflows/cd-release.yml`）が自動的にトリガーされます。

## リリースワークフローの概要

CD ワークフローは 4 つのジョブで構成されています。

| ジョブ | ランナー | 内容 |
|--------|---------|------|
| `metadata` | ubuntu-latest | タグ形式を検証し、バージョン文字列とプレリリースフラグを解決します。 |
| `verify` | windows-latest | タグのコミットを対象にリストア・フルテストスイートを実行します。 |
| `publish-assets` | windows-latest（マトリックス） | 各 RID に対してパブリッシュ、インストーラーとスパース MSIX のビルド、アセットのアーカイブを行います。 |
| `release` | ubuntu-latest | GitHub Release を作成してすべての成果物をアップロードします。 |

`publish-assets` は `win-x64` と `win-x86` で並列実行されます。
すべてのマトリックスが成功するまでリリースは作成されません。

## リリース成果物

各 RID につき 4 ファイルが生成されます。

| ファイル | 説明 |
|---------|------|
| `applanch-<version>-<rid>.zip` | ポータブルな自己完結型単一ファイル実行ファイル + 設定 |
| `applanch-<version>-<rid>.zip.sha256` | ZIP の SHA-256 チェックサム |
| `applanch-<version>-<rid>-installer.exe` | Inno Setup インストーラー |
| `applanch-<version>-<rid>-installer.exe.sha256` | インストーラーの SHA-256 チェックサム |

対応 RID：`win-x64`、`win-x86`。

## 手動実行

タグをプッシュせずにワークフローを手動でトリガーすることもできます。

1. **Actions → CD Release → Run workflow** を開きます。
2. タグを `v1.2.3` の形式で入力します（タグはリポジトリに既に存在する必要があります）。
3. **Run workflow** をクリックします。

ワークフローの問題を修正した後に再実行する場合などに便利です。

## CI ワークフロー

CI ワークフロー（`.github/workflows/ci.yml`）は `master` へのプッシュとプルリクエスト時に実行されます。
共通のビルドステップの後、`format`・`test`・`lint` の 3 ジョブが並列実行されます。
PR のマージには 3 つすべてが通過する必要があります。

## ドキュメントの公開

ドキュメントサイトは `.github/workflows/docs.yml` によって GitHub Pages にデプロイされます。
`master` へのプッシュ、リリースの公開時、手動実行時に更新されます。
