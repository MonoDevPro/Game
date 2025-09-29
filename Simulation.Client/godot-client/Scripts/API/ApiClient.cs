// ApiClientWithNode.cs

using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Models.Models;
using Godot;

namespace GodotClient.API;

public partial class ApiClient : Node
{
    private const string ApiBaseUrl = "http://localhost:5000/api";
    private HttpRequest _http;
    private JsonSerializerOptions _jsonOptions;

    public override void _Ready()
    {
        _http = new HttpRequest();
        AddChild(_http);
        _http.RequestCompleted += OnRequestCompleted;

        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());

        FetchConfig();
    }

    public void FetchConfig()
    {
        _http.Request($"{ApiBaseUrl}/options/client", null, HttpClient.Method.Get);
    }
    
    public void FetchRegister()
    {
        
    }
    
    public void FetchLogin()
    {
    }

    private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[]? body)
    {
        var text = body != null ? System.Text.Encoding.UTF8.GetString(body) : "";
        if (result != (long)Error.Ok || responseCode < 200 || responseCode >= 300)
        {
            GD.PrintErr($"HTTP erro: {result} code={responseCode} body={text}");
            return;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<ConfigDto>(text, _jsonOptions);
            if (dto != null)
            {
                var cfg = GetNode<ConfigManager>("/root/ConfigManager");
                cfg.UpdateFromDto(dto);
                GD.Print("[ApiClientWithNode] Config aplicada.", "World:", dto.World, "Network:", dto.Network, "Authority:", dto.Authority);
            }
        }
        catch (JsonException je)
        {
            GD.PrintErr("Erro parse JSON tipado: " + je.Message);
        }
    }
}