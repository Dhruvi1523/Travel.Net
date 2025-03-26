using System.Text;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // Use AddControllers() for a Web API (instead of AddControllersWithViews)

builder.Services.AddScoped<IMongoDbService, MongoDbService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Configure HttpClient for AirScrapper API
builder.Services.AddHttpClient("AirScrapperClient", client =>
{
    var flightApiConfig = builder.Configuration.GetSection("AirScrapperApi");
    client.BaseAddress = new Uri(flightApiConfig["BaseUrl"]);
    client.DefaultRequestHeaders.Add("X-RapidAPI-Key", flightApiConfig["ApiKey"]);
    client.DefaultRequestHeaders.Add("X-RapidAPI-Host", flightApiConfig["ApiHost"]);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IFlightService, FlightService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("AirScrapperClient");
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<FlightService>>();
    return new FlightService(httpClient, configuration, logger);
});

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Check for token in Authorization header (for Postman)
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                }
                // Fallback to cookie (for frontend)
                else if (context.Request.Cookies.ContainsKey("TravelAccessToken"))
                {
                    context.Token = context.Request.Cookies["TravelAccessToken"];
                }
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// Configure CORS to allow both frontend and Postman
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendAndPostman", builder =>
    {
        builder
            .WithOrigins("http://localhost:8080") // Allow frontend origin
            .AllowAnyOrigin()                     // Allow all origins (for Postman in development)
            .AllowAnyMethod()                     // Allow GET, POST, etc.
            .AllowAnyHeader();                    // Allow any headers (e.g., Authorization, Content-Type)
    });
});

// Configure logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); // Detailed error pages for development
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\": \"An unexpected error occurred.\"}");
        });
    });
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowFrontendAndPostman");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // Map API controllers (instead of MVC routing)

app.Run();