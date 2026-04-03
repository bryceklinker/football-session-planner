using FootballPlanner.Application.Focus.Commands;
using FootballPlanner.Application.Focus.Queries;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace FootballPlanner.Api.Functions;

public class FocusFunctions(IMediator mediator)
{
    [Function("GetFocuses")]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "focuses")] HttpRequestData req)
    {
        var focuses = await mediator.Send(new GetAllFocusesQuery());
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(focuses);
        return response;
    }

    [Function("CreateFocus")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "focuses")] HttpRequestData req)
    {
        var command = await req.ReadFromJsonAsync<CreateFocusCommand>()
            ?? throw new InvalidOperationException("Invalid request body.");
        var focus = await mediator.Send(command);
        var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
        await response.WriteAsJsonAsync(focus);
        return response;
    }

    [Function("UpdateFocus")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "focuses/{id:int}")] HttpRequestData req,
        int id)
    {
        var body = await req.ReadFromJsonAsync<UpdateFocusRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        await mediator.Send(new UpdateFocusCommand(id, body.Name));
        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    [Function("DeleteFocus")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "focuses/{id:int}")] HttpRequestData req,
        int id)
    {
        await mediator.Send(new DeleteFocusCommand(id));
        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    private record UpdateFocusRequest(string Name);
}
