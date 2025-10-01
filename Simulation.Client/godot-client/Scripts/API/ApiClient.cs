using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameWeb.Application.Auth;
using GameWeb.Application.Common.Options;
using Godot;

namespace GodotClient.API;

public partial class ApiClient : Node
{
    // Torne configurável (ProjectSettings, [Export] ou via setter)
    [Export] public string ApiBaseUrl { get; set; } = "http://localhost:5000/api";

    private HttpRequest _http = default!;
    private JsonSerializerOptions _jsonOptions = null!;

    private enum PendingRequest { None, FetchConfig, Register, Login }
    private PendingRequest _lastRequest = PendingRequest.None;

    // Token JWT armazenado depois do login/register (para usar em calls autenticadas)
    private string? _jwtToken;

    public override void _Ready()
    {
        _http = new HttpRequest
        {
            // Melhora a responsividade e evita travas por DNS/IO
            UseThreads = true,
            Timeout = 15, // segundos (ajuste conforme necessidade)
        };

        AddChild(_http);
        _http.RequestCompleted += OnRequestCompleted;

        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public override void _ExitTree()
    {
        _http.RequestCompleted -= OnRequestCompleted;
    }

    // Headers padrão + Authorization quando houver JWT.
    private string[] BuildHeaders(bool needsAuth = false)
    {
        var headers = new List<string>
        {
            "Accept: application/json",
            "Content-Type: application/json"
        };

        if (needsAuth && !string.IsNullOrEmpty(_jwtToken))
            headers.Add($"Authorization: Bearer {_jwtToken}");

        return headers.ToArray();
    }

    private bool IsBusy()
    {
        // Simples guarda. Opcionalmente, pode checar status do HttpRequest.
        return _lastRequest != PendingRequest.None;
    }

    public void FetchConfig()
    {
        if (IsBusy())
        {
            GD.PrintErr("[ApiClient] Ainda processando outra requisição.");
            return;
        }

        _lastRequest = PendingRequest.FetchConfig;
        var err = _http.Request($"{ApiBaseUrl}/options/client", BuildHeaders(), HttpClient.Method.Get);
        if (err != Error.Ok)
        {
            GD.PrintErr($"[ApiClient] Falha ao iniciar FetchConfig: {err}");
            _lastRequest = PendingRequest.None;
        }
    }

    public void Register(RegisterRequest payload)
    {
        if (IsBusy())
        {
            GD.PrintErr("[ApiClient] Ainda processando outra requisição.");
            return;
        }

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(json);

        _lastRequest = PendingRequest.Register;
        var err = _http.RequestRaw($"{ApiBaseUrl}/auth/register", BuildHeaders(), HttpClient.Method.Post, body);
        if (err != Error.Ok)
        {
            GD.PrintErr($"[ApiClient] Falha ao iniciar Register: {err}");
            _lastRequest = PendingRequest.None;
        }
    }

    public void Login(LoginRequest payload)
    {
        if (IsBusy())
        {
            GD.PrintErr("[ApiClient] Ainda processando outra requisição.");
            return;
        }

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var body = Encoding.UTF8.GetBytes(json);

        _lastRequest = PendingRequest.Login;
        var err = _http.RequestRaw($"{ApiBaseUrl}/auth/login", BuildHeaders(), HttpClient.Method.Post, body);
        if (err != Error.Ok)
        {
            GD.PrintErr($"[ApiClient] Falha ao iniciar Login: {err}");
            _lastRequest = PendingRequest.None;
        }
    }

    private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[]? body)
    {
        try
        {
            var text = body != null && body.Length > 0 ? Encoding.UTF8.GetString(body) : string.Empty;

            if (result != (long)Error.Ok || responseCode < 200 || responseCode >= 300)
            {
                GD.PrintErr($"HTTP erro: result={result} code={responseCode} body={text}");
                TryLogValidationErrors(text);
                return;
            }

            // Evita parse em 204/empty
            bool hasJsonBody = !string.IsNullOrWhiteSpace(text) && responseCode != 204;

            switch (_lastRequest)
            {
                case PendingRequest.FetchConfig:
                    if (hasJsonBody)
                    {
                        var cfg = JsonSerializer.Deserialize<OptionsDto>(text, _jsonOptions);
                        if (cfg != null)
                        {
                            // Melhor: emitir um sinal/evento ao invés de GetNode fixo
                            GetNode<ConfigManager>("/root/ConfigManager").UpdateFromDto(cfg);
                            GD.Print("[ApiClient] Config aplicada.");
                        }
                        else
                        {
                            GD.PrintErr("[ApiClient] ConfigDto nulo.");
                        }
                    }
                    break;

                case PendingRequest.Register:
                {
                    if (hasJsonBody)
                    {
                        var regResp = JsonSerializer.Deserialize<AuthResponse>(text, _jsonOptions);
                        if (regResp != null)
                        {
                            _jwtToken = regResp.AccessToken;
                            GD.Print($"[ApiClient] Registro OK. userId={regResp.UserId}");
                        }
                        else
                        {
                            GD.PrintErr("[ApiClient] AuthResponse (register) nulo.");
                        }
                    }
                    break;
                }

                case PendingRequest.Login:
                {
                    if (hasJsonBody)
                    {
                        var loginResp = JsonSerializer.Deserialize<AuthResponse>(text, _jsonOptions);
                        if (loginResp != null)
                        {
                            _jwtToken = loginResp.AccessToken;
                            GD.Print($"[ApiClient] Login OK. token length={_jwtToken?.Length ?? 0}");
                        }
                        else
                        {
                            GD.PrintErr("[ApiClient] AuthResponse (login) nulo.");
                        }
                    }
                    break;
                }

                default:
                    GD.Print("[ApiClient] Resposta recebida:", text);
                    break;
            }
        }
        catch (JsonException je)
        {
            GD.PrintErr("Erro parse JSON tipado: " + je.Message);
        }
        finally
        {
            _lastRequest = PendingRequest.None;
        }
    }

    private void TryLogValidationErrors(string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText)) return;

        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            // shape { Errors: [ { Field, Message } ] }
            if (root.TryGetProperty("Errors", out var errorsElem) && errorsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in errorsElem.EnumerateArray())
                {
                    var field = item.TryGetProperty("Field", out var f) ? f.GetString() : "(unknown)";
                    var msg = item.TryGetProperty("Message", out var m) ? m.GetString() : item.ToString();
                    GD.PrintErr($"Validation error: {field} => {msg}");
                }
                return;
            }

            // shape { errors: { "UserName": ["msg1","msg2"] } } (ASP.NET Core)
            if (root.TryGetProperty("errors", out var errs2) && errs2.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in errs2.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var m in prop.Value.EnumerateArray())
                            GD.PrintErr($"Validation error: {prop.Name} => {m.GetString()}");
                    }
                }
                return;
            }

            // RFC 7807 ProblemDetails
            if (root.TryGetProperty("title", out var title))
                GD.PrintErr("ProblemDetails.title: " + title.GetString());

            if (root.TryGetProperty("detail", out var detail))
                GD.PrintErr("ProblemDetails.detail: " + detail.GetString());

            if (root.TryGetProperty("type", out var type))
                GD.PrintErr("ProblemDetails.type: " + type.GetString());
        }
        catch (Exception)
        {
            GD.PrintErr("Validation (raw): " + jsonText);
        }
    }
}