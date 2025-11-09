using Microsoft.EntityFrameworkCore;
using ApiHelpFast.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// registra DbContext com a connection string DefaultConnection e habilita retry para falhas transitórias
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);

var app = builder.Build();



    app.UseSwagger();
    app.UseSwaggerUI();


app.UseRouting();
app.MapControllers();
app.Run();
