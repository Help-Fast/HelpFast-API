using ApiHelpFast.Data;
using ApiHelpFast.Models;
using ApiHelpFast.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// registra DbContext com a connection string Default e habilita retry para falhas transit�rias
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);

builder.Services.Configure<GoogleDriveOptions>(builder.Configuration.GetSection("GoogleDrive"));
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection("OpenAI"));

builder.Services.AddSingleton<IGoogleDriveService, GoogleDriveService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddHttpClient<IOpenAIService, OpenAIService>();

// add authorization services so [Authorize] attributes work
builder.Services.AddAuthorization();

var app = builder.Build();



    app.UseSwagger();
    app.UseSwaggerUI();


app.UseRouting();

// enable authorization middleware required by endpoints with [Authorize]
app.UseAuthorization();

app.MapControllers();
app.Run();
