using Microsoft.EntityFrameworkCore;
using Dimage.Models;

var builder = WebApplication.CreateBuilder(args);
IWebHostEnvironment env = builder.Environment;
builder.Services.AddControllers().AddNewtonsoftJson();
string path = Path.Combine(env.ContentRootPath, "res");
if (!Directory.Exists(path)) Directory.CreateDirectory(path);
path = Path.Combine(path, "db");
if (!Directory.Exists(path)) Directory.CreateDirectory(path);
path = Path.Combine(path, "dimage.db");
string context = "Filename=" + path;
builder.Services.AddDbContext<SqlContext>(opt => opt.UseSqlite(context));
var app = builder.Build();
app.UseAuthorization();
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();
app.Run();
