# 起動フォールバック JSON 形式

このページでは、起動フォールバックルールファイルに使用する JSON 構造を説明します。

## トップレベル構造

各フォールバックファイルは `rules` 配列を 1 つ持ちます。

```json
{
  "rules": [
    {
      "name": "マイゲーム",
      "kind": "uri-template",
      "enabled": true,
      "matchFileNames": ["MyGame.exe"],
      "uriTemplate": "mylauncher://launch/{appId}",
      "appIdSource": "static:12345"
    }
  ]
}
```

## ルールオブジェクトのフィールド

| フィールド | 型 | 必須 | 説明 |
|-----------|-----|------|------|
| `name` | string | | 識別用の表示名。 |
| `kind` | string | ✓ | `"command-template"` または `"uri-template"`。 |
| `enabled` | boolean | | デフォルト: `true`。`false` にするとルールを無効化できます。 |
| `fallbackTrigger` | string | | `"always"` または `"access-denied"`（デフォルト）。 |
| `matchFileNames` | string[] | | このルールが適用される実行ファイル名のリスト。 |
| `pathContains` | string | | フルパスに含まれる文字列で一致させます。 |
| `fileNameTemplate` | string | | ランチャー実行ファイルのパス。`kind` が `"command-template"` のときに使用。 |
| `argumentsTemplate` | string | | ランチャーへのコマンドライン引数。 |
| `uriTemplate` | string | | 開く URI。`kind` が `"uri-template"` のときに使用。 |
| `appId` | string | | 静的な App ID。`appIdSource` より優先されます。 |
| `appIdSource` | string | | 実行時に `{appId}` を解決する方法。後述の [App ID ソース](#app-id-sources) を参照。 |
| `product` | string | | テンプレートの `{product}` に代入する値。 |
| `patchline` | string | | テンプレートの `{patchline}` に代入する値。デフォルト: `"live"`。 |

### `kind`

| 値 | 動作 |
|----|------|
| `"command-template"` | `fileNameTemplate` と `argumentsTemplate` からコマンドを組み立てて実行します。 |
| `"uri-template"` | `uriTemplate` を展開してシェル経由で開きます（リンクをクリックする動作と同等）。 |

### `fallbackTrigger`

| 値 | ルールが適用されるタイミング |
|----|--------------------------|
| `"always"` | 一致する実行ファイルを起動するたび。元の実行ファイルの代わりにフォールバックを使用します。 |
| `"access-denied"` | 直接起動がアクセス拒否エラーで失敗したときのみ。デフォルトです。 |

### `matchFileNames` と `pathContains`

これらのフィールドは、どの実行ファイルにルールを適用するかを絞り込みます。
ルールが発動するには少なくとも一方が一致する必要があります。

- `matchFileNames` — ファイル名のリスト。大文字小文字を区別せずに照合します（例: `["MyGame.exe"]`）。
- `pathContains` — フルパスに対して部分一致します。`\` は `/` に正規化してから照合します（例: `"steamapps/common/"`）。

両方を指定した場合は両方が一致する必要があります。

## テンプレートプレースホルダー

`fileNameTemplate`・`argumentsTemplate`・`uriTemplate` では以下のプレースホルダーを使用できます。
Windows 環境変数（例: `%ProgramFiles%`）も展開されます。

| プレースホルダー | 値 |
|----------------|-----|
| `{appId}` | 解決された App ID（`appId` フィールドまたは `appIdSource` による）。 |
| `{product}` | `product` フィールドの値。 |
| `{patchline}` | `patchline` フィールドの値（デフォルト: `"live"`）。 |
| `{launchPath}` | 起動した実行ファイルのフルパス。 |
| `{launchDirectory}` | 起動した実行ファイルが含まれるディレクトリ。 |
| `{launchFileName}` | 起動した実行ファイルのファイル名（拡張子あり）。 |
| `{launchFileStem}` | 起動した実行ファイルのファイル名（拡張子なし）。 |
| `{launchPathQuoted}` | `{launchPath}` をダブルクォートで囲んだもの。 |
| `{launchDirectoryQuoted}` | `{launchDirectory}` をダブルクォートで囲んだもの。 |
| `{ancestorPath:Name}` | `Name` という名前を持つ最も近い祖先ディレクトリのパス。 |
| `{ancestorPathQuoted:Name}` | `{ancestorPath:Name}` をダブルクォートで囲んだもの。 |

### `{ancestorPath:Name}`

起動した実行ファイルからディレクトリツリーをさかのぼり、`Name` と一致する名前のディレクトリを見つけてそのフルパスを返します（大文字小文字を区別しません）。

例 — Riot Games のランチャーはゲームから相対的に固定された場所にあります:

```json
"fileNameTemplate": "{ancestorPath:Riot Games}\\Riot Client\\RiotClientServices.exe"
```

ゲームが `C:\Riot Games\VALORANT\live\VALORANT.exe` にある場合、
`C:\Riot Games\Riot Client\RiotClientServices.exe` に展開されます。

## App ID ソース { #app-id-sources }

`appIdSource` フィールドは実行時に `{appId}` をどのように解決するかを制御します。
`appId` フィールドも設定されている場合はそちらが優先され、`appIdSource` は無視されます。

### `steam-manifest`

起動した実行ファイルと同じディレクトリにある Steam の `appmanifest_*.acf` ファイルを読み取り、Steam App ID を取得します。

```json
"appIdSource": "steam-manifest"
```

実行ファイルが `steamapps/common/<ゲーム名>/` 内にある Steam ゲームに使用します。

### `registry:<hive>:<keyPath>:<valueName>`

Windows レジストリから文字列値を読み取ります。

書式:
```
registry:HIVE:KEY_PATH:VALUE_NAME
```

使用可能なハイブ:

| ハイブ | 説明 |
|--------|------|
| `HKEY_LOCAL_MACHINE` | システム全体のレジストリ（64 ビットビューを使用）。 |
| `HKEY_CURRENT_USER` | ユーザーごとのレジストリ。 |
| `HKEY_CLASSES_ROOT` | クラス登録のマージビュー。 |
| `HKEY_USERS` | 全ユーザーのハイブ。 |
| `HKEY_CURRENT_CONFIG` | ハードウェアプロファイル。 |

例:

```json
"appIdSource": "registry:HKEY_LOCAL_MACHINE:SOFTWARE\\WOW6432Node\\Ubisoft\\Launcher\\Installs\\12345:UplayId"
```

`12345` は対象ゲームのレジストリサブキーに置き換えてください。

### `static:<value>`

ランタイムの検索なしに固定文字列を App ID として使用します。

```json
"appIdSource": "static:MyGameAppId"
```

同じ効果として `appId` フィールドを直接設定することもできます:

```json
"appId": "MyGameAppId"
```
