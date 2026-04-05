# リリースガイド

## バージョン管理

- applanch は `v` プレフィックス付きの Git タグを使用します。
- バージョン計算は MinVer によって行われます。

## 通常のリリース手順

```powershell
git tag v0.3.1
git push origin master
git push origin v0.3.1
```

## リリース成果物

各ランタイム向けに 2 種類の形式で公開されます。

- ポータブル ZIP パッケージ
- インストーラー EXE パッケージ

## ドキュメントの公開

ドキュメントサイトは GitHub Actions によって GitHub Pages にデプロイされます。
`master` へのプッシュ、リリースの公開時、および手動トリガーで更新されます。
