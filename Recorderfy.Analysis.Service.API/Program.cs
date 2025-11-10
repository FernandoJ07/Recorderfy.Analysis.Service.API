using Microsoft.EntityFrameworkCore;
using Recorderfy.Analisys.Service.BLL.Interfaces;
using Recorderfy.Analisys.Service.BLL.Services;
using Recorderfy.Analisys.Service.DAL.Data;
using Recorderfy.Analisys.Service.DAL.Interfaces;
using Recorderfy.Analisys.Service.DAL.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// PostgreSQL Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// HttpClient Factory
builder.Services.AddHttpClient();

// Repositories
builder.Services.AddScoped<IAnalisisRepository, AnalisisRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();

// Services
builder.Services.AddScoped<IAnalisisService, AnalisisService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();

var app = builder.Build();

app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
