var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
       .AddInteractiveServerComponents();

// Mendaftarkan Pipa Antrean
builder.Services.AddSingleton<RadiologiAsyncAPI.Services.AntreanService>();

// Mendaftarkan Pekerja Latar Belakang (Worker)
builder.Services.AddHostedService<RadiologiAsyncAPI.Services.MedGemmaWorker>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

app.UseAntiforgery();

app.MapRazorComponents<RadiologiAsyncAPI.App>() // Kita akan buat file App.razor ini setelah ini
   .AddInteractiveServerRenderMode();

app.Run();
