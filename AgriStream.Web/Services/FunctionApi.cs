using System.Net.Http.Json;
namespace AgriStream.Web.Services;
public class FunctionApi(HttpClient http, IConfiguration config) {
    private string Base => (config["Azure:FunctionsBaseUrl"] ?? "").TrimEnd('/');
    private string Key => config["Azure:FunctionsKey"] ?? "";
    public async Task<T?> GetAsync<T>(string path) {
        if (string.IsNullOrWhiteSpace(Base)) throw new InvalidOperationException("Configure Azure:FunctionsBaseUrl.");
        using var req=new HttpRequestMessage(HttpMethod.Get,$"{Base}/api/{path}");
        if (!string.IsNullOrWhiteSpace(Key)) req.Headers.Add("x-functions-key",Key);
        using var response=await http.SendAsync(req); response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }
    public async Task<HttpResponseMessage> PostAsync<T>(string path,T body) {
        if (string.IsNullOrWhiteSpace(Base)) throw new InvalidOperationException("Configure Azure:FunctionsBaseUrl.");
        using var req=new HttpRequestMessage(HttpMethod.Post,$"{Base}/api/{path}") { Content=JsonContent.Create(body) };
        if (!string.IsNullOrWhiteSpace(Key)) req.Headers.Add("x-functions-key",Key);
        return await http.SendAsync(req);
    }
}
