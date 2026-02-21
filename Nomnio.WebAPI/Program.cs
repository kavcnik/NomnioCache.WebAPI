using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Diagnostics;
using Nomnio.WebAPI.Contracts;
using Nomnio.WebAPI.Contracts.DataSources;
using Nomnio.WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("SwaggerPolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IDataSource, InMemoryDataSource>();

builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection(CacheOptions.SectionName));

builder.Host.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();

    silo.AddAzureBlobGrainStorage("cacheStore", options =>
    {
        options.ContainerName = "cache";
        options.BlobServiceClient = new BlobServiceClient(
            builder.Configuration.GetConnectionString("BlobStorage"));
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("SwaggerPolicy");
app.UseHttpsRedirection();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices
            .GetRequiredService<ILogger<Program>>();

        logger.LogError(
            context.Features.Get<IExceptionHandlerFeature>()?.Error,
            "Unhandled exception");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            error = "An unexpected error occurred"
        });
    });
});

app.MapControllers();
app.Run();