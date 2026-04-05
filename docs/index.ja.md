# applanch ドキュメント

このサイトには applanch のドキュメントをまとめています。

## ドキュメント構成

### ユーザーガイド

- [インストールガイド](install.ja.md)
- [クイックスタート](user/getting-started.ja.md)
- [アイテムとカテゴリー](user/items.ja.md)
- [設定リファレンス](user/settings.ja.md)
- [テーマ](user/themes.ja.md)
- [起動フォールバック](user/launch-fallbacks.ja.md)
- [Windows 統合](user/windows-integration.ja.md)
- [自動更新](user/updates.ja.md)

### 開発者ガイド

- [コントリビューション](dev/contributing.ja.md)
- [リリースガイド](dev/release.ja.md)

## スコープ

このドキュメントは、リポジトリの README よりも詳しい運用・利用手順を中心にまとめています。

## ローカル確認

依存関係をインストールしてからローカルサーバーを起動します。

```powershell
python -m pip install -r docs/requirements.txt
python -m mkdocs serve
```

## GitHub Pages

このリポジトリでは GitHub Actions を使って GitHub Pages にドキュメントを公開します。
