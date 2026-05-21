using GestionPortefeuille.Infrastructure.Data;
using GestionPortefeuille.Services;
using GestionPortefeuille.Web.Components;
using GestionPortefeuille.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddPortfolioServices(builder.Configuration);
builder.Services.AddScoped<ToastService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await DbInitializer.SeedAsync(db);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Echec de la migration ou de l'initialisation de la base de donnees");
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

app.Run();
