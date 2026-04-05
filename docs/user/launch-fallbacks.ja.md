# 起動フォールバック

登録した実行ファイルを直接起動できない場合（ゲームが別のランチャー経由での起動を要求する場合など）、applanch は自動的に適切なランチャー経由でリダイレクトします。

フォールバックルールは `Config/launch-fallbacks.json` で定義されています。

## 組み込みルール

### デフォルトで有効なルール

| ルール | トリガー | 方式 |
|--------|---------|------|
| Riot VALORANT | 常時 | `RiotClientServices.exe` 経由で起動 |
| Riot League of Legends | 常時 | `RiotClientServices.exe` 経由で起動 |
| Steam ライブラリの実行ファイル | 常時 | `steam://rungameid/{appId}` URI 経由で起動 |

### サンプル（デフォルトで無効）

以下のルールはサンプルとして含まれています。有効にしてプレースホルダーの値を置き換えることで使用できます。

| ルール | ランチャー | URI テンプレート |
|--------|---------|----------------|
| Epic Games サンプル | Epic Games Launcher | `com.epicgames.launcher://apps/{appId}?action=launch&silent=true` |
| Ubisoft Connect サンプル | Ubisoft Connect | `uplay://launch/{appId}/0` |
| EA app サンプル | EA app | `ea://launchgame/{appId}` |
| Battle.net サンプル | Battle.net | `battlenet://{appId}` |

## ルールの構造

`launch-fallbacks.json` の各ルールは以下のフィールドを持ちます。

| フィールド | 説明 |
|-----------|------|
| `name` | 識別用の表示名 |
| `kind` | `command-template` — 実行ファイルを起動；`uri-template` — URI を開く |
| `enabled` | `true` にするとルールが有効になる |
| `fallbackTrigger` | `always` — 常にフォールバックを使用；`access-denied` — アクセス拒否エラー時のみ |
| `matchFileNames` | このルールが適用される実行ファイル名のリスト |
| `pathContains` | パスに含まれる文字列で一致させる（例：`steamapps/common/`） |
| `fileNameTemplate` | ランチャー実行ファイルのパステンプレート（`command-template` 種別） |
| `argumentsTemplate` | コマンドライン引数のテンプレート |
| `uriTemplate` | 開く URI のテンプレート（`uri-template` 種別） |
| `appIdSource` | `{appId}` の解決方法：`steam-manifest` またはレジストリパス |
| `product` | `{product}` プレースホルダーに使用するプロダクト識別子 |
| `patchline` | `{patchline}` プレースホルダーに使用するパッチライン識別子 |

## カスタムルールを追加する

組み込みエントリーにないゲームランチャー向けにルールを追加するには：

1. テキストエディターで `Config/launch-fallbacks.json` を開きます。
2. `rules` 配列に新しいオブジェクトを追加します。
3. `"enabled": true` に設定します。
4. 適切な `matchFileNames` とテンプレートフィールドを入力します。
5. applanch を再起動します。

**例 — Ubisoft Connect（有効化済み）:**

```json
{
  "name": "マイ Ubisoft ゲーム",
  "kind": "uri-template",
  "enabled": true,
  "matchFileNames": ["MyGame.exe"],
  "uriTemplate": "uplay://launch/{appId}/0",
  "appIdSource": "registry:HKEY_LOCAL_MACHINE:SOFTWARE\\WOW6432Node\\Ubisoft\\Launcher\\Installs\\12345:UplayId"
}
```

`12345` は対象ゲームのレジストリキーに置き換えてください。
