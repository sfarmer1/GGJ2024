using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MoonWorks.Graphics;

namespace Tactician.Data;

[JsonSerializable(typeof(CramTextureAtlasData))]
internal partial class CramTextureAtlasDataContext : JsonSerializerContext {
}

public static class CramAtlasReader {
    private static readonly JsonSerializerOptions options = new() {
        PropertyNameCaseInsensitive = true
    };

    private static readonly CramTextureAtlasDataContext context = new(options);

    public static void ReadTextureAtlas(GraphicsDevice graphicsDevice, TexturePage texturePage) {
        var data = (CramTextureAtlasData)JsonSerializer.Deserialize(File.ReadAllText(texturePage.JsonFilename),
            typeof(CramTextureAtlasData), context);
        texturePage.Load(graphicsDevice, data);
    }
}