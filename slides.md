---
theme: seriph
background: https://cover.sli.dev
title: Integration testing
info: |
  ## Overview and best practices for integration testing
class: text-center
highlighter: shiki
drawings:
  persist: false
transition: slide-left
mdc: true
---
# Integration testing

Overview of best practices for integration testing.

<!--
Today I'll be talking about testing and specifically integration testing. I'll go into a bit of theory at the beginning of the presentation and then jump straight into practical examples, since those are the most fun.

You might be lucky and when you're placed in a on-going project as an consultant or a new employee, where there might be already a set of guidelines on how you do testing. If that's you that's great, I'm happy for you. 

I was never as lucky. In every on going project I've joined has a mixture of NUnit- XUnit libraries, Unit tests, a very weird attempts on integration testing that don't test anything else other than which status code the controller is returning, unit tests that depend on each other and sprinkled in some k6 load testing places that didn't make any sense because "just for fun". And usually none of the tests actually tested if the result of the endpoint if correct.

Combination of all of those factors led me to just make changes in the code and avoid making any changes to those tests, since the testing suite was more fragile than the code itself. 
-->

---
transition: fade-out
layout: two-cols
---
# Unit test

  - <span v-mark.circle.orange="2">Fast</span>
  - <span v-mark.strike-through.orange="4">Isolated</span>
  - <span v-mark.circle.orange="1">Repeatable</span>
  - <span v-mark.circle.orange="4">Self-validating</span>
  - <span v-mark.strike-through.orange="4">"Solitary"</span>

::right::

# Integration test 

- <span v-mark.strike-through.orange="4">Slow</span>
- External dependencies
- <span v-mark.strike-through.orange="4">Less deterministic</span>
- <span v-mark.strike-through.orange="4">Flaky</span>
- <span v-mark.circle.orange="3">"Sociable"</span>

<!-- There are 2 camps with 2 different definitions of integration testing.
- First camp only calls tests integration tests when NO mocks are used are you're actually calling the real service or real database. 
- Second camp has a bit more flexible definition of integration testing, this is also what Microsoft's documentation tends to agree with, is that integration testing tests the whole picture with mocking kept to minimum. You generally don't use real database for this purpose, you might spin it up in memory or as a Docker container, but you rarely call external services.

When people talk about integration testing vs unit testing they often compare those two as if they're black and white. I've seen this comparison over and over again, and I don't agree with it.
People tend to describe unit tests as 
- Fast
- Isolated
- Repeatable
- Self-validating
- Timely
- "Solitary"

And integration tests as:
- Invokes multiple parts of the system together 
- External dependencies - flaky if you're calling third party system
- Less deterministic. Only goes towards real database / cache and uses real users and authorization.
- "Sociable"

I believe that those definitions can be combined together. 

 -->


---
transition: fade-out
---
# Testing methodology

<div style="margin-bottom: 20px;" v-click>
Only care about the <span v-mark.underscore.orange="1">output</span> of the system
</div>

<div style="margin-bottom: 20px;" v-click>
Most of the time mock out dependent systems - reduce flakiness 
</div>

<div style="margin-bottom: 20px;" v-click>
Create a Faker instead with exact expected result from external dependency
</div>

<div style="margin-bottom: 20px;" v-click>
Test DI and all the parts of the internal system
</div>

<div style="margin-bottom: 20px;" v-click>
Tests must be short and concise
</div>

<div style="margin-bottom: 20px;" v-click>
Grand goal of testing: being able to trust tests in a way where any changes made to the codebase can be pushed<span v-mark.underscore.orange="6"> without or minimal manual testing. </span>
</div>

---
transition: fade-out
---
# Acceptance criteria
<div style="margin-bottom: 20px;">
GIVEN the user is owner of the account or an admin
</div>
<div style="margin-bottom: 20px;">
WHEN user tries to get user information
</div>
<div style="margin-bottom: 20px;">
THEN return Ok 
</div>
<div style="margin-bottom: 20px;">
AND users name, last name, social security number and address. 
</div>

---
transition: slide-left
---
# Where do we start

```csharp
public class GetUserInformation()
{
  [Fact]
    public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner() {
      // Arrange
      var userNumber = "123456789";

      // Act
      var response = await client.GetAsync($"user-information/{userNumber}");
      var content = await response.Content.ReadAsStringAsync();
      var contactInformationResponse = JsonSerializer.Deserialize<UserInformationDto>
      (content, new JsonSerializerOptions
      {
          PropertyNameCaseInsensitive = true,
      });

      // Assert
    }
}
```
---
transition: slide-left
---
# Step 1 : Abstract away
```csharp
public static class Json
{
    public static T? Deserialize<T>(string json, JsonSerializerOptions? options = null) where T : class
    {
        options ??= new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
        };

        return JsonSerializer.Deserialize<T>(json, options);
    }
}
```
---
transition: slide-left
---
# Step 2 : Apply
````md magic-move {lines: true}
```csharp {*}
public class GetUserInformation()
{
  [Fact]
  public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner() {
    // Arrange
    var userNumber = "123456789";

    // Act
    var response = await client.GetAsync($"user-information/{userNumber}");
    var content = await response.Content.ReadAsStringAsync();
    var contactInformationResponse = JsonSerializer.Deserialize<UserInformationDto>
    (content, new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true,
    });

    // Assert
  }
}
```
```csharp
public class GetUserInformation()
{
  [Fact]
  public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner() {
    // Arrange
    var userNumber = "123456789";

    // Act
    var response = await client.GetAsync($"user-information/{userNumber}");
    var content = await response.Content.ReadAsStringAsync();
    var userInformationResponse = Json.Deserialize<UserInformationDto>(content);

    // Assert
  }
}
```
````
---
transition: slide-left
---
# Step 3 : Create a WebApplicationFactory

````md magic-move {lines: true}
```csharp {*}
{
  public class ApiFactory
  {
   public static WebApplicationFactory<T> WebApplicationFactory<T>(IServiceCollection testServices, 
   bool enableTestAuthPolicy) where T : class =>
        new WebApplicationFactory<T>()
            .WithWebHostBuilder(builder =>
            {
                // Override appsettings.Development.json
                builder.UseEnvironment("IntegrationTesting");
                builder.ConfigureAppConfiguration((context, config) => { config.AddJsonFile
                ($"appsettings.IntegrationTests.json").AddEnvironmentVariables(); });

                builder.ConfigureTestServices(services =>
                {
                    // Remove services
                    services.Remove(services.SingleOrDefault(descriptor => descriptor.ServiceType == 
                    typeof(DbContext<PostgresDbContext>)));
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
                    // Or even better use Testcontainers library
                    // to spin up docker container for more real 
                    // database instance
                    foreach (var service in testServices)
                    {
                        services.Add(service);
                    }
                });
            });
  
  }
}

```
```csharp
{
   public static WebApplicationFactory<T> WebApplicationFactory<T>(IServiceCollection testServices, 
   bool enableTestAuthPolicy) where T : class =>
        new WebApplicationFactory<T>()
            .WithWebHostBuilder(builder =>
            {
                {...}
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
```
````
---
transition: slide-left
---
# Step 4 : Back to test, add WebApp
````md magic-move {lines: true}
```csharp {*}
[Fact]
public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner() {
  // Arrange
  var userNumber = "123456789";

  // Act
  var response = await client.GetAsync($"user-information/{userNumber}");
  var content = await response.Content.ReadAsStringAsync();
  var userInformationResponse = Json.Deserialize<UserInformationDto>(content);

  // Assert
}
```
```csharp
[Fact]
public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner() {
  // Arrange
  var factory = ServiceApiFactory.WebApplicationFactory<IApiMarker>(testServices: null, enableTestAuthPolicy: false);
  var client = factory.CreateClient();

  var userNumber = "123456789";

  // Act
  var response = await client.GetAsync($"user-information/{userNumber}");
  var content = await response.Content.ReadAsStringAsync();
  var userInformationResponse = Json.Deserialize<UserInformationDto>(content);

  // Assert
}
```
```csharp
[Fact]
public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner() {
  // Arrange
  // Fake external dependency
  // .. 
  var factory = ServiceApiFactory.WebApplicationFactory<IApiMarker>(testServices: null, enableTestAuthPolicy: false);
  var client = factory.CreateClient();

  var userNumber = "123456789";

  // Act
  var response = await client.GetAsync($"user-information/{userNumber}");
  var content = await response.Content.ReadAsStringAsync();
  var userInformationResponse = Json.Deserialize<UserInformationDto>(content);

  // Assert
}
```
````
---
transition: slide-left
---
# Problems
<div style="margin-bottom: 20px;" v-click>
Slow - we're spinning up a fake web app for each test
</div>

<div style="margin-bottom: 20px;" v-click>
Arrange can grow when there's need for faking more services
</div>

<div style="margin-bottom: 20px;" v-click>
So let's fix it
</div>

---
transition: slide-left
---
# Step 5 : Handle arrange part - create a ClientBuilder
```csharp {*}
public class ClientBuilder
{
    private readonly ServiceCollection serviceCollection = new();
    private WebApplicationFactory<IApiMarker> factory = new();
    private bool enableTestAuthPolicy;
        
    public ClientBuilder AddScopedFaker<TService, TImplementation>(TImplementation faker) 
        where TService : class 
        where TImplementation : class, TService {
        this.serviceCollection.AddScoped<TService>(_ => faker);
        return this;
    }
    
    public ClientBuilder EnableTestAuthPolicy() {
        this.enableTestAuthPolicy = true;
        return this;
    }

    public HttpClient Build() {
        this.factory = AccountsApiFactory.WebApplicationFactory<IApiMarker>(this.serviceCollection, 
        this.enableTestAuthPolicy);
        var client = this.factory.CreateClient();
        return client;
    }
}
```
---
transition: slide-left
---
# Step 6 : Performance - introduce fixture (XUnit)

```csharp
/// <summary>
///     Improves the time it takes for each test to execute when using WebAppFactory and injecting as IClassFixture.
/// </summary>
public class ServiceApiFakeOwnerFixture
{
  public ServiceApiFakeOwnerFixture()
  {
    var clientBuilder = new ClientBuilder();
    
    this.ClientWithFakeOwnerAuth = clientBuilder
      .EnableTestAuthPolicy()
      .AddScopedFaker<RealServiceToOverride, FakeService>(new FakeService({..., NullLogger<FakeService>.Instance}))
      .AddScopedFaker<IAuthenticationService, FakeOwnerAuthenticationService>(new FakeOwnerAuthenticationService())
      .Build();
  }

  public HttpClient ClientWithFakeOwnerAuth { get; private set; }
}
```

---
transition: slide-left
---
# Step 7 : Apply 
````md magic-move {lines: true}
```csharp {*}
public class GetUserInformationTests
{
  [Fact]
  public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner() {
    // Arrange
    // Fake external dependency
    // .. 
    var factory = ServiceApiFactory.WebApplicationFactory<IApiMarker>(testServices: null, enableTestAuthPolicy: false);
    var client = factory.CreateClient();

    var userNumber = "123456789";

    // Act
    var response = await client.GetAsync($"user-information/{userNumber}");
    var content = await response.Content.ReadAsStringAsync();
    var userInformationResponse = Json.Deserialize<UserInformationDto>(content);

    // Assert
  }
}
```
```csharp
public class GetUserInformationTests
{
  [Fact]
  public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner()
  {
    // Remove arrange
    // Act
    var response = await client.GetAsync("user-information/123456789");
    var content = await response.Content.ReadAsStringAsync();
    var userInformationResponse = Json.Deserialize<UserInformationDto>(content);
    // Assert
  }
}
```
```csharp
public class GetUserInformationTests(ServiceApiFakeOwnerFixture fakeOwnerFixture) : 
IClassFixture<ServiceApiFakeOwnerFixture>
{
  private readonly HttpClient clientWithFakeOwnerAuth = 
  fakeOwnerFixture.ClientWithFakeOwnerAuth;

  [Fact]
  public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner()
  {
    // Act
    var response = await clientWithFakeOwnerAuth.GetAsync("user-information/123456789");
    var content = await response.Content.ReadAsStringAsync();
    var userInformationResponse = Json.Deserialize<UserInformationDto>(content);
    // Assert
  }
}
```
````

---
transition: slide-left
---
# Step 8: Write asserts

````md magic-move {lines: true}
```csharp {*}
public class GetUserInformationTests(ServiceApiFakeOwnerFixture fakeOwnerFixture) : 
IClassFixture<ServiceApiFakeOwnerFixture>
{
  private readonly HttpClient clientWithFakeOwnerAuth = 
  fakeOwnerFixture.ClientWithFakeOwnerAuth;

  public static GetUserInformation ValidCustomer() =>
    new(
        new UserNumber("ValidCustomer.UserNumber"),
        new Name("name", "lastname"),
        new Address("Address 12345B", "1234", "Bergen", "Norway"),
        Birthday.TryCreate(1990, 01, 01),
        new EmailAddress("test@test.no"),
        new Telephone("12345654"));

  [Fact]
  public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner()
  {
    // Act
    var response = await clientWithFakeOwnerAuth.GetAsync($"user-information/{ValidCustomer.CustomerNumber.Number}");
    var content = await response.Content.ReadAsStringAsync();
    var userInformationResponse = Json.Deserialize<UserInformationDto>(content);
    // Assert
    userInformationResponse.Should().NotBeNull();
    userInformationResponse.CustomerNumber.Should().Be(this.requestedCustomer.CustomerNumber.Number);
    userInformationResponse.Name.Should().Be($"{this.requestedCustomer.Name.FirstName} {requestedCustomer.Name.LastName}");
    userInformationResponse.Address.Should().Be($"{this.requestedCustomer.Address.StreetAddress}," + $" {requestedCustomer.Address.PostalCode} {requestedCustomer.Address.City}");
    userInformationResponse.Birthday.Should().Be(this.requestedCustomer.Birthday.Match(Cr.Birthday.ToString, () => null));
    userInformationResponse.Email.Should().Be(this.requestedCustomer.EmailAddress.Match(e => e.Address, () => null));
    userInformationResponse.TelephoneNumber.Should().Be(this.requestedCustomer.TelephoneNumber.Match(e => e.Number, () => null));}
}
```
```csharp
public class GetUserInformationTests(ServiceApiFakeOwnerFixture fakeOwnerFixture) : 
IClassFixture<ServiceApiFakeOwnerFixture>
{
  private readonly HttpClient clientWithFakeOwnerAuth = 
  fakeOwnerFixture.ClientWithFakeOwnerAuth;
  
  private readonly UserInformation.GetUserInformation requestedUser = FakeUser.ValidCustomer();

  [Fact]
  public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner()
  {
    // Act
    var response = await clientWithFakeOwnerAuth.GetAsync($"user-information/{requestedUser.CustomerNumber.Number}");
    var content = await response.Content.ReadAsStringAsync();
    var userInformationResponse = Json.Deserialize<UserInformationDto>(content);
    // Assert
    userInformationResponse.Should().NotBeNull();
    userInformationResponse.CustomerNumber.Should().Be(this.requestedCustomer.CustomerNumber.Number);
    userInformationResponse.Name.Should().Be($"{this.requestedCustomer.Name.FirstName} {requestedCustomer.Name.LastName}");
    userInformationResponse.Address.Should().Be($"{this.requestedCustomer.Address.StreetAddress}," + $" {requestedCustomer.Address.PostalCode} {requestedCustomer.Address.City}");
    userInformationResponse.Birthday.Should().Be(this.requestedCustomer.Birthday.Match(Cr.Birthday.ToString, () => null));
    userInformationResponse.Email.Should().Be(this.requestedCustomer.EmailAddress.Match(e => e.Address, () => null));
    userInformationResponse.TelephoneNumber.Should().Be(this.requestedCustomer.TelephoneNumber.Match(e => e.Number, () => null));}
}
```

```csharp
public class GetUserInformationTests(ServiceApiFakeOwnerFixture fakeOwnerFixture) : 
IClassFixture<ServiceApiFakeOwnerFixture>
{
  private readonly HttpClient clientWithFakeOwnerAuth = 
  fakeOwnerFixture.ClientWithFakeOwnerAuth;
  
  private readonly UserInformation.GetUserInformation requestedUser = FakeUser.ValidCustomer();

  [Fact]
  public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner()
  {
    // Act
    var response = await clientWithFakeOwnerAuth.GetAsync($"user-information/{requestedUser.CustomerNumber.Number}");
    var content = await response.Content.ReadAsStringAsync();
    var userInformationResponse = Json.Deserialize<UserInformationDto>(content);

    // Assert
    this.AssertUserInformationResponse(userInformationResponse);
  }
}
```
````
---
transition: slide-left
---
# Bonus Step 9 : Let's get functional

```csharp {*}
public class GetUserInformationTests(ServiceApiFakeOwnerFixture fakeOwnerFixture) : 
IClassFixture<ServiceApiFakeOwnerFixture>
{
  private readonly HttpClient clientWithFakeOwnerAuth = 
  fakeOwnerFixture.ClientWithFakeOwnerAuth;
  
  private readonly UserInformation.GetUserInformation requestedUser = FakeUser.ValidCustomer();

  [Fact]
  public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner()
  {
    // Act
    var response = await clientWithFakeOwnerAuth.GetAsync($"user-information/{requestedUser.CustomerNumber.Number}");
    var content = await response.Content.ReadAsStringAsync();
    var userInformationResponse = Json.Deserialize<UserInformationDto>(content);
    // Assert

    this.AssertUserInformationResponse(userInformationResponse);
  }
}
```
---
transition: slide-left
---
# Intoduce generic methods : Try
```csharp
  public static async Task<Result<HttpResponseMessage>> Try(this Task<HttpResponseMessage?> performRequestAsync, 
  ILogger logger, string? errorMessage = null)
    {
        HttpResponseMessage? response = null;
        try
        {
            response = await performRequestAsync;
            return response == null ? Result<HttpResponseMessage>.Fail(new Exception(ErrorType.Dependency, errorMessage ?? "No response")) : Result<HttpResponseMessage>.Ok(response);
        }
        catch (Exception e)
        {
            if (response?.Content == null)
            {
                return Result<HttpResponseMessage>.Fail(new Exception(ErrorType.Dependency, errorMessage ?? e.Message, e));
            }

            try
            {
                logger.LogError(e, "{response}", await response.Content.ReadAsStringAsync());
            }
            catch
            {
                return Result<HttpResponseMessage>.Fail(new Exception(ErrorType.Dependency, errorMessage ?? e.Message, e));
            }

            return Result<HttpResponseMessage>.Fail(new Exception(ErrorType.Dependency, errorMessage ?? e.Message, e));
        }
    }
```

---
transition: slide-left
---
# Intoduce generic methods : Bind
```csharp
    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Task<Result<TIn>> t, Func<TIn, Task<Result<TOut>>> f)
        => await (await t).Bind(f);
```

---
transition: slide-left
---
# Bonus Step 9 : Get rid of async and apply functional methods
````md magic-move {lines: true}
```csharp {*}
public class GetUserInformationTests(ServiceApiFakeOwnerFixture fakeOwnerFixture) : 
IClassFixture<ServiceApiFakeOwnerFixture>
{
  private readonly HttpClient clientWithFakeOwnerAuth = fakeOwnerFixture.ClientWithFakeOwnerAuth;
  private readonly UserInformation.GetUserInformation requestedUser = FakeUser.ValidCustomer();

  [Fact]
  public async Task Should_return_users_information_if_user_number_exists_and_user_is_owner()
  {
    // Act
    var response = await clientWithFakeOwnerAuth.GetAsync($"user-information/{requestedUser.CustomerNumber.Number}");
    var content = await response.Content.ReadAsStringAsync();
    var userInformationResponse = Json.Deserialize<UserInformationDto>(content);
    // Assert
    this.AssertUserInformationResponse(userInformationResponse);
  }
}
```

```csharp
public class GetUserInformationTests(ServiceApiFakeOwnerFixture fakeOwnerFixture) : 
IClassFixture<ServiceApiFakeOwnerFixture>
{
  private readonly HttpClient clientWithFakeOwnerAuth = fakeOwnerFixture.ClientWithFakeOwnerAuth;
  private readonly UserInformation.GetUserInformation requestedUser = FakeUser.ValidCustomer();

  [Fact]
  public Task Should_return_users_information_if_user_number_exists_and_user_is_owner()
  {
    // Act
    var response = clientWithFakeOwnerAuth.GetAsync($"user-information/{requestedUser.CustomerNumber.Number}");
    var content = response.Content.ReadAsStringAsync();
    var userInformationResponse = Json.Deserialize<UserInformationDto>(content);
    // Assert
    this.AssertUserInformationResponse(userInformationResponse);
  }
}
```
```csharp
public class GetUserInformationTests(ServiceApiFakeOwnerFixture fakeOwnerFixture) : 
IClassFixture<ServiceApiFakeOwnerFixture>
{
  private readonly HttpClient clientWithFakeOwnerAuth = fakeOwnerFixture.ClientWithFakeOwnerAuth;
  private readonly UserInformation.GetUserInformation requestedUser = FakeUser.ValidCustomer();

  [Fact]
  public Task Should_return_users_information_if_user_number_exists_and_user_is_owner()
  {
    // Act
    var response = clientWithFakeOwnerAuth
      .GetAsync($"user-information/{requestedUser.CustomerNumber.Number}")
      .Try(logger, "optional exception message");
    var content = response.Content.ReadAsStringAsync();
    var userInformationResponse = Json.Deserialize<UserInformationDto>(content);
    // Assert
    this.AssertUserInformationResponse(userInformationResponse);
  }
}
```

```csharp
public class GetUserInformationTests(ServiceApiFakeOwnerFixture fakeOwnerFixture) : 
IClassFixture<ServiceApiFakeOwnerFixture>
{
  private readonly HttpClient clientWithFakeOwnerAuth = fakeOwnerFixture.ClientWithFakeOwnerAuth;
  private readonly UserInformation.GetUserInformation requestedUser = FakeUser.ValidCustomer();

  [Fact]
  public Task Should_return_users_information_if_user_number_exists_and_user_is_owner()
  {
    // Act
    var response = clientWithFakeOwnerAuth
      .GetAsync($"user-information/{requestedUser.CustomerNumber.Number}")
      .Try()
      .Bind(x => x.Content.ReadAsStringAsync().Try());

    var userInformationResponse = Json.Deserialize<UserInformationDto>(content);
    // Assert
    this.AssertUserInformationResponse(userInformationResponse);
  }
}
```


```csharp
public class GetUserInformationTests(ServiceApiFakeOwnerFixture fakeOwnerFixture) : 
IClassFixture<ServiceApiFakeOwnerFixture>
{
  private readonly HttpClient clientWithFakeOwnerAuth = fakeOwnerFixture.ClientWithFakeOwnerAuth;
  private readonly UserInformation.GetUserInformation requestedUser = FakeUser.ValidCustomer();

  [Fact]
  public Task Should_return_users_information_if_user_number_exists_and_user_is_owner()
  {
    // Act
    var response = clientWithFakeOwnerAuth
      .GetAsync($"user-information/{requestedUser.CustomerNumber.Number}")
      .Try()
      .Bind(x => x.Content.ReadAsStringAsync().Try())
      .Bind(s => Result<UserInformationDto>.Ok(Json.Deserialize<UserInformationDto>(s)));
        
    // Assert
    this.AssertUserInformationResponse(userInformationResponse);
  }
}
```

```csharp
public class GetUserInformationTests(ServiceApiFakeOwnerFixture fakeOwnerFixture) : 
IClassFixture<ServiceApiFakeOwnerFixture>
{
  private readonly HttpClient clientWithFakeOwnerAuth = fakeOwnerFixture.ClientWithFakeOwnerAuth;
  private readonly UserInformation.GetUserInformation requestedUser = FakeUser.ValidCustomer();

  [Fact]
  public Task Should_return_users_information_if_user_number_exists_and_user_is_owner()
  {
    // Act
    var response = clientWithFakeOwnerAuth
      .GetAsync($"user-information/{requestedUser.CustomerNumber.Number}")
      .Try()
      .Bind(x => x.Content.ReadAsStringAsync().Try())
      .Bind(s => Result<UserInformationDto>.Ok(Json.Deserialize<UserInformationDto>(s)))
      .Bind(this.AssertContactInformationResponse);
  }
}
```

```csharp
public class GetUserInformationTests(ServiceApiFakeOwnerFixture fakeOwnerFixture) : 
IClassFixture<ServiceApiFakeOwnerFixture>
{
  private readonly HttpClient clientWithFakeOwnerAuth = fakeOwnerFixture.ClientWithFakeOwnerAuth;
  private readonly UserInformation.GetUserInformation requestedUser = FakeUser.ValidCustomer();

  [Fact]
  public Task Should_return_users_information_if_user_number_exists_and_user_is_owner()
  {
    var response = clientWithFakeOwnerAuth
      .GetAsync($"user-information/{requestedUser.CustomerNumber.Number}")
      .Try()
      .Bind(x => x.Content.ReadAsStringAsync().Try())
      .Bind(s => Result<UserInformationDto>.Ok(Json.Deserialize<UserInformationDto>(s)))
      .Bind(this.AssertContactInformationResponse);
  }
}
```

```csharp
public class GetUserInformationTests(ServiceApiFakeOwnerFixture fakeOwnerFixture) : 
IClassFixture<ServiceApiFakeOwnerFixture>
{
  private readonly HttpClient clientWithFakeOwnerAuth = fakeOwnerFixture.ClientWithFakeOwnerAuth;
  private readonly UserInformation.GetUserInformation requestedUser = FakeUser.ValidCustomer();

  [Fact]
  public Task Should_return_users_information_if_user_number_exists_and_user_is_owner() =>
    clientWithFakeOwnerAuth
      .GetAsync($"user-information/{requestedUser.CustomerNumber.Number}")
      .Try()
      .Bind(x => x.Content.ReadAsStringAsync().Try())
      .Bind(s => Result<UserInformationDto>.Ok(Json.Deserialize<UserInformationDto>(s)))
      .Bind(this.AssertContactInformationResponse);
}
```
````
---
transition: slide-up
level: 2
---
# Thank you

Repo with presentation (Slidev)
https://github.com/GregoryBa/integration-testing-sli

<img src="qr.png" alt="qr" width="200" height="200"/>


---
transition: slide-up
level: 2
---
# TestingPolicyEvaluator
```csharp
internal sealed class TestingPolicyEvaluator : IPolicyEvaluator
{
    public Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
    {
        const string testScheme = "TestScheme";
        var principal = new ClaimsPrincipal();
        principal.AddIdentity(new ClaimsIdentity(
            new[]
            {
                new Claim("Permission", "CanViewPage"),
                new Claim("Manager", "yes"),
                new Claim(ClaimTypes.Role, "Administrator"),
                new Claim(ClaimTypes.NameIdentifier, "Test"),
            }, 
            testScheme));

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, new AuthenticationProperties(), testScheme)));
    }

    public Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, AuthenticateResult authenticateResult, HttpContext context, object resource)
    {
        return Task.FromResult(PolicyAuthorizationResult.Success());
    }
}
```
---
transition: slide-up
level: 2
---
# Fake Authentication Service
```csharp
public class FakeOwnerAuthenticationService : IAuthenticationService
{
    public Task<Result<CustomerNumbers>> GetCustomerNumbers() =>
        Task.FromResult(this.ValidTestCustomerNumbers());

    public Task<Result<CustomerNumber>> GetCustomerNumber(string customerNumber) =>
        Task.FromResult((new OwnerCustomerNumber(customerNumber) as CustomerNumber)
            .AsResult(() => new Exception(ErrorType.Forbidden, $"Customer number is not a testing customer number")));
    
    private Result<CustomerNumbers> ValidTestCustomerNumbers() => throw new NotImplementedException();
}
```

---
transition: slide-up
level: 2
---
# Fake user
```csharp
    public static GetUserInformation ValidCustomer() =>
        new(
            new UserNumber("1234567890"),
            new Name("name", "lastname"),
            new Address("Address 12345B", "1234", "Bergen", "Norway"),
            Birthday.TryCreate(1990, 01, 01),
            new EmailAddress("test@test.no"),
            new Telephone("12345654"));
```