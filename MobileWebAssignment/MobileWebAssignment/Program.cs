global using MobileWebAssignment.Models;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSession();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\DB.mdf;
");

var app = builder.Build();
app.UseSession();// add Session
app.UseHttpsRedirection();//redirect HTTP to HTTPS
app.UseStaticFiles();//enable static files serving from the wwwroot folder
                     //enable default URL rooting following the pattern { controller = Home }
app.MapDefaultControllerRoute();
app.Run();