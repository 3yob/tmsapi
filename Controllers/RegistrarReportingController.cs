using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Controllers
{
    [ApiController]
    [Route("api/registrar/reports")]
    public class RegistrarReportingController : ControllerBase
    {
        private readonly TmsDbContext _context;

        public RegistrarReportingController(TmsDbContext context)
        {
            _context = context;
        }

        // 1. How many active students have GPA >= 3.0?
        [HttpGet("active-high-gpa-count")]
        public async Task<ActionResult<int>> GetActiveHighGpaCount()
        {
            var count = await _context.Students
                .Where(s => s.IsActive && s.GPA >= 3.0m)
                .CountAsync();

            return Ok(count);
        }

        // 2. Which courses have the most enrollments, sorted descending?
        [HttpGet("courses-by-enrollment")]
        public async Task<ActionResult> GetCoursesByEnrollment()
        {
            var list = await _context.Courses
                .Select(c => new
                {
                    c.Title,
                    EnrollmentCount = c.Enrollments.Count
                })
                .OrderByDescending(x => x.EnrollmentCount)
                .ToListAsync();

            return Ok(list);
        }

        // 3. What is the average GPA per course?
        [HttpGet("average-gpa-per-course")]
        public async Task<ActionResult> GetAverageGpaPerCourse()
        {
            var list = await _context.Enrollments
                .GroupBy(e => e.Course.Title)
                .Select(g => new
                {
                    Course = g.Key,
                    AverageGPA = g.Average(e => e.Student.GPA)
                })
                .ToListAsync();

            return Ok(list);
        }

        // 4. Which students have zero enrollments? (Approach A: Subquery)
        [HttpGet("unenrolled-students-subquery")]
        public async Task<ActionResult> GetUnenrolledStudentsSubquery()
        {
            var list = await _context.Students
                .Where(s => !s.Enrollments.Any())
                .Select(s => s.Name)
                .ToListAsync();

            return Ok(list);
        }

        // 4. Which students have zero enrollments? (Approach B: EF Core LeftJoin)
        [HttpGet("unenrolled-students-leftjoin")]
        public async Task<ActionResult> GetUnenrolledStudentsLeftJoin()
        {
            var list = await _context.Students
                .LeftJoin(_context.Enrollments,
                    s => s.Id,
                    e => e.StudentId,
                    (s, e) => new { s, e })
                .Where(x => x.e == null)
                .Select(x => x.s.Name)
                .ToListAsync();

            return Ok(list);
        }
    }
}