using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Mvc;
using static NotesAPI.Models;

namespace NotesAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class NotesController : ControllerBase
{
    private readonly IAmazonDynamoDB _db;
    private readonly string _tableName;

    public NotesController(IAmazonDynamoDB db, IConfiguration config)
    {
        _db = db;
        _tableName = config["NOTES_TABLE"] ?? "Notes";
    }

    // GET /notes?userId=public
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NoteItem>>> GetNotes([FromQuery] string userId)
    {
        var resp = await _db.QueryAsync(new QueryRequest
        {
            TableName = _tableName,
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

        return Ok(notes);
    }

    // POST /notes?userId=public
    [HttpPost]
    public async Task<ActionResult<NoteItem>> CreateNote([FromQuery] string userId, [FromBody] CreateNoteRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Text))
            return BadRequest("Text is required.");

        var noteId = Guid.NewGuid().ToString("N");
        var createdAt = DateTime.UtcNow.ToString("O");

        await _db.PutItemAsync(new PutItemRequest
        {
            TableName = _tableName,
            Item = new()
            {
                ["UserId"] = new AttributeValue { S = userId },
                ["NoteId"] = new AttributeValue { S = noteId },
                ["Text"] = new AttributeValue { S = req.Text },
                ["CreatedAt"] = new AttributeValue { S = createdAt }
            },
            ConditionExpression = "attribute_not_exists(UserId) AND attribute_not_exists(NoteId)"
        });

        return Created($"/notes/{noteId}", new NoteItem(userId, noteId, req.Text, createdAt));
    }

    // DELETE /notes/{noteId}?userId=public
    [HttpDelete("{noteId}")]
    public async Task<IActionResult> DeleteNote([FromRoute] string noteId, [FromQuery] string userId)
    {
        await _db.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = _tableName,
            Key = new()
            {
                ["UserId"] = new AttributeValue { S = userId },
                ["NoteId"] = new AttributeValue { S = noteId }
            }
        });

        return NoContent();
    }
}
