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

    public Task<ActivityDto?> CreateActivityAsync(CreateActivityRequest request) =>
        http.PostAsJsonAsync("activities", request).ContinueWith(t => t.Result.Content.ReadFromJsonAsync<ActivityDto>()).Unwrap();

    public Task<HttpResponseMessage> UpdateActivityAsync(int id, UpdateActivityRequest request) =>
        http.PutAsJsonAsync($"activities/{id}", request);

    public Task<HttpResponseMessage> DeleteActivityAsync(int id) =>
        http.DeleteAsync($"activities/{id}");

    public Task<List<SessionDto>?> GetSessionsAsync() =>
        http.GetFromJsonAsync<List<SessionDto>>("sessions");

    public Task<SessionDto?> GetSessionAsync(int id) =>
        http.GetFromJsonAsync<SessionDto>($"sessions/{id}");

    public Task<HttpResponseMessage> CreateSessionAsync(CreateSessionRequest request) =>
        http.PostAsJsonAsync("sessions", request);

    public Task<HttpResponseMessage> UpdateSessionAsync(int id, UpdateSessionRequest request) =>
        http.PutAsJsonAsync($"sessions/{id}", request);

    public Task<HttpResponseMessage> DeleteSessionAsync(int id) =>
        http.DeleteAsync($"sessions/{id}");

    public Task<HttpResponseMessage> AddSessionActivityAsync(int sessionId, AddSessionActivityRequest request) =>
        http.PostAsJsonAsync($"sessions/{sessionId}/activities", request);

    public Task<HttpResponseMessage> UpdateSessionActivityAsync(int sessionId, int id, UpdateSessionActivityRequest request) =>
        http.PutAsJsonAsync($"sessions/{sessionId}/activities/{id}", request);

    public Task<HttpResponseMessage> RemoveSessionActivityAsync(int sessionId, int id) =>
        http.DeleteAsync($"sessions/{sessionId}/activities/{id}");

    public Task<HttpResponseMessage> UpdateSessionActivityKeyPointsAsync(int sessionId, int id, List<string> keyPoints) =>
        http.PutAsJsonAsync($"sessions/{sessionId}/activities/{id}/keypoints", new UpdateSessionActivityKeyPointsRequest(keyPoints));

    public record PhaseDto(int Id, string Name, int Order);
    public record FocusDto(int Id, string Name);
    public record ActivityDto(
        int Id, string Name, string Description, string? InspirationUrl,
        int EstimatedDuration, string? DiagramJson, DateTime CreatedAt, DateTime UpdatedAt);
    public record SessionDto(
        int Id, string Title, DateTime Date, string? Notes,
        DateTime CreatedAt, DateTime UpdatedAt,
        List<SessionActivityDto> Activities);
    public record SessionActivityDto(
        int Id, int SessionId, int ActivityId, ActivityDto? Activity,
        int PhaseId, PhaseDto? Phase, int FocusId, FocusDto? Focus,
        int Duration, int DisplayOrder, string? Notes,
        List<SessionActivityKeyPointDto> KeyPoints);
    public record SessionActivityKeyPointDto(int Id, int Order, string Text);
    public record CreatePhaseRequest(string Name, int Order);
    public record UpdatePhaseRequest(string Name, int Order);
    public record CreateFocusRequest(string Name);
    public record UpdateFocusRequest(string Name);
    public record CreateActivityRequest(string Name, string Description, string? InspirationUrl, int EstimatedDuration);
    public record UpdateActivityRequest(string Name, string Description, string? InspirationUrl, int EstimatedDuration);
    public record CreateSessionRequest(DateTime Date, string Title, string? Notes);
    public record UpdateSessionRequest(DateTime Date, string Title, string? Notes);
    public record AddSessionActivityRequest(int ActivityId, int PhaseId, int FocusId, int Duration, string? Notes);
    public record UpdateSessionActivityRequest(int PhaseId, int FocusId, int Duration, string? Notes);
    public record UpdateSessionActivityKeyPointsRequest(List<string> KeyPoints);
}
