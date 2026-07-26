using System.Net.Http.Json;
using AgentPlatform.Api.Exceptions;

namespace AgentPlatform.Api.Services;

public interface ISupabaseAdminClient
{
    Task UpdatePasswordAsync(Guid userId, string newPassword, CancellationToken ct = default);
}

/// <summary>
/// Schimbarea parolei trece prin Admin API-ul Supabase: parolele nu ajung
/// niciodata in baza noastra, deci nu le putem modifica direct.
/// </summary>
/// <remarks>
/// Configurarea e citita la apel, nu la construirea HttpClient-ului. Altfel un
/// secret lipsa ar arunca la rezolvarea controllerului si ar dobori si
/// endpointurile care nu au nevoie de Admin API (profil, utilizare).
/// </remarks>
public class SupabaseAdminClient : ISupabaseAdminClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<SupabaseAdminClient> _logger;

    public SupabaseAdminClient(
        HttpClient http,
        IConfiguration config,
        ILogger<SupabaseAdminClient> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task UpdatePasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken ct = default)
    {
        var url = _config["Supabase:Url"]?.TrimEnd('/');
        var secret = _config["Supabase:SecretKey"];

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "Supabase:Url sau Supabase:SecretKey lipsesc. Schimbarea parolei " +
                "are nevoie de secret key pentru Admin API.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{url}/auth/v1/admin/users/{userId}")
        {
            Content = JsonContent.Create(new { password = newPassword }),
        };
        request.Headers.Add("apikey", secret);
        request.Headers.Authorization = new("Bearer", secret);

        var response = await _http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogWarning(
            "Supabase a refuzat schimbarea parolei: {Status} {Body}",
            (int)response.StatusCode,
            body);

        // Traducem doar cazul util; restul devine 500 prin middleware
        if (response.StatusCode is System.Net.HttpStatusCode.UnprocessableEntity
            or System.Net.HttpStatusCode.BadRequest)
        {
            throw new ValidationException(
                "Parola nu a fost acceptată. Folosește minim 6 caractere.");
        }

        throw new InvalidOperationException(
            $"Supabase Admin API a răspuns {(int)response.StatusCode}.");
    }
}
