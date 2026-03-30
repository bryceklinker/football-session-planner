using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Sql;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;

namespace FootballPlanner.Infra;

public class FootballPlannerStack : Stack
{
    public FootballPlannerStack()
    {
        var config = new Config();
        var environment = config.Require("environment");
        var naming = new Naming(environment);

        // Location is read from AZURE_LOCATION env var (set by CI/CD workflows).
        // centralus is the default; override the env var to deploy to a different region.
        // This is the single place the default is defined — do not add it elsewhere.
        var location = System.Environment.GetEnvironmentVariable("AZURE_LOCATION") ?? "centralus";

        var resourceGroup = new ResourceGroup(naming.Resource("football-planner-rg"), new ResourceGroupArgs
        {
            Location = location,
            ResourceGroupName = naming.Resource("football-planner-rg"),
        });

        var sqlServer = new Server(naming.Resource("football-planner-sql"), new ServerArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            ServerName = naming.Resource("football-planner-sql"),
            AdministratorLogin = config.RequireSecret("sqlAdminLogin"),
            AdministratorLoginPassword = config.RequireSecret("sqlAdminPassword"),
        });

        var sqlDatabase = new Database(naming.Resource("football-planner-db"), new DatabaseArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            ServerName = sqlServer.Name,
            DatabaseName = naming.Resource("football-planner-db"),
            Sku = new Pulumi.AzureNative.Sql.Inputs.SkuArgs { Name = "GP_S_Gen5_1", Tier = "GeneralPurpose" },
            AutoPauseDelay = 60,
            MinCapacity = 0.5,
        });

        var staticWebApp = new StaticSite(naming.Resource("football-planner-swa"), new StaticSiteArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            Name = naming.Resource("football-planner-swa"),
            Sku = new SkuDescriptionArgs { Name = "Standard", Tier = "Standard" },
        });

        // Retrieve the SWA deployment token from Azure so the deploy workflow
        // can read it as a stack output — no manual secret configuration needed.
        var swaSecrets = Pulumi.AzureNative.Web.ListStaticSiteSecrets.Invoke(
            new Pulumi.AzureNative.Web.ListStaticSiteSecretsInvokeArgs
            {
                ResourceGroupName = resourceGroup.Name,
                Name = staticWebApp.Name,
            });

        ResourceGroupName = resourceGroup.Name;
        StaticWebAppUrl = staticWebApp.DefaultHostname;
        SqlServerName = sqlServer.Name;
        DatabaseName = sqlDatabase.Name;
        StaticWebAppDeployToken = swaSecrets.Apply(s => s.Properties["apiKey"]);
    }

    [Output] public Output<string> ResourceGroupName { get; set; }
    [Output] public Output<string> StaticWebAppUrl { get; set; }
    [Output] public Output<string> SqlServerName { get; set; }
    [Output] public Output<string> DatabaseName { get; set; }
    [Output] public Output<string> StaticWebAppDeployToken { get; set; }
}
