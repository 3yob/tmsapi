using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi(); // Required for MapOpenApi and MapScalarApiReference

builder.Services.AddControllers();

//m5-exercise 1- add db context and connection string
// Register TmsDbContext scoped for incoming HTTP requests
builder.Services.AddDbContext<TmsDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))

//m5-exercise 2- add logging and sensitive data to db context step-1

.LogTo(Console.WriteLine, LogLevel.Information) // Log SQL to output window
.EnableSensitiveDataLogging()); // Show parameters in querylogs (dev only)

// Authentication / Authorization
builder.Services
    .AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);
builder.Services.AddAuthorization();

// enrollment service and worker
builder.Services.AddSingleton<EnrollmentWorker>();         
builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>(); 

// //student service and worker
// builder.Services.AddSingleton<IStudentService, StudentService>();
// builder.Services.AddSingleton<StudentWorker>();

// //course service and worker
// builder.Services.AddSingleton<ICourseService, CourseService>();
// builder.Services.AddSingleton<CourseWorker>();

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart(); 

// ProblemDetails registration
builder.Services.AddProblemDetails();

builder.Services.AddProblemDetails();
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});


var app = builder.Build();

// Middleware order
app.UseMiddleware<RequestLoggingMiddleware>(); // outer wrapper
// UseProblemDetails() is provided by external packages (e.g. Hellang.Middleware.ProblemDetails).
// If that package/using is not available, remove the call and rely on ExceptionHandler/StatusCodePages.
// app.UseProblemDetails();                        // format errors/status codes as JSON
app.UseExceptionHandler();                      // catch exceptions
app.UseStatusCodePages();                       // wrap empty-body codes
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
 app.MapOpenApi(); 
app.MapScalarApiReference();
 // Required for MapOpenApi and MapScalarApiReference  
// Endpoints
// app.MapGet("/api/assessments/results", () => Results.Ok(new
// {
//     courseCode = "CS-101",
//     studentId = "S-001", 
//     letterGrade = "A"
// })).RequireAuthorization();

//enrollment worker test route
// app.MapGet("/api/enrollments/worker-smoke", (EnrollmentWorker worker) =>
// {
//     worker.ProcessBatch();
//     return Results.Ok("processed");
// });

// //student worker test route
// app.MapGet("/api/students/worker-smoke", (StudentWorker worker) =>
// {
//     worker.ProcessBatch();
//     return Results.Ok("processed");
// });

// //course worker test route
// app.MapGet("/api/courses/worker-smoke", (CourseWorker worker) =>
// {
//     worker.ProcessBatch();
//     return Results.Ok("processed");
// });

// //enrollment test route
// app.MapPost("/api/enrollments/test", async (IEnrollmentService service) =>
// {
//     // … your test logic …
//     return Results.Ok("Logging test completed - check console for structured logs");
// });

//student test route
// app.MapPost("/api/students/test", async (IStudentService service) =>
// {
//     // … your test logic …
//     return Results.Ok("Logging test completed - check console for structured logs");
// });

// //course test route
// app.MapPost("/api/courses/test", async (ICourseService service) =>
// {
//     // … your test logic …
//     return Results.Ok("Logging test completed - check console for structured logs");
// });
// Test route that throws
app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
});





//m5-exercise 2- add logging and sensitive data to db context step-2
// Seed test data at startup
using (var scope = app.Services.CreateScope())
{
var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
context.Database.Migrate(); // Applies any pending migrations; keeps migration history intact
if (!context.Students.Any())
{
var students = new List<Student>
{
new() { RegistrationNumber = "TMS-2026-0001", Name = "AliceSmith", GPA = 3.8m, IsActive = true },
new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
new() { RegistrationNumber = "TMS-2026-0004", Name = "DianaPrince", GPA = 3.9m, IsActive = true },
new() { RegistrationNumber = "TMS-2026-0005", Name = "EvanWright", GPA = 2.5m, IsActive = true }
};
context.Students.AddRange(students);
var courses = new List<Course>
{
new() { Code = "CS-101", Title = "Introduction to ComputerScience", Capacity = 30 },
new() { Code = "CS-201", Title = "Data Structures and Algorithms", Capacity = 25 },
new() { Code = "MAT-101", Title = "Calculus I", Capacity =40 }
};
context.Courses.AddRange(courses);
context.SaveChanges();
var enrollments = new List<Enrollment>
{
new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
};
context.Enrollments.AddRange(enrollments);
context.SaveChanges();
}
}
app.MapControllers();
app.Run();
public class PaymentOptions
 {
 }