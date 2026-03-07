using InstruaMe.Domain.Models.Commands;
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
    public class StudentController : ControllerBase
    {
        private readonly InstruaMeDbContext _dbContext;

        public StudentController(InstruaMeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var student = await _dbContext.Students
                .FirstOrDefaultAsync(x => x.Id == userId && !x.Deleted, ct);

            if (student is null)
                return NotFound();

            return Ok(new StudentProfileResult
            {
                Id = student.Id,
                Name = student.Name,
                Email = student.Email,
                Birthday = student.Birthday,
                Photo = student.Photo
            });
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var student = await _dbContext.Students
                .FirstOrDefaultAsync(x => x.Id == id && !x.Deleted, ct);

            if (student is null)
                return NotFound();

            return Ok(new StudentProfileResult
            {
                Id = student.Id,
                Name = student.Name,
                Email = student.Email,
                Birthday = student.Birthday,
                Photo = student.Photo
            });
        }

        [Authorize(Roles = "Student")]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateStudentCommand command, CancellationToken ct)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var student = await _dbContext.Students
                .FirstOrDefaultAsync(x => x.Id == userId && !x.Deleted, ct);

            if (student is null)
                return NotFound();

            student.Update(command);

            await _dbContext.SaveChangesAsync(ct);

            return NoContent();
        }
    }
}
