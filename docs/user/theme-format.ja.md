# テーマ JSON 形式

このページでは、カスタムテーマで使う JSON 構造を説明します。

## トップレベル構造

テーマファイルは `themes` 配列を使います。

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
        { "key": "Brush.AppBackground", "hex": "#1E1E2E" }
      ]
    }
  ]
}
```

## テーマオブジェクトのフィールド

| フィールド | 型 | 必須 | 補足 |
|------------|----|------|------|
| `id` | string | ✓ | テーマの内部 ID です。`entriesFrom` からの参照にも使われ、小文字に正規化されます。既存 ID を再利用するとそのテーマを上書きします。 |
| `displayNames` | `{ [languageCode]: string }` | | 設定画面に表示する名前です。 |
| `entries` | array | | このテーマで直接定義するブラシ値です。 |
| `entriesFrom` | `string` または `{ light?: string, dark?: string }` | | 別テーマのブラシ値を継承します。 |

### `id`

- 新しいテーマでは一意な文字列を使います。
- 組み込み ID には `system`、`light`、`dark` があります。
- `light` や `dark` を再利用すると、その組み込みテーマを上書きします。

### `displayNames`

言語コードをキー、表示名を値にしたオブジェクトです。

```json
"displayNames": {
  "en": "My Theme",
  "ja": "マイテーマ"
}
```

形:

```ts
{
  [languageCode: string]: string
}
```

- 現在対応している `languageCode`:
  - `en`
  - `ja`
- 省略した場合、組み込みテーマは既定のローカライズ済み表示名を使います。
- カスタムテーマは `id` から生成したタイトル風の名前にフォールバックします。

### `entries`

`entries` は次の形のオブジェクト配列です。

```json
{ "key": "Brush.AppBackground", "hex": "#1E1E2E" }
```

| フィールド | 型 | 必須 | 補足 |
|------------|----|------|------|
| `key` | string | ✓ | UI で使うブラシリソース名です。 |
| `hex` | string | ✓ | `#RRGGBB` または `#AARRGGBB` 形式の色です。 |

不足しているブラシキーは、継承元または組み込みテーマの値にフォールバックします。

## `entriesFrom`

`entriesFrom` は、すべてのブラシを毎回書かずに既存テーマを再利用したいときに使います。

常に 1 つのテーマを継承する例:

```json
{
  "id": "my-light-copy",
  "entriesFrom": "light"
}
```

Windows のカラーモードに応じて継承元を切り替える例:

```json
{
  "id": "my-dark-variant",
  "entriesFrom": {
    "light": "light",
    "dark": "dark"
  }
}
```

| 形式 | 型 | 意味 |
|------|----|------|
| `"light"` | string | 常に 1 つのテーマ ID を継承します。 |
| `{ "light": "...", "dark": "..." }` | `{ light?: string, dark?: string }` | Windows のライト/ダーク設定に応じて継承元テーマを切り替えます。 |

オブジェクトの形:

```json
{
  light?: string;
  dark?: string;
}
```

プロパティ名には `light` と `dark` を使い、値にはテーマ ID を指定します。

`entries` と `entriesFrom` を併用した場合、`entries` でこのテーマ独自の色を定義し、足りないキーを `entriesFrom` の参照先から補います。

## 使用可能なブラシキー

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