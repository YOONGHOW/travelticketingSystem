global using MobileWebAssignment.Models;
global using MobileWebAssignment;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\DB.mdf;
");
builder.Services.AddScoped<Helper>();

builder.Services.AddAuthentication().AddCookie();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();
app.UseHttpsRedirection();//redirect HTTP to HTTPS
app.UseStaticFiles();//enable static files serving from the wwwroot folder
                     //enable default URL rooting following the pattern { controller = Home }
app.MapDefaultControllerRoute();
app.Run();