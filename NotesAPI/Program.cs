using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddPolicy("spa", p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()
));

// ✅ Add Controllers
builder.Services.AddControllers();

// AWS
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonDynamoDB>();

var app = builder.Build();

app.UseCors("spa");

// ✅ Map controllers
app.MapControllers();

// Keep health if you want it minimal
app.MapGet("/health", () => Results.Ok(new { ok = true }));

app.Run();