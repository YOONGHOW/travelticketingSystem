global using MobileWebAssignment.Models;
global using Demo;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\DB.mdf;
");
builder.Services.AddScoped<Helper>();

var app = builder.Build();
app.UseHttpsRedirection();//redirect HTTP to HTTPS
app.UseStaticFiles();//enable static files serving from the wwwroot folder
                     //enable default URL rooting following the pattern { controller = Home }
app.MapDefaultControllerRoute();
app.Run();