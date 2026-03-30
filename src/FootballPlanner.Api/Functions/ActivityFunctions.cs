using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Application.Queries.Activity;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace FootballPlanner.Api.Functions;

public class ActivityFunctions(IMediator mediator)
{
    [Function("GetActivities")]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "activities")] HttpRequestData req)
    {
        var activities = await mediator.Send(new GetAllActivitiesQuery());
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(activities);
        return response;
    }

    [Function("CreateActivity")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "activities")] HttpRequestData req)
    {
        var command = await req.ReadFromJsonAsync<CreateActivityCommand>()
            ?? throw new InvalidOperationException("Invalid request body.");
        var activity = await mediator.Send(command);
        var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
        await response.WriteAsJsonAsync(activity);
        return response;
    }

    [Function("UpdateActivity")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "activities/{id:int}")] HttpRequestData req,
        int id)
    {
        var body = await req.ReadFromJsonAsync<UpdateActivityRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        await mediator.Send(new UpdateActivityCommand(
            id, body.Name, body.Description, body.InspirationUrl, body.EstimatedDuration));
        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    [Function("DeleteActivity")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "activities/{id:int}")] HttpRequestData req,
        int id)
    {
        await mediator.Send(new DeleteActivityCommand(id));
        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    private record UpdateActivityRequest(
        string Name, string Description, string? InspirationUrl, int EstimatedDuration);
}
