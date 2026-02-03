using InstruaMe.Domain.Contracts.Services;
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
    public class InstruaMeController : ControllerBase
    {
        private readonly InstruaMeDbContext _dbContext;
        private readonly IPasswordHasherService _passwordHasher;

        public InstruaMeController(InstruaMeDbContext dbContext, IPasswordHasherService passwordHasher)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("instructor", Name = "RegisterInstructor")]
        public async Task<IActionResult> RegisterInstructor(RegisterInstructorCommand command, CancellationToken ct) 
        {
            var passwordResult = _passwordHasher.Hash(command.Password);

            var instructor = new Instructor(command, passwordResult.hash, passwordResult.salt);

            await _dbContext.AddAsync(instructor, ct);

            await _dbContext.SaveChangesAsync(ct);

            return Created();
        }

        [HttpPost("student", Name = "RegisterStudent")]
        public async Task<IActionResult> RegisterStudent(RegisterStudentCommand command, CancellationToken ct) 
        {
            var passwordResult = _passwordHasher.Hash(command.Password);

            var student = new Student(command, passwordResult.hash, passwordResult.salt);

            await _dbContext.AddAsync(student, ct);

            await _dbContext.SaveChangesAsync(ct);

            return Created();
        }

        [HttpPost("login", Name = "Login")]
        public async Task<IActionResult> Login(
             LoginCommand command,
             [FromServices] JwtTokenService tokenService,
             CancellationToken ct)
        {
            var email = command.Email.ToLowerInvariant();

            var instructor = await _dbContext.Instructors.FirstOrDefaultAsync(x => x.Email.ToLower().Trim() == email.ToLower().Trim() && !x.Deleted, ct);

            if (instructor is not null)
            {
                if (!_passwordHasher.Verify(
                        command.Password,
                        instructor.PasswordHash,
                        instructor.PasswordSalt))
                    return Unauthorized();

                var token = tokenService.GenerateToken(
                    instructor.Id,
                    instructor.Email,
                    "Instructor"
                );

                return Ok(new LoginResult
                {
                    Token = token,
                    Role = instructor.Role
                });
            }

            var student = await _dbContext.Students.FirstOrDefaultAsync(x => x.Email.ToLower().Trim() == email.ToLower().Trim() && !x.Deleted, ct);

            if (student is not null)
            {
                if (!_passwordHasher.Verify(
                        command.Password,
                        student.PasswordHash,
                        student.PasswordSalt))
                    return Unauthorized();

                var token = tokenService.GenerateToken(
                    student.Id,
                    student.Email,
                    "Student"
                );

                return Ok(new LoginResult
                {
                    Token = token,
                    Role = student.Role
                });
            }

            return Unauthorized();
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Email = User.FindFirstValue(ClaimTypes.Email),
                Role = User.FindFirstValue(ClaimTypes.Role)
            });
        }
    }
}
