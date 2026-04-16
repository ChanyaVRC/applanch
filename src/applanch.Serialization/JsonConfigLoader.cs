namespace applanch.Serialization;

/// <summary>
/// JSON ファイルから型安全にデシリアライズするためのユーティリティクラス。
/// </summary>
public static class JsonConfigLoader
{
    /// <summary>
    /// 指定されたパスから JSON ファイルをデシリアライズします。
    /// </summary>
    /// <typeparam name="T">デシリアライズ対象の型</typeparam>
    /// <param name="path">JSON ファイルのパス</param>
    /// <returns>デシリアライズされたオブジェクト</returns>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合</exception>
    /// <exception cref="InvalidOperationException">デシリアライズに失敗した場合</exception>
    public static T DeserializeFile<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Config file not found.", path);
        }

        var json = File.ReadAllText(path);
        return Deserialize<T>(json);
    }

    /// <summary>
    /// JSON 文字列をデシリアライズします。
    /// </summary>
    /// <typeparam name="T">デシリアライズ対象の型</typeparam>
    /// <param name="json">JSON 文字列</param>
    /// <returns>デシリアライズされたオブジェクト</returns>
    /// <exception cref="InvalidOperationException">デシリアライズに失敗した場合</exception>
    public static T Deserialize<T>(string json)
    {
        return System.Text.Json.JsonSerializer.Deserialize<T>(
            json,
            DefaultSerializerOptions)
            ?? throw new InvalidOperationException(
                $"Failed to deserialize JSON to type '{typeof(T).Name}'.");
    }

    /// <summary>
    /// JSON ファイルをドキュメントとして読み込みます（要素カウントやマージなど）。
    /// </summary>
    /// <param name="path">JSON ファイルのパス</param>
    /// <returns>JSON ドキュメント</returns>
    /// <exception cref="FileNotFoundException">ファイルが見つからない場合</exception>
    public static System.Text.Json.JsonDocument ParseFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Config file not found.", path);
        }

        using var stream = File.OpenRead(path);
        return System.Text.Json.JsonDocument.Parse(
            stream,
            DefaultDocumentOptions);
    }

    /// <summary>
    /// デフォルトの JsonSerializerOptions。
    /// コメント、末尾カンマ、大文字小文字を区別しない設定を含みます。
    /// </summary>
    public static System.Text.Json.JsonSerializerOptions DefaultSerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// デフォルトの JsonDocumentOptions。
    /// コメントと末尾カンマの処理を含みます。
    /// </summary>
    public static System.Text.Json.JsonDocumentOptions DefaultDocumentOptions { get; } = new()
    {
        CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}
