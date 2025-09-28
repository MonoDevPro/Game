using Godot;
using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Simulation.Core.Options;

namespace GodotClient;

public sealed partial class ConfigManager : Node
{
    public WorldOptions? World { get; private set; }
    public NetworkOptions? Network { get; private set; }
    public AuthorityOptions? Authority { get; private set; }

    public override void _Ready()
    {
        LoadSettings();
    }

    private void LoadSettings()
    {
        const string path = "res://appsettings.json";

        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr("appsettings.json não encontrado em res://");
            return;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var json = file.GetAsText();

        // Loga o JSON inteiro para debug
        GD.Print($"[ConfigManager] appsettings.json:\n{json}\n--- end ---");

        // Opções do JsonSerializer
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        options.Converters.Add(new JsonStringEnumConverter()); // tenta desserializar enums por nome

        try
        {
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty(WorldOptions.SectionName, out var worldElem))
            {
                World = JsonSerializer.Deserialize<WorldOptions>(worldElem.GetRawText(), options);
                GD.Print($"[ConfigManager] World loaded: {World?.GetType().Name} (null? {World==null})");
            }

            if (doc.RootElement.TryGetProperty(NetworkOptions.SectionName, out var netElem))
            {
                Network = JsonSerializer.Deserialize<NetworkOptions>(netElem.GetRawText(), options);
                GD.Print($"[ConfigManager] Network loaded: {Network?.GetType().Name} (null? {Network==null})");
            }

            if (doc.RootElement.TryGetProperty(AuthorityOptions.SectionName, out var authElem))
            {
                try
                {
                    // tentativa direta (conversor de enums deve ajudar)
                    Authority = JsonSerializer.Deserialize<AuthorityOptions>(authElem.GetRawText(), options);
                    GD.Print($"[ConfigManager] Authority loaded via JsonSerializer (null? {Authority==null})");
                }
                catch (JsonException je)
                {
                    // Fallback permissivo: monta manualmente a instância (case-insensitive para enums)
                    GD.PrintErr($"[ConfigManager] JsonException ao desserializar Authority: {je.Message}. Tentando fallback permissivo...");
                    Authority = TryBuildFromJsonElement<AuthorityOptions>(authElem);
                    if (Authority != null)
                        GD.Print("[ConfigManager] Authority carregada via fallback permissivo.");
                    else
                        GD.PrintErr("[ConfigManager] Fallback falhou: Authority permanece nula.");
                }
            }
        }
        catch (JsonException je)
        {
            GD.PrintErr("[ConfigManager] JSON inválido em appsettings.json: " + je.Message);
        }
        catch (Exception ex)
        {
            GD.PrintErr("[ConfigManager] Erro inesperado ao carregar config: " + ex);
        }
    }

    // Tenta popular um objeto T a partir de um JsonElement, fazendo parsing case-insensitive para enums.
    private static T? TryBuildFromJsonElement<T>(JsonElement elem) where T : class, new()
    {
        var target = new T();
        var tType = typeof(T);
        foreach (var prop in elem.EnumerateObject())
        {
            // procurar propriedade no tipo alvo (case-insensitive)
            var pi = tType.GetProperty(prop.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pi == null || !pi.CanWrite) continue;

            try
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    var str = prop.Value.GetString()!;
                    if (pi.PropertyType.IsEnum)
                    {
                        // tenta parse ignorando case
                        var enumVal = Enum.Parse(pi.PropertyType, str, ignoreCase: true);
                        pi.SetValue(target, enumVal);
                    }
                    else if (pi.PropertyType == typeof(string))
                    {
                        pi.SetValue(target, str);
                    }
                    else
                    {
                        // tentar desserializar o valor para o tipo da propriedade
                        var des = JsonSerializer.Deserialize(str, pi.PropertyType);
                        pi.SetValue(target, des);
                    }
                }
                else if (prop.Value.ValueKind == JsonValueKind.Number || prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False || prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                {
                    // desserializa o JSON bruto para o tipo da propriedade
                    var raw = prop.Value.GetRawText();
                    var des = JsonSerializer.Deserialize(raw, pi.PropertyType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    pi.SetValue(target, des);
                }
                else if (prop.Value.ValueKind == JsonValueKind.Null)
                {
                    pi.SetValue(target, null);
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"[ConfigManager] Falha ao setar propriedade '{prop.Name}' em {tType.Name}: {e.Message}");
            }
        }

        return target;
    }
}
