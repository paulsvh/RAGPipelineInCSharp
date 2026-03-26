using DotNetRAG.Api.Endpoints;
using DotNetRAG.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRagPipeline(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapIngestionEndpoints();
app.MapQueryEndpoints();
app.MapDiagnosticEndpoints();

app.Run();

public partial class Program;
