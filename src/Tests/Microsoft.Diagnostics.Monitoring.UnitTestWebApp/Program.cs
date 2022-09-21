// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

app.Logger.LogInformation("1");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

app.UseHttpsRedirection();

app.Logger.LogInformation("2");
app.UseStaticFiles();
app.Logger.LogInformation("3");

app.UseRouting();
app.Logger.LogInformation("4");

app.UseAuthorization();
app.Logger.LogInformation("5");

app.MapRazorPages();
app.Logger.LogInformation("6");

app.Run();
app.Logger.LogInformation("7");
