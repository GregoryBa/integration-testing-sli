public class ServiceApiFactory
{
    // step 3 : Create a WebApplicationFactory
    public static WebApplicationFactory<T> WebApplicationFactory<T>(IServiceCollection testServices,
    bool enableTestAuthPolicy) where T : class =>
         new WebApplicationFactory<T>()
             .WithWebHostBuilder(builder =>
             {
                 // Override appsettings.Development.json
                 builder.UseEnvironment("IntegrationTesting");
                 builder.ConfigureAppConfiguration((context, config) =>
                 {
                     config.AddJsonFile
                 ($"appsettings.IntegrationTests.json").AddEnvironmentVariables();
                 });

                 builder.ConfigureTestServices(services =>
                 {
                     // Remove services
                     services.Remove(services.SingleOrDefault(descriptor => descriptor.ServiceType ==
                     typeof(DbContextOptions<PostgresDbContext>)));
                     services.Remove(services.SingleOrDefault(descriptor => descriptor.ServiceType ==
                     typeof(NpgSqlHealthCheck)));
                     services.Remove(services.SingleOrDefault(descriptor => descriptor.ServiceType ==
                     typeof(SqlServerHealthCheck)));
                     services.Remove(services.SingleOrDefault(descriptor => descriptor.ServiceType ==
                     typeof(AuthenticationService)));

                     if (enableTestAuthPolicy)
                     {
                         services.AddSingleton<IPolicyEvaluator, TestingPolicyEvaluator>();
                     }

                     // Add instance of in memory db
                     services.AddDbContext<PostgresDbContext>((sp, options) =>
                     {
                         options.UseInMemoryDatabase("InMemoryDbForTesting");
                     });
                     foreach (var service in testServices)
                     {
                         services.Add(service);
                     }
                 });
             });
}