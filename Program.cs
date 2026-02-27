using Microsoft.EntityFrameworkCore;
using SunatGreApi.Data;
using SunatGreApi.Repositories;
using SunatGreApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=guias_sunat.db"));

// Extraemos la cadena de conexión de SQL Server una única vez
var sqlServerConnectionString = builder.Configuration.GetConnectionString("SqlServerConnection") 
    ?? throw new InvalidOperationException("SqlServerConnection string not found.");

// Inyectamos la cadena directamente al repositorio al registrarlo
builder.Services.AddScoped<ISqlServerRepository>(provider => 
    new SqlServerRepository(sqlServerConnectionString));

builder.Services.AddScoped<IGuiaService, GuiaService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Asegurar que la base de datos sea creada al inicio
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

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
