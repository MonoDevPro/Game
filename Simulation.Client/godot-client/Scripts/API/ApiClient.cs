using System.Text;
using System.Text.Json;
using Godot;

namespace GodotClient.API;

public partial class ApiClient : Node
{
    private HttpRequest _http = null!;

    public override void _Ready()
    {
        _http = new HttpRequest();
        AddChild(_http);
        _http.RequestCompleted += OnRequestCompleted;
    }

    public void FetchConfig()
    {
        var url = "https://api.example.com/config";
        var headers = new string[] { "Accept: application/json" };
        _http.Request(url, headers);
    }

    public void PostData(object payload)
    {
        var url = "https://api.example.com/data";
        var json = JsonSerializer.Serialize(payload);
        var body = Encoding.UTF8.GetBytes(json);
        var headers = new string[] { "Content-Type: application/json", "Authorization: Bearer SUA_TOKEN" };
        _http.Request(url, headers, HttpClient.Method.Post, body);
    }

    private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
    {
        var text = body != null ? Encoding.UTF8.GetString(body) : "";
        if (result != (long)Error.Ok || responseCode < 200 || responseCode >= 300)
        {
            GD.PrintErr($"HTTP erro: result={result} code={responseCode} body={text}");
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            GD.Print("Resposta JSON:", doc.RootElement.ToString());
            // parse para classes:
            // var dto = JsonSerializer.Deserialize<MyDto>(text, options);
        }
        catch (JsonException je)
        {
            GD.PrintErr("Erro ao parsear JSON: " + je.Message);
        }
    }
}