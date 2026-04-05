# テーマ

applanch には 3 つの組み込みテーマがあり、カスタムテーマパレットにも対応しています。

## 組み込みテーマ

| テーマ | 説明 |
|--------|------|
| システムに従う | Windows のライト/ダーク設定に合わせて自動切り替え |
| ライト | 明るい背景・暗いテキスト |
| ダーク | 暗い背景（ネイビー系）・明るいテキスト |

テーマの切り替えは [設定](settings.ja.md) の **外観 → テーマ** から行えます。

## カスタムテーマ

組み込みテーマは `Config/theme-palette.json` から読み込まれます。
ユーザー定義テーマは `Config/UserDefined/theme-palette/*.json` に配置します。

JSON の項目一覧や型は [テーマ JSON 形式](theme-format.ja.md) を参照してください。

### カスタムテーマを追加する

1. `Config/UserDefined/theme-palette/` 配下に JSON ファイルを作成します（例: `my-theme.json`）。
2. そのファイルにカスタムテーマを定義します。
3. applanch を再起動します。

```json
{
  "themes": [
    {
      "id": "my-theme",
      "displayNames": {
        "en": "My Theme",
        "ja": "マイテーマ"
      },
      "entries": [
        { "key": "Brush.AppBackground", "hex": "#1E1E2E" },
        { "key": "Brush.Surface",       "hex": "#27273A" }
      ]
    }
  ]
}
```

このファイルは組み込みテーマにマージされるため、追加・上書きしたい内容だけを定義すれば十分です。

### 別テーマを参照する

`entries` を直接指定する代わりに、`entriesFrom` を使うと既存テーマのブラシ値をすべて継承できます。

- 常に同じテーマを参照したい場合は、文字列でテーマ ID を指定します。
- Windows のライト/ダーク設定に応じて参照先を切り替えたい場合は、`light` と `dark` を持つオブジェクトを指定します。
- 参照先のテーマ ID には、`light` / `dark` のような組み込みテーマだけでなく、別の JSON ファイルで定義したカスタムテーマ ID も使えます。

これは、`entries` を毎回全部書かずに別名テーマを追加したいときや、既存パレットを再利用したいときに便利です。

常に 1 つのテーマを継承する例:

```json
{
  "id": "my-light-copy",
  "displayNames": { "ja": "ライト複製" },
  "entriesFrom": "light"
}
```

Windows のカラーモードに応じて参照先を切り替える例:

```json
{
  "id": "my-dark-variant",
  "displayNames": { "ja": "ダークバリアント" },
  "entriesFrom": {
    "light": "light",
    "dark": "dark"
  }
}
```

Windows の設定がライトのときは `light` 側の参照先テーマ ID が、ダークのときは `dark` 側の参照先テーマ ID が使われます。
これは組み込みの **システムに従う** テーマと同じ仕組みです。
