using DeviceManagement.Services.Report;
using DeviceManagementOnly.Auth;
using DeviceManagement.Data;
using DeviceManagementOnly.Hubs;
using DeviceManagementOnly.Middlewares;
using DeviceManagementOnly.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
// Add this using directive for Pomelo.EntityFrameworkCore.MySql
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System.Text;

ExcelPackage.License.SetNonCommercialOrganization("DeviceManagementOnly");

var builder = WebApplication.CreateBuilder(args);

// Kestrel ko khud "Server: Kestrel" header bhejne se rokta hai (source pe hi)
builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);

// ── Database ──────────────────────────────────────────────────────
// SQL Server (default)
//builder.Services.AddDbContext<DBContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("DefaultConnection")));

// SQL Server wali line COMMENT karo
// builder.Services.AddDbContext<DBContext>(options =>
//     options.UseSqlServer(...));

// MySQL wali UNCOMMENT karo
builder.Services.AddDbContext<DBContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection"))));

// To use MySQL instead, replace above with:
// builder.Services.AddDbContext<DBContext>(options =>
//     options.UseMySql(
//         builder.Configuration.GetConnectionString("DefaultConnection"),
//         ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));

// ── Authentication (JWT) ──────────────────────────────────────────────
// Issuer/Audience/Key DemoAuth ke Jwt config ke EXACTLY same hone chahiye,
// tabhi DemoAuth se issue hua token yahan valid maana jaayega.
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
    };

    // ── SignalR websocket handshake me custom Authorization header nahi
    //    bheja ja sakta browser se — isliye connection URL me
    //    ?access_token=... query string se bhi token accept karo,
    //    par SIRF /hubs/** paths ke liye (normal REST APIs pe nahi) ──
    o.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// ── Authorization (permission-based policies, e.g. "perm:Device.View") ─
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

// ── Device Services ───────────────────────────────────────────────
builder.Services.AddScoped<IDeviceCategoryService, DeviceCategoryService>();
builder.Services.AddScoped<IDeviceTypeService, DeviceTypeService>();
builder.Services.AddScoped<IModelCategoryService, ModelCategoryService>();
builder.Services.AddScoped<IModelSpecificationService, ModelSpecificationService>();
builder.Services.AddScoped<IModelParameterService, ModelParameterService>();
builder.Services.AddScoped<IDeviceMasterService, DeviceMasterService>();
builder.Services.AddScoped<IDeviceDetailService, DeviceDetailService>();
builder.Services.AddScoped<IDeviceAssociationService, DeviceAssociationService>();
builder.Services.AddScoped<IDeviceModelService, DeviceModelService>();
builder.Services.AddScoped<IUnitMasterService, UnitMasterService>();
//
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFileStoreService, FileStoreService>();
builder.Services.AddScoped<IDeviceLocationService, DeviceLocationService>();
builder.Services.AddScoped<ICurrentCompanyService, CurrentCompanyService>();
builder.Services.AddScoped<IFloorService, FloorService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IRackService, RackService>();
builder.Services.AddScoped<IRackPowerCapacityService, RackPowerCapacityService>();
builder.Services.AddScoped<IDeviceRackLocationService, DeviceRackLocationService>();
builder.Services.AddScoped<IDeviceConnectionConfigService, DeviceConnectionConfigService>();
builder.Services.AddScoped<INetworkConnectionService, NetworkConnectionService>();


builder.Services.AddScoped<IDocxReportBuilder, DocxReportBuilder>();
builder.Services.AddScoped<IPdfConverterService, PdfConverterService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IReportService, ReportService>();

// ── SignalR (Room layout real-time sync) ────────────────────────────
builder.Services.AddSignalR();

// ── API ───────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Device Management API", Version = "v1" });

    // ✅ Swagger me JWT token test karne ke liye "Authorize" button
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste only the JWT (no 'Bearer ' prefix)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();



// ── Global exception handler — koi bhi unhandled exception ab blank 500 ki jagah
// proper JSON error dega, aur poora stack trace bhi log ho jaayega ──
app.UseExceptionHandler(errorApp =>
{
errorApp.Run(async context =>
{
context.Response.StatusCode = StatusCodes.Status500InternalServerError;
context.Response.ContentType = "application/json";

var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
var ex = exceptionHandlerPathFeature?.Error;

var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
logger.LogError(ex, "Unhandled exception on {Path}", exceptionHandlerPathFeature?.Path);

var response = new
{
error = "An unexpected error occurred.",
// Sirf Development me actual exception message bhejo — Production me hide rakho (security ke liye)
detail = app.Environment.IsDevelopment() ? ex?.Message : null,
path = exceptionHandlerPathFeature?.Path
};

await context.Response.WriteAsJsonAsync(response);
});
});

// ── Security headers sabse pehle — har response (errors bhi) cover ho ──
//app.UseSecurityHeaders();

// ── Security headers sabse pehle — har response (errors bhi) cover ho ──
//app.UseSecurityHeaders();

//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseStaticFiles();

// ── Uploads folder serve karo ─────────────────────────────────────────
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
    Directory.CreateDirectory(uploadsPath);
app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePages();
app.MapControllers();
app.MapHub<RoomLayoutHub>("/hubs/room-layout");

// ── Auto-migrate: DB na ho toh bana bhi dega, pending migrations bhi apply karega ──
using (var scope = app.Services.CreateScope())
{
    // NOTE: DBContext resolve karte hi UseMySql(...) ke andar ServerVersion.AutoDetect()
    // turant MySQL se eagerly connect karta hai — isliye ye bhi try ke ANDAR honi chahiye,
    // warna MySQL down hone par app hi crash ho jaayegi (unhandled exception).
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("──────────────── Database Startup Check ────────────────");

        var db = scope.ServiceProvider.GetRequiredService<DBContext>();

        // DB pehle se exist karta hai ya nahi, ye check karke rakh lo (sirf logging ke liye)
        bool dbExistedBefore = await db.Database.CanConnectAsync();
        logger.LogInformation("Database already exists: {Exists}", dbExistedBefore);

        var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToList();

        if (dbExistedBefore && pendingMigrations.Count == 0)
        {
            logger.LogInformation(" Database is already up to date. No pending migrations found.");
        }
        else
        {
            if (!dbExistedBefore)
                logger.LogInformation("⏳ Database does not exist — creating it and applying all migrations...");
            else
                logger.LogInformation(
                    " {Count} pending migration(s) found, applying now: {Migrations}",
                    pendingMigrations.Count, string.Join(", ", pendingMigrations));

            // MigrateAsync() khud DB create kar deta hai agar exist nahi karta,
            // aur sirf missing migrations apply karta hai agar DB already hai.
            await db.Database.MigrateAsync();

            logger.LogInformation(
                !dbExistedBefore
                    ? "✅ Database created successfully and migrations applied: {Migrations}"
                    : "✅ Pending migrations applied successfully: {Migrations}",
                pendingMigrations.Count == 0 ? "(none)" : string.Join(", ", pendingMigrations));
        }

        // Ab actual schema padh kar dikhao — kitni tables hain, kis table mein kaunse columns
        await LogDatabaseSchemaAsync(db, logger);

        logger.LogInformation("──────────────────────────────────────────────────────────");
    }
    catch (Exception ex)
    {
        // App should not crash if migration fails — just log the error,
        // so an existing running app doesn't go down because of a DB issue.
        logger.LogError(ex, " Database migration FAILED on startup. The database remains in its existing state.");
    }
}

app.Run();

// ── Schema introspection: DB se hi live table/column list padh kar log karta hai ──
static async Task LogDatabaseSchemaAsync(DBContext db, ILogger logger)
{
    var connection = db.Database.GetDbConnection();
    bool shouldClose = connection.State != System.Data.ConnectionState.Open;
    if (shouldClose)
        await connection.OpenAsync();

    try
    {
        var databaseName = connection.Database;

        var tableNames = new List<string>();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText =
                "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @db ORDER BY TABLE_NAME";
            var p = cmd.CreateParameter();
            p.ParameterName = "@db";
            p.Value = databaseName;
            cmd.Parameters.Add(p);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                tableNames.Add(reader.GetString(0));
        }

        logger.LogInformation("📊 Total tables in '{Database}': {Count}", databaseName, tableNames.Count);

        foreach (var table in tableNames)
        {
            var columns = new List<string>();
            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText =
                    "SELECT COLUMN_NAME, COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
                    "WHERE TABLE_SCHEMA = @db AND TABLE_NAME = @t ORDER BY ORDINAL_POSITION";
                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@db";
                p1.Value = databaseName;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@t";
                p2.Value = table;
                cmd.Parameters.Add(p2);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    columns.Add($"{reader.GetString(0)} ({reader.GetString(1)})");
            }

            logger.LogInformation("   • {Table}  →  [{Columns}]", table, string.Join(", ", columns));
        }
    }
    finally
    {
        if (shouldClose)
            await connection.CloseAsync();
    }
}