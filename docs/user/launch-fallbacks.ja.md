# 起動フォールバック

登録した実行ファイルを直接起動できない場合（ゲームが別のランチャー経由での起動を要求する場合など）、applanch は自動的に適切なランチャー経由でリダイレクトします。

組み込みルールは `Config/launch-fallbacks.json` にあります。
カスタムルールは `Config/UserDefined/launch-fallbacks/*.json` に追加してください。

## 組み込みルール

### デフォルトで有効なルール

| ルール | トリガー | 方式 | App ID ソース |
|--------|---------|------|--------------|
| Riot VALORANT | 常時 | `RiotClientServices.exe` 経由で起動 | — |
| Riot League of Legends | 常時 | `RiotClientServices.exe` 経由で起動 | — |
| Steam ライブラリの実行ファイル | 常時 | `steam://rungameid/{appId}` URI 経由で起動 | `steam-manifest` |

### サンプル（デフォルトで無効）

以下のルールはサンプルとして含まれています。有効にしてプレースホルダーの値を置き換えることで使用できます。

| ルール | ランチャー | URI テンプレート |
|--------|---------|----------------|
| Epic Games サンプル | Epic Games Launcher | `com.epicgames.launcher://apps/{appId}?action=launch&silent=true` |
| Ubisoft Connect サンプル | Ubisoft Connect | `uplay://launch/{appId}/0` |
| EA app サンプル | EA app | `ea://launchgame/{appId}` |
| Battle.net サンプル | Battle.net | `battlenet://{appId}` |

## カスタムルールを追加する

組み込みエントリーにないゲームランチャー向けにルールを追加するには：

1. `Config/UserDefined/launch-fallbacks/` 配下に JSON ファイルを作成します（例: `ubisoft.json`）。
2. `rules` 配列の中にルールを追加します。
3. 有効化したいルールは `"enabled": true` に設定します。
4. 適切な `matchFileNames` とテンプレートフィールドを入力します。
5. applanch を再起動します。

**例 — Ubisoft Connect（有効化済み）:**

```json
{
  "rules": [
    {
      "name": "マイ Ubisoft ゲーム",
      "kind": "uri-template",
      "enabled": true,
      "matchFileNames": ["MyGame.exe"],
      "uriTemplate": "uplay://launch/{appId}/0",
      "appIdSource": "registry:HKEY_LOCAL_MACHINE:SOFTWARE\\WOW6432Node\\Ubisoft\\Launcher\\Installs\\12345:UplayId"
    }
  ]
}
```

`12345` は対象ゲームのレジストリキーに置き換えてください。

すべてのルールフィールド、テンプレートプレースホルダー、App ID ソースの詳細は [起動フォールバック形式](launch-fallback-format.md) を参照してください。
