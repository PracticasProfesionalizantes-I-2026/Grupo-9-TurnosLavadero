using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

// Herramienta de demostración escrita en C#. No modifica la base habitual del lavadero.
var root = new DirectoryInfo(AppContext.BaseDirectory);
while (root is not null && !File.Exists(Path.Combine(root.FullName, "TurnosLavadero.slnx")))
    root = root.Parent;
if (root is null) throw new InvalidOperationException("No se encuentra la raíz del repositorio.");
string Option(string name, string fallback)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
}
var output = Path.GetFullPath(Option("--output", "TestResults/demo"));
Directory.CreateDirectory(output);
var dotnet = Option("--dotnet", Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet");
var configuration = Option("--configuration", "Debug");
var apiDll = Path.Combine(root.FullName, "API", "bin", configuration, "net10.0", "API.dll");
if (!File.Exists(apiDll)) throw new InvalidOperationException("Compilar primero: dotnet build TurnosLavadero.slnx");
var temporary = Path.Combine(Path.GetTempPath(), $"lavadero-demo-{Guid.NewGuid():N}");
Directory.CreateDirectory(temporary);
await using var smtp = new LocalSmtpCapture();
var adminPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
var clientPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
var otherPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
var portProbe = new TcpListener(IPAddress.Loopback, 0);
portProbe.Start();
var port = ((IPEndPoint)portProbe.LocalEndpoint).Port;
portProbe.Stop();
using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(30) };
using var log = new StreamWriter(Path.Combine(output, "api.log"), false, Encoding.UTF8) { AutoFlush = true };
var logLock = new object();
var start = new ProcessStartInfo(dotnet) { WorkingDirectory = Path.Combine(root.FullName, "API"),
    RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
start.ArgumentList.Add(apiDll);
foreach (var (key, value) in new Dictionary<string, string> {
    ["ASPNETCORE_ENVIRONMENT"] = "Development", ["ASPNETCORE_URLS"] = http.BaseAddress.ToString(),
    ["ConnectionStrings__DefaultConnection"] = $"Data Source={Path.Combine(temporary, "demo.db")};Pooling=False",
    ["TURNOSLAVADERO_SEED_ADMIN_EMAIL"] = "admin@lavadero.local",
    ["TURNOSLAVADERO_SEED_ADMIN_PASSWORD"] = adminPassword,
    ["Recordatorios__Habilitados"] = "false", ["Smtp__Host"] = "127.0.0.1",
    ["Smtp__Port"] = smtp.Port.ToString(), ["Smtp__EnableSsl"] = "false",
    ["Smtp__From"] = "demo@example.test", ["Smtp__User"] = "", ["Smtp__Password"] = "" })
    start.Environment[key] = value;
using var api = new Process { StartInfo = start };
void CaptureLog(object sender, DataReceivedEventArgs e) { if (e.Data is not null) lock (logLock) log.WriteLine(e.Data); }
api.OutputDataReceived += CaptureLog;
api.ErrorDataReceived += CaptureLog;
var steps = new List<object>();
var apiStarted = false;
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
try
{
    apiStarted = api.Start();
    api.BeginOutputReadLine();
    api.BeginErrorReadLine();
    var ready = false;
    for (var attempt = 0; attempt < 180; attempt++)
    {
        if (api.HasExited) throw new InvalidOperationException("La API terminó al iniciar. Consultar api.log.");
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using var response = await http.GetAsync("/openapi/v1.json", timeout.Token);
            if (response.IsSuccessStatusCode) { ready = true; break; }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { }
        await Task.Delay(500);
    }
    if (!ready) throw new TimeoutException("La API no inició en 90 segundos.");
    var admin = (await Request("CU-08 Inicio de sesión administrador", "POST", "/api/auth/login", 200,
        new { email = "admin@lavadero.local", password = adminPassword }))!["token"]!.GetValue<string>();
    var service = (await Request("CU-06 Alta de servicio", "POST", "/api/servicios", 201,
        new { nombre = "Lavado demostración", importe = 12000 }, admin))!["id"]!.GetValue<string>();
    var account = await Request("CU-07 Registro de cliente", "POST", "/api/auth/registro", 201,
        new { nombre = "Cliente", apellido = "Demostración", email = "cliente.demo@example.test", password = clientPassword });
    var token = account!["token"]!.GetValue<string>();
    await Request("RNF-01 Acceso sin token", "GET", "/api/servicios", 401);
    await Request("CU-13 Consulta de servicios", "GET", "/api/servicios", 200, token: token);
    var date = DateTimeOffset.UtcNow.AddDays(7);
    var create = new { servicioId = service, fechaHora = date };
    var turno = (await Request("CU-01 Reserva del cliente autenticado", "POST", "/api/turnos", 201, create, token))!["id"]!.GetValue<string>();
    await Request("RN-01 Horario ocupado", "POST", "/api/turnos", 409, create, token);
    var other = (await Request("Registro de segundo cliente", "POST", "/api/auth/registro", 201,
        new { nombre = "Otro", apellido = "Cliente", email = "otro.demo@example.test", password = otherPassword }))!["token"]!.GetValue<string>();
    var update = new { servicioId = service, fechaHora = date.AddHours(1) };
    await Request("RNF-02 Rechazo de modificación ajena", "PUT", $"/api/turnos/{turno}", 403, update, other);
    await Request("CU-04 Modificación por el titular", "PUT", $"/api/turnos/{turno}", 200, update, token);
    var localDay = update.fechaHora.ToOffset(TimeSpan.FromHours(-3)).ToString("yyyy-MM-dd");
    var agenda = await Request("CU-11 Agenda del día argentino", "GET", $"/api/turnos/agenda?fecha={localDay}", 200, token: admin);
    Require(agenda!.AsArray().Any(x => x!["id"]!.GetValue<string>() == turno), "El turno debe figurar en la agenda.");
    var period = $"desde={Uri.EscapeDataString(date.ToString("O"))}&hasta={Uri.EscapeDataString(date.AddHours(2).ToString("O"))}";
    var reminder = await Request("CU-02 Recordatorio por SMTP local", "POST", "/api/recordatorios/procesar?" + period, 200, token: admin);
    Require(reminder!["enviados"]!.GetValue<int>() == 1 && smtp.Messages.Count == 1, "Debe capturarse un correo.");
    var repeat = await Request("CU-02 Segundo procesamiento sin reenvío", "POST", "/api/recordatorios/procesar?" + period, 200, token: admin);
    Require(repeat!["procesados"]!.GetValue<int>() == 0 && smtp.Messages.Count == 1, "No debe reenviarse el correo.");
    await Request("CU-05 Cancelación del turno", "DELETE", $"/api/turnos/{turno}", 204, token: token);
    var available = await Request("CU-09 Horario liberado", "GET",
        "/api/turnos/disponibilidad?fechaHora=" + Uri.EscapeDataString(update.fechaHora.ToString("O")), 200, token: token);
    Require(available!["disponible"]!.GetValue<bool>(), "El horario debe quedar libre.");
    var history = await Request("CU-10 Historial de turnos propios", "GET", "/api/turnos/mis-turnos", 200, token: token);
    Require(history!.AsArray().Any(x => x!["id"]!.GetValue<string>() == turno && x["estado"]!.GetValue<string>() == "Cancelado"), "Debe conservarse el turno cancelado.");
    await Request("Validación de datos incompletos", "POST", "/api/turnos", 400, new { }, token);
    var summary = new { fechaUtc = DateTimeOffset.UtcNow, implementacion = "C# / .NET 10", pasos = steps,
        correosCapturadosLocalmente = smtp.Messages.Count, entregaExterna = false };
    await File.WriteAllTextAsync(Path.Combine(output, "resultados.json"), JsonSerializer.Serialize(summary, jsonOptions));
    var sections = new StringBuilder();
    foreach (var item in steps)
    {
        var step = JsonSerializer.SerializeToNode(item)!;
        sections.Append($"<section><h2>{H(step["paso"]!.ToString())}</h2><p><b>{step["obtenido"]}</b> {H(step["metodo"]!.ToString())} {H(step["ruta"]!.ToString())}</p><pre>{H(step["respuesta"]?.ToJsonString(jsonOptions) ?? "Sin cuerpo")}</pre></section>");
    }
    var html = """
        <!doctype html><html lang="es"><meta charset="utf-8"><title>Demo C# verificada del lavadero</title>
        <style>body{font:17px system-ui;margin:40px auto;max-width:1100px;padding:0 24px;color:#172b3a;background:#f4f7fa}
        h1{font-size:36px}section{background:white;padding:20px;margin:20px 0;border-left:5px solid #008c80}
        pre{white-space:pre-wrap;overflow-wrap:anywhere;font-size:14px}b{color:#007365}</style>
        <h1>Sistema de turnos para lavadero</h1><p>Grupo 9 · Juan Jesús Baigorria y Tomás Martín Ponce</p>
        """ + $"<p>Demo escrita en C# / .NET 10. Ejecución: {summary.fechaUtc:O}. {steps.Count} peticiones verificadas con JWT real y SQLite temporal.</p>" +
        "<p>El correo se capturó en SMTP local. Esta evidencia no acredita entrega a una casilla externa.</p>" + sections +
        $"<section><h2>Correo capturado</h2><pre>{H(smtp.Messages.Single())}</pre></section></html>";
    await File.WriteAllTextAsync(Path.Combine(output, "demo.html"), html);
    Console.WriteLine($"Demo C# completada: {steps.Count} respuestas verificadas y {smtp.Messages.Count} correo local. {Path.Combine(output, "demo.html")}");
}
finally
{
    if (apiStarted && !api.HasExited) { api.Kill(entireProcessTree: true); await api.WaitForExitAsync(); }
    try { Directory.Delete(temporary, recursive: true); }
    catch (IOException) { Console.Error.WriteLine("No se pudo eliminar la base temporal: " + temporary); }
}

async Task<JsonNode?> Request(string label, string method, string route, int expected, object? data = null, string? token = null)
{
    using var request = new HttpRequestMessage(new HttpMethod(method), route);
    if (data is not null) request.Content = JsonContent.Create(data);
    if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    using var response = await http.SendAsync(request);
    var raw = await response.Content.ReadAsStringAsync();
    var body = string.IsNullOrWhiteSpace(raw) ? null : JsonNode.Parse(raw);
    steps.Add(new { paso = label, metodo = method, ruta = route, esperado = expected,
        obtenido = (int)response.StatusCode, correcto = (int)response.StatusCode == expected,
        peticion = Redact(JsonSerializer.SerializeToNode(data)), respuesta = Redact(body?.DeepClone()) });
    Console.WriteLine($"{((int)response.StatusCode == expected ? "OK" : "ERROR")} {(int)response.StatusCode} {label}");
    Require((int)response.StatusCode == expected, $"{label}: esperaba {expected}, recibió {(int)response.StatusCode}.");
    return body;
}
static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
static string H(string value) => WebUtility.HtmlEncode(value);
static JsonNode? Redact(JsonNode? node)
{
    if (node is JsonObject obj)
        foreach (var key in obj.Select(x => x.Key).ToArray())
            obj[key] = key.ToLowerInvariant() is "token" or "password" or "passwordhash" ? JsonValue.Create("[omitido]") : Redact(obj[key]?.DeepClone());
    else if (node is JsonArray arr)
        for (var i = 0; i < arr.Count; i++) arr[i] = Redact(arr[i]?.DeepClone());
    return node;
}

// Receptor SMTP limitado a loopback para demostrar el transporte sin enviar a personas.
sealed class LocalSmtpCapture : IAsyncDisposable
{
    private readonly TcpListener listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task loop;
    public List<string> Messages { get; } = [];
    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
    public LocalSmtpCapture() { listener.Start(); loop = AcceptAsync(); }
    private async Task AcceptAsync()
    {
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                using var client = await listener.AcceptTcpClientAsync(cancellation.Token);
                await HandleAsync(client, cancellation.Token);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
    }
    private async Task HandleAsync(TcpClient client, CancellationToken token)
    {
        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        await using var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
        await writer.WriteLineAsync("220 localhost demo SMTP");
        while (await reader.ReadLineAsync(token) is { } line)
        {
            if (line.Equals("QUIT", StringComparison.OrdinalIgnoreCase)) { await writer.WriteLineAsync("221 Bye"); return; }
            if (line.Equals("DATA", StringComparison.OrdinalIgnoreCase))
            {
                await writer.WriteLineAsync("354 End with dot");
                var content = new StringBuilder();
                while (await reader.ReadLineAsync(token) is { } part && part != ".")
                    content.AppendLine(part.StartsWith("..", StringComparison.Ordinal) ? part[1..] : part);
                Messages.Add(content.ToString());
            }
            await writer.WriteLineAsync("250 OK");
        }
    }
    public async ValueTask DisposeAsync()
    {
        cancellation.Cancel();
        listener.Stop();
        await loop;
        cancellation.Dispose();
    }
}
