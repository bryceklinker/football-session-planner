using FootballPlanner.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace FootballPlanner.Api.Functions;

public class HealthFunctions(AppDbContext db)
{
    [Function("Health")]
    public async Task<HttpResponseData> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        var canConnect = await db.Database.CanConnectAsync();
        return req.CreateResponse(canConnect ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);
    }
}
