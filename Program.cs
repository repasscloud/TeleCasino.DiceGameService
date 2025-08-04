using Microsoft.OpenApi.Models;
using TeleCasino.DiceGameService.Services;
using TeleCasino.DiceGameService.Services.Interface;

var builder = WebApplication.CreateBuilder(args);

// Enable controllers
builder.Services.AddControllers();

// ✅ Register DiceGameService as singleton
builder.Services.AddSingleton<IDiceGameService, DiceGameService>();

// Enable OpenAPI (Swagger UI)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TeleCasino DiceGame API",
        Version = "v1",
        Description = "API to generate Dice game results and files."
    });
});

var app = builder.Build();

// Enable Swagger UI in dev environment
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TeleCasino DiceGame API v1");
        options.RoutePrefix = "swagger";
    });
}

// app.UseHttpsRedirection();

// Allow serving static files (JSON, MP4 results later)
app.UseStaticFiles();

app.MapControllers();

app.Run();
