using CleanCrud.API.Middleware;
using CleanCrud.Application.Interfaces;
using CleanCrud.Application.Mappings;
using CleanCrud.Application.Validators;
using CleanCrud.Infrastructure.Data;
using CleanCrud.Infrastructure.Repositories;
using CleanCrud.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        "Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddValidatorsFromAssemblyContaining<StudentDtoValidator>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();



builder.Services.AddSwaggerGen();

//builder.Services.AddSwaggerGen(options =>
//{
//    options.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "CleanCrud API",
//        Version = "v1"
//    });

//    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Name = "Authorization",
//        Type = SecuritySchemeType.Http,
//        Scheme = "bearer",
//        BearerFormat = "JWT",
//        In = ParameterLocation.Header,
//        Description = "Enter JWT Token"
//    });
//});

builder.Services.AddAutoMapper(cfg => { }, typeof(StudentProfile).Assembly);

//builder.Services.AddOpenApi();



builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IFileService, FileService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        //ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}

//if (app.Environment.IsDevelopment())
//{

app.UseSwagger();
app.UseSwaggerUI();


//}

app.UseHttpsRedirection();


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
