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

カスタムテーマは applanch インストールフォルダー内の `Config/theme-palette.json` で定義します。

### カスタムテーマを追加する

`themes` 配列に新しいエントリーを追加します。

```json
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
```

applanch を再起動すると、設定のテーマドロップダウンに新しいテーマが表示されます。

### 使用可能なブラシキー

| キー | 用途 |
|------|------|
| `Brush.AppBackground` | メインウィンドウの背景 |
| `Brush.Surface` | カード・パネル・ダイアログの背景 |
| `Brush.SurfaceBorder` | サーフェス要素の境界線 |
| `Brush.TextPrimary` | 主要テキストの色 |
| `Brush.TextSecondary` | 補助・弱調テキスト |
| `Brush.TextTertiary` | 無効テキスト・スクロールバーのつまみ |
| `Brush.ItemBackground` | アイテム行の背景 |
| `Brush.ItemBorder` | アイテム行の境界線 |
| `Brush.IconBackground` | アイコンプレースホルダーの背景 |
| `Brush.NotificationInfoBackground` | 情報通知の背景 |
| `Brush.NotificationInfoBorder` | 情報通知の境界線 |
| `Brush.NotificationWarningBackground` | 警告通知の背景 |
| `Brush.NotificationWarningBorder` | 警告通知の境界線 |
| `Brush.NotificationErrorBackground` | エラー通知の背景 |
| `Brush.NotificationErrorBorder` | エラー通知の境界線 |
| `Brush.NotificationProgressTrack` | プログレスバーのトラック |
| `Brush.NotificationProgressValue` | プログレスバーの進捗部分 |
| `Brush.QuickAddInfoText` | クイック追加の情報メッセージテキスト |
| `Brush.QuickAddWarningText` | クイック追加の警告メッセージテキスト |

### 別テーマを参照する

`entries` を直接指定する代わりに、`entriesFrom` を使うと既存テーマのブラシ値をすべて継承できます。

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

Windows の設定がライトのときは `light` で指定したテーマ ID が、ダークのときは `dark` で指定したテーマ ID が使われます。
これは組み込みの **システムに従う** テーマと同じ仕組みです。
