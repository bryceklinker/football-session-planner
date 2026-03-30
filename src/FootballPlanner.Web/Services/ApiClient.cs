using System.Net.Http.Json;

namespace FootballPlanner.Web.Services;

public class ApiClient(HttpClient http)
{
    public Task<List<PhaseDto>?> GetPhasesAsync() =>
        http.GetFromJsonAsync<List<PhaseDto>>("phases");

    public Task<HttpResponseMessage> CreatePhaseAsync(CreatePhaseRequest request) =>
        http.PostAsJsonAsync("phases", request);

    public Task<HttpResponseMessage> UpdatePhaseAsync(int id, UpdatePhaseRequest request) =>
        http.PutAsJsonAsync($"phases/{id}", request);

    public Task<HttpResponseMessage> DeletePhaseAsync(int id) =>
        http.DeleteAsync($"phases/{id}");

    public Task<List<FocusDto>?> GetFocusesAsync() =>
        http.GetFromJsonAsync<List<FocusDto>>("focuses");

    public Task<HttpResponseMessage> CreateFocusAsync(CreateFocusRequest request) =>
        http.PostAsJsonAsync("focuses", request);

    public Task<HttpResponseMessage> UpdateFocusAsync(int id, UpdateFocusRequest request) =>
        http.PutAsJsonAsync($"focuses/{id}", request);

    public Task<HttpResponseMessage> DeleteFocusAsync(int id) =>
        http.DeleteAsync($"focuses/{id}");

    public Task<List<ActivityDto>?> GetActivitiesAsync() =>
        http.GetFromJsonAsync<List<ActivityDto>>("activities");

    public Task<HttpResponseMessage> CreateActivityAsync(CreateActivityRequest request) =>
        http.PostAsJsonAsync("activities", request);

    public Task<HttpResponseMessage> UpdateActivityAsync(int id, UpdateActivityRequest request) =>
        http.PutAsJsonAsync($"activities/{id}", request);

    public Task<HttpResponseMessage> DeleteActivityAsync(int id) =>
        http.DeleteAsync($"activities/{id}");

    public record PhaseDto(int Id, string Name, int Order);
    public record FocusDto(int Id, string Name);
    public record ActivityDto(
        int Id,
        string Name,
        string Description,
        string? InspirationUrl,
        int EstimatedDuration,
        string? DiagramJson,
        DateTime CreatedAt,
        DateTime UpdatedAt);
    public record CreatePhaseRequest(string Name, int Order);
    public record UpdatePhaseRequest(string Name, int Order);
    public record CreateFocusRequest(string Name);
    public record UpdateFocusRequest(string Name);
    public record CreateActivityRequest(
        string Name, string Description, string? InspirationUrl, int EstimatedDuration);
    public record UpdateActivityRequest(
        string Name, string Description, string? InspirationUrl, int EstimatedDuration);
}
