# applanch ドキュメント

このサイトは applanch のドキュメントサイトです。

## ここに置く内容

- ポータブル版とインストーラー版の導入手順
- リリースや配布に関する補足情報
- README には長すぎる運用メモや詳細説明

## ローカル確認

依存関係をインストールしてからローカルサーバーを起動します。

```powershell
python -m pip install -r docs/requirements.txt
python -m mkdocs serve
```

## GitHub Pages

このリポジトリでは GitHub Actions を使って GitHub Pages にドキュメントを公開します。
