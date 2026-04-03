using FootballPlanner.Application.Session.Commands;
using FootballPlanner.Application.Session.Queries;
using FootballPlanner.Application.SessionActivity.Commands;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace FootballPlanner.Api.Functions;

public class SessionFunctions(IMediator mediator)
{
    [Function("GetSessions")]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions")] HttpRequestData req)
    {
        var sessions = await mediator.Send(new GetAllSessionsQuery());
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(sessions);
        return response;
    }

    [Function("GetSessionById")]
    public async Task<HttpResponseData> GetById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/{id:int}")] HttpRequestData req,
        int id)
    {
        var session = await mediator.Send(new GetSessionByIdQuery(id));
        if (session is null)
            return req.CreateResponse(HttpStatusCode.NotFound);
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(session);
        return response;
    }

    [Function("CreateSession")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sessions")] HttpRequestData req)
    {
        var command = await req.ReadFromJsonAsync<CreateSessionCommand>()
            ?? throw new InvalidOperationException("Invalid request body.");
        var session = await mediator.Send(command);
        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(session);
        return response;
    }

    [Function("UpdateSession")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "sessions/{id:int}")] HttpRequestData req,
        int id)
    {
        var body = await req.ReadFromJsonAsync<UpdateSessionRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        await mediator.Send(new UpdateSessionCommand(id, body.Date, body.Title, body.Notes));
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    [Function("DeleteSession")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "sessions/{id:int}")] HttpRequestData req,
        int id)
    {
        await mediator.Send(new DeleteSessionCommand(id));
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    [Function("AddSessionActivity")]
    public async Task<HttpResponseData> AddActivity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sessions/{id:int}/activities")] HttpRequestData req,
        int id)
    {
        var body = await req.ReadFromJsonAsync<AddSessionActivityRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        var sa = await mediator.Send(new AddSessionActivityCommand(
            id, body.ActivityId, body.PhaseId, body.FocusId, body.Duration, body.Notes));
        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(sa);
        return response;
    }

    [Function("UpdateSessionActivity")]
    public async Task<HttpResponseData> UpdateActivity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "sessions/{id:int}/activities/{sessionActivityId:int}")] HttpRequestData req,
        int id, int sessionActivityId)
    {
        var body = await req.ReadFromJsonAsync<UpdateSessionActivityRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        await mediator.Send(new UpdateSessionActivityCommand(
            sessionActivityId, body.PhaseId, body.FocusId, body.Duration, body.Notes));
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    [Function("RemoveSessionActivity")]
    public async Task<HttpResponseData> RemoveActivity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "sessions/{id:int}/activities/{sessionActivityId:int}")] HttpRequestData req,
        int id, int sessionActivityId)
    {
        await mediator.Send(new RemoveSessionActivityCommand(sessionActivityId));
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    [Function("UpdateSessionActivityKeyPoints")]
    public async Task<HttpResponseData> UpdateKeyPoints(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "sessions/{id:int}/activities/{sessionActivityId:int}/keypoints")] HttpRequestData req,
        int id, int sessionActivityId)
    {
        var body = await req.ReadFromJsonAsync<UpdateKeyPointsRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        await mediator.Send(new UpdateSessionActivityKeyPointsCommand(sessionActivityId, body.KeyPoints));
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    private record UpdateSessionRequest(DateTime Date, string Title, string? Notes);
    private record AddSessionActivityRequest(int ActivityId, int PhaseId, int FocusId, int Duration, string? Notes);
    private record UpdateSessionActivityRequest(int PhaseId, int FocusId, int Duration, string? Notes);
    private record UpdateKeyPointsRequest(List<string> KeyPoints);
}
