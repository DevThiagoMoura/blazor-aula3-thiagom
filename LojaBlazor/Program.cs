using LojaBlazor.Components;
using LojaBlazor.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// A string de conexão vem do appsettings.json (ConnectionStrings:Loja).
var connectionString = builder.Configuration.GetConnectionString("Loja")
    ?? throw new InvalidOperationException("String de conexão 'Loja' não encontrada no appsettings.json.");

// Fábrica de contextos: em Blazor Server cada operação cria um DbContext
// curto (using), em vez de um contexto único vivo durante todo o circuito.
builder.Services.AddDbContextFactory<LojaContext>(options =>
    options.UseMySQL(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
