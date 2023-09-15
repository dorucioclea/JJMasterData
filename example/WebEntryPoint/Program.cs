using JJMasterData.Web.Extensions;
using JJMasterData.Pdf;
using JJMasterData.WebExample.Authorization;

var builder = WebApplication.CreateBuilder(args);

var root = Path.GetFullPath(Path.Join(builder.Environment.ContentRootPath, "..", ".."));
var settingsPath = Path.Combine(root, "appsettings.json");
builder.Configuration.AddJsonFile(settingsPath, optional: false, reloadOnChange: true);
builder.Services.AddControllersWithViews().AddViewLocalization();
builder.Services.AddJJMasterDataWeb(settingsPath)
    .WithPdfExportation();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MasterDataPolicy", policy =>
    {
        policy.AddRequirements(new AllowAnonymousAuthorizationRequirement());
    });
});
        
var app = builder.Build();
if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseExceptionHandler("/Error");
    app.UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}");
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseJJMasterDataWeb();
app.MapJJMasterData()
    .RequireAuthorization("MasterDataPolicy");
app.Run();
