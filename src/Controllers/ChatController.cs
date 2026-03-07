using InstruaMe.Domain.Entities;
using InstruaMe.Domain.Models.Results;
using InstruaMe.Infrastructure.ORM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InstruaMe.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly InstruaMeDbContext _dbContext;

        public ChatController(InstruaMeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Authorize]
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations(CancellationToken ct)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var conversations = await _dbContext.Conversations
                .Include(c => c.Instructor)
                .Include(c => c.Student)
                .Where(c => c.InstructorId == userId || c.StudentId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new ConversationResult
                {
                    Id = c.Id,
                    InstructorId = c.InstructorId,
                    InstructorName = c.Instructor.Name,
                    StudentId = c.StudentId,
                    StudentName = c.Student.Name,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(conversations);
        }

        [Authorize(Roles = "Student")]
        [HttpPost("conversations/{instructorId:guid}")]
        public async Task<IActionResult> GetOrCreateConversation(Guid instructorId, CancellationToken ct)
        {
            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var instructorExists = await _dbContext.Instructors
                .AnyAsync(x => x.Id == instructorId && !x.Deleted, ct);

            if (!instructorExists)
                return NotFound(new { message = "Instrutor não encontrado." });

            var existing = await _dbContext.Conversations
                .Include(c => c.Instructor)
                .Include(c => c.Student)
                .FirstOrDefaultAsync(c => c.InstructorId == instructorId && c.StudentId == studentId, ct);

            if (existing is not null)
            {
                return Ok(new ConversationResult
                {
                    Id = existing.Id,
                    InstructorId = existing.InstructorId,
                    InstructorName = existing.Instructor.Name,
                    StudentId = existing.StudentId,
                    StudentName = existing.Student.Name,
                    CreatedAt = existing.CreatedAt
                });
            }

            var conversation = new Conversation(instructorId, studentId);
            _dbContext.Conversations.Add(conversation);
            await _dbContext.SaveChangesAsync(ct);

            var instructor = await _dbContext.Instructors.FindAsync(instructorId);
            var student = await _dbContext.Students.FindAsync(studentId);

            return Created(string.Empty, new ConversationResult
            {
                Id = conversation.Id,
                InstructorId = conversation.InstructorId,
                InstructorName = instructor!.Name,
                StudentId = conversation.StudentId,
                StudentName = student!.Name,
                CreatedAt = conversation.CreatedAt
            });
        }

        [Authorize]
        [HttpGet("conversations/{id:guid}/messages")]
        public async Task<IActionResult> GetMessages(Guid id, CancellationToken ct)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (conversation is null)
                return NotFound();

            if (userId != conversation.InstructorId && userId != conversation.StudentId)
                return Forbid();

            var messages = await _dbContext.ChatMessages
                .Where(m => m.ConversationId == id)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageResult
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    SenderId = m.SenderId,
                    SenderRole = m.SenderRole,
                    Content = m.Content,
                    Read = m.Read,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(messages);
        }
    }
}
