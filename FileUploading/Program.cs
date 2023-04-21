using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Features;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 524288000;
});
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit= 524288000
);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();
app.MapPost("/uploadFile", async (HttpContext context) => {
    string[] permittedExtensions = { ".pdf", ".bin" };
    //if (!context.Request.HasFormContentType)
    //{
    //    context.Response.StatusCode = StatusCodes.Status400BadRequest;
    //    await context.Response.WriteAsync("Expected a multipart request");
    //    return Results.BadRequest("expected a multipart request");
    //}
    Console.WriteLine("Body:", context.Request.Body);
    var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue
        .Parse(context.Request.ContentType).Boundary).Value;
    var reader = new MultipartReader(boundary, context.Request.Body);
    var section = await reader.ReadNextSectionAsync();
    while (section != null)
    {
        if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition))
        {
            var fileExt = Path.GetExtension(contentDisposition.FileName.Value);
            Console.WriteLine(fileExt);
            if (string.IsNullOrEmpty(fileExt) || !permittedExtensions.Contains(fileExt) || !permittedExtensions.Contains(fileExt.ToLower()))
            {
                return Results.BadRequest("Invalid Extension");
            }
            if (contentDisposition.FileName.HasValue)
            {
                var fileName = contentDisposition.FileName.Value;
                Console.WriteLine(fileName);
                var fn2 = Path.GetRandomFileName();
                using (var stream = new FileStream(Path.Combine(Path.GetTempPath(),fn2), FileMode.Create))
                {
                    await section.Body.CopyToAsync(stream);
                    await stream.FlushAsync();
                }
            }
        }
        
        section = await reader.ReadNextSectionAsync();
    }
    return Results.Ok("File uploaded successfully.");
});

app.MapPost("/uploadIFormFile", async (IFormFile file) =>
{
    //Console.WriteLine(file.FileName); 
    var filename = Path.GetTempFileName();
    Console.WriteLine(filename);
    using(var stream = File.Create(filename))
    {
        await file.CopyToAsync(stream);
    }
    return Results.Ok("File uploaded "+file.Length);
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
