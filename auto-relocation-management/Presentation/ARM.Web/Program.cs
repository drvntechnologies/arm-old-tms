using Microsoft.AspNetCore.Builder;
using Nop.Web;
using Nop.Web.Framework.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddNopWeb();

var app = builder.Build();

//configure the application HTTP request pipeline
app.ConfigureRequestPipeline();
app.StartEngine();

app.MapControllers();

app.Run();
