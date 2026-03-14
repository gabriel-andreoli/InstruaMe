using InstruaMe.Domain.Entities;
using InstruaMe.Domain.Models.Commands;
using InstruaMe.Domain.Models.Results;
using InstruaMe.Infrastructure.ORM;
using InstruaMe.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InstruaMe.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class InstructorController : ControllerBase
    {
        private readonly InstruaMeDbContext _dbContext;

        public InstructorController(InstruaMeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ListInstructorsQuery query, CancellationToken ct)
        {
            var baseQuery = _dbContext.Instructors
                .Where(x => !x.Deleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Name))
                baseQuery = baseQuery.Where(x => EF.Functions.ILike(x.Name, $"%{query.Name}%"));

            if (!string.IsNullOrWhiteSpace(query.City))
                baseQuery = baseQuery.Where(x => x.City == query.City);

            if (!string.IsNullOrWhiteSpace(query.State))
                baseQuery = baseQuery.Where(x => x.State == query.State);

            if (!string.IsNullOrWhiteSpace(query.CarModel))
                baseQuery = baseQuery.Where(x => x.CarModel == query.CarModel);

            if (query.MaxPricePerHour.HasValue)
                baseQuery = baseQuery.Where(x => x.PricePerHour <= query.MaxPricePerHour.Value);

            var projected = baseQuery.Select(x => new InstructorCardResult
            {
                Id = x.Id,
                Name = x.Name,
                Photo = x.Photo,
                City = x.City,
                State = x.State,
                CarModel = x.CarModel,
                PricePerHour = x.PricePerHour,
                AverageRating = x.Reviews.Any() ? x.Reviews.Average(r => (double)r.Rating) : 0,
                TotalReviews = x.Reviews.Count()
            });

            if (query.MinRating.HasValue)
                projected = projected.Where(x => x.AverageRating >= query.MinRating.Value);

            var totalCount = await projected.CountAsync(ct);

            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

            var items = await projected
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Ok(new PagedResult<InstructorCardResult>(items, page, pageSize, totalCount));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var instructor = await _dbContext.Instructors
                .Include(x => x.Reviews)
                    .ThenInclude(r => r.Student)
                .FirstOrDefaultAsync(x => x.Id == id && !x.Deleted, ct);

            if (instructor is null)
                return NotFound();

            var result = new InstructorProfileResult
            {
                Id = instructor.Id,
                Name = instructor.Name,
                Email = instructor.Email,
                PhoneNumber = instructor.PhoneNumber,
                State = instructor.State,
                City = instructor.City,

                Birthday = instructor.Birthday,
                CarModel = instructor.CarModel,
                Biography = instructor.Biography,
                Description = instructor.Description,
                Photo = instructor.Photo,
                PricePerHour = instructor.PricePerHour,
                AverageRating = instructor.Reviews.Any() ? instructor.Reviews.Average(r => (double)r.Rating) : 0,
                TotalReviews = instructor.Reviews.Count,
                Reviews = instructor.Reviews.Select(r => new ReviewResult
                {
                    Id = r.Id,
                    StudentId = r.StudentId,
                    StudentName = r.Student.Name,
                    StudentPhoto = r.Student.Photo,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            return Ok(result);
        }

        [Authorize(Roles = "Instructor")]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe(
            [FromBody] UpdateInstructorCommand command,
            [FromServices] PhotoService photoService,
            CancellationToken ct)
        {
            if (command.Photo is not null)
            {
                try { command.Photo = photoService.ResizeToThumbnail(command.Photo); }
                catch { return BadRequest(new { message = "Foto inválida. Envie uma imagem em base64." }); }
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var instructor = await _dbContext.Instructors
                .FirstOrDefaultAsync(x => x.Id == userId && !x.Deleted, ct);

            if (instructor is null)
                return NotFound();

            instructor.Update(command);

            await _dbContext.SaveChangesAsync(ct);

            return NoContent();
        }

        [Authorize(Roles = "Instructor")]
        [HttpGet("me/dashboard")]
        public async Task<IActionResult> Dashboard(CancellationToken ct)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var reviews = await _dbContext.Reviews
                .Include(r => r.Student)
                .Where(r => r.InstructorId == userId)
                .ToListAsync(ct);

            var result = new InstructorDashboardResult
            {
                TotalStudentReviewers = reviews.Select(r => r.StudentId).Distinct().Count(),
                AverageRating = reviews.Any() ? reviews.Average(r => (double)r.Rating) : 0,
                RecentReviews = reviews
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .Select(r => new ReviewResult
                    {
                        Id = r.Id,
                        StudentId = r.StudentId,
                        StudentName = r.Student.Name,
                        StudentPhoto = r.Student.Photo,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt
                    }).ToList()
            };

            return Ok(result);
        }

        [Authorize(Roles = "Student")]
        [HttpPost("{id:guid}/reviews")]
        public async Task<IActionResult> SubmitReview(Guid id, [FromBody] SubmitReviewCommand command, CancellationToken ct)
        {
            if (command.Rating < 1 || command.Rating > 5)
                return BadRequest(new { message = "Rating deve ser entre 1 e 5." });

            var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var instructorExists = await _dbContext.Instructors
                .AnyAsync(x => x.Id == id && !x.Deleted, ct);

            if (!instructorExists)
                return NotFound();

            var review = new Review(id, studentId, command.Rating, command.Comment);

            _dbContext.Reviews.Add(review);

            try
            {
                await _dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "Você já avaliou este instrutor." });
            }

            return Created();
        }

        [HttpGet("{id:guid}/reviews")]
        public async Task<IActionResult> GetReviews(Guid id, CancellationToken ct)
        {
            var instructorExists = await _dbContext.Instructors
                .AnyAsync(x => x.Id == id && !x.Deleted, ct);

            if (!instructorExists)
                return NotFound();

            var reviews = await _dbContext.Reviews
                .Include(r => r.Student)
                .Where(r => r.InstructorId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewResult
                {
                    Id = r.Id,
                    StudentId = r.StudentId,
                    StudentName = r.Student.Name,
                    StudentPhoto = r.Student.Photo,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(reviews);
        }
    }
}
