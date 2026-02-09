using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Extensions.NETCore.Setup;
using static NotesAPI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o => o.AddPolicy("spa", p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()
));

// AWS
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonDynamoDB>();

var app = builder.Build();
app.UseCors("spa");

var tableName = builder.Configuration["NOTES_TABLE"] ?? "Notes";

app.MapGet("/health", () => Results.Ok(new { ok = true }));



// READ: list notes for a user
app.MapGet("/notes", async (string userId, IAmazonDynamoDB db) =>
{
    var resp = await db.QueryAsync(new QueryRequest
    {
        TableName = tableName,
        KeyConditionExpression = "UserId = :u",
        ExpressionAttributeValues = new()
        {
            [":u"] = new AttributeValue { S = userId }
        },
        ScanIndexForward = false 
    });

    var notes = resp.Items.Select(i => new NoteItem(
        i["UserId"].S!,
        i["NoteId"].S!,
        i["Text"].S!,
        i["CreatedAt"].S!
    ));

    return Results.Ok(notes);
});



// CREATE
RouteHandlerBuilder routeHandlerBuilder = app.MapPost("/notes", async (string userId, CreateNoteRequest req, IAmazonDynamoDB db) =>
{
    if (string.IsNullOrWhiteSpace(req.Text))
        return Results.BadRequest("Text is required.");

    var noteId = Guid.NewGuid().ToString("N");
    var createdAt = DateTime.UtcNow.ToString("O");

    await db.PutItemAsync(new PutItemRequest
    {
        TableName = tableName,
        Item = new()
        {
            ["UserId"] = new AttributeValue { S = userId },
            ["NoteId"] = new AttributeValue { S = noteId },
            ["Text"] = new AttributeValue { S = req.Text },
            ["CreatedAt"] = new AttributeValue { S = createdAt }
        },
        ConditionExpression = "attribute_not_exists(UserId) AND attribute_not_exists(NoteId)"
    });

    return Results.Created($"/notes/{noteId}", new NoteItem(userId, noteId, req.Text, createdAt));
});

// DELETE
app.MapDelete("/notes/{noteId}", async (string userId, string noteId, IAmazonDynamoDB db) =>
{
    await db.DeleteItemAsync(new DeleteItemRequest
    {
        TableName = tableName,
        Key = new()
        {
            ["UserId"] = new AttributeValue { S = userId },
            ["NoteId"] = new AttributeValue { S = noteId }
        }
    });

    return Results.NoContent();
});


app.Run();
