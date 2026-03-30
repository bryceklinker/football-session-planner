using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Queries.Phase;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace FootballPlanner.Api.Functions;

public class PhaseFunctions(IMediator mediator)
{
    [Function("GetPhases")]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "phases")] HttpRequestData req)
    {
        var phases = await mediator.Send(new GetAllPhasesQuery());
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(phases);
        return response;
    }

    [Function("CreatePhase")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "phases")] HttpRequestData req)
    {
        var command = await req.ReadFromJsonAsync<CreatePhaseCommand>()
            ?? throw new InvalidOperationException("Invalid request body.");
        var phase = await mediator.Send(command);
        var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
        await response.WriteAsJsonAsync(phase);
        return response;
    }

    [Function("UpdatePhase")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "phases/{id:int}")] HttpRequestData req,
        int id)
    {
        var body = await req.ReadFromJsonAsync<UpdatePhaseRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        await mediator.Send(new UpdatePhaseCommand(id, body.Name, body.Order));
        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    [Function("DeletePhase")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "phases/{id:int}")] HttpRequestData req,
        int id)
    {
        await mediator.Send(new DeletePhaseCommand(id));
        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    private record UpdatePhaseRequest(string Name, int Order);
}
