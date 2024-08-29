using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// ---  ivma doc:
// AddIdentityCore() ..adds this endpoints
//   /register
//   /login
//   /refresh
//   /confirmEmail
//   /resendConfirmationEmail
//   /forgotPassword
//   /resetPassword
//   /manage/2fa
//   /manage/info
//   /manage/info
//   /logout
// 
// AddIdentity() .. macht nicht mehr aber man kann noch das Model IdentityRole mitgeben 
// 
// roles und claims endpoints aber selber bauen
//
// ---- folgendes erstellt die Datenbank ... script siehe weiter unten
// Install-Package Microsoft.EntityFrameworkCore.Tools
// Add-Migration InitialCreate
// Update-Database


builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies()
    .ApplicationCookie!.Configure(opt => opt.Events = new CookieAuthenticationEvents()
    {
        OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        }
    });
builder.Services.AddAuthorizationBuilder();

// builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("AppDb"));
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("default")));

// there are 3: AddDefaultIdentity (adds razor UI), AddIdentityCore, AddIdentity  https://stackoverflow.com/questions/55361533/addidentity-vs-addidentitycore 
//builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)  ... comes from other apporach, when: dotnet new angular -au Individual
//builder.Services.AddIdentity<MyUser, IdentityRole>()
builder.Services.AddIdentityCore<MyUser>()
    .AddRoles<IdentityRole>() // optional 
    .AddEntityFrameworkStores<AppDbContext>()
    .AddApiEndpoints();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapIdentityApi<MyUser>();
app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// protection from cross-site request forgery (CSRF/XSRF) attacks with empty body
// form can't post anything useful so the body is null, the JSON call can pass
// an empty object {} but doesn't allow cross-site due to CORS.
app.MapPost("/logout", async (
    SignInManager<MyUser> signInManager,
    [FromBody] object empty) =>
{
    if (empty is not null)
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    }
    return Results.NotFound();
}).RequireAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi()
.RequireAuthorization();

/*
//--------custom Roles--------------->>>
app.MapPost("/roles", async (RoleManager<IdentityRole> roleManager, [FromBody] string roleName) =>
{
    if (string.IsNullOrWhiteSpace(roleName))
    {
        return Results.BadRequest("Role name cannot be empty.");
    }

    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
    if (result.Succeeded)
    {
        return Results.Ok();
    }

    return Results.BadRequest(result.Errors);
}).RequireAuthorization();

app.MapGet("/roles", async (RoleManager<IdentityRole> roleManager) =>
{
    var roles = await roleManager.Roles.ToListAsync();
    return Results.Ok(roles);
}); //.RequireAuthorization();
//-----------------------<<<

//-------custom UserRoles---------------->>>
// Assign role to user
app.MapPost("/users/{userId}/roles", async (UserManager<MyUser> userManager, RoleManager<IdentityRole> roleManager, string userId, [FromBody] string roleName) =>
{
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    var roleExists = await roleManager.RoleExistsAsync(roleName);
    if (!roleExists)
    {
        return Results.BadRequest("Role does not exist.");
    }

    var result = await userManager.AddToRoleAsync(user, roleName);
    if (result.Succeeded)
    {
        return Results.Ok();
    }

    return Results.BadRequest(result.Errors);
}).RequireAuthorization();

// Remove role from user
app.MapDelete("/users/{userId}/roles", async (UserManager<MyUser> userManager, string userId, [FromBody] string roleName) =>
{
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    var result = await userManager.RemoveFromRoleAsync(user, roleName);
    if (result.Succeeded)
    {
        return Results.Ok();
    }

    return Results.BadRequest(result.Errors);
}).RequireAuthorization();

// List user roles
app.MapGet("/users/{userId}/roles", async (UserManager<MyUser> userManager, string userId) =>
{
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    var roles = await userManager.GetRolesAsync(user);
    return Results.Ok(roles);
}).RequireAuthorization();
//-----------------------<<<

//-------custom Claims---------------->>>
app.MapPost("/users/{userId}/claims", async (UserManager<MyUser> userManager, string userId, [FromBody] Claim claim) =>
{
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    var result = await userManager.AddClaimAsync(user, claim);
    if (result.Succeeded)
    {
        return Results.Ok();
    }

    return Results.BadRequest(result.Errors);
}).RequireAuthorization();

app.MapGet("/users/{userId}/claims", async (UserManager<MyUser> userManager, string userId) =>
{
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    var claims = await userManager.GetClaimsAsync(user);
    return Results.Ok(claims);
});//.RequireAuthorization();
//-----------------------<<<


// Check if user is in a specific role
app.MapGet("/users/{userId}/roles/{roleName}", async (UserManager<MyUser> userManager, string userId, string roleName) =>
{
    var user = await userManager.FindByIdAsync(userId);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    var isInRole = await userManager.IsInRoleAsync(user, roleName);
    if (isInRole)
    {
        return Results.Ok(true);
    }

    return Results.Ok(false);
}).RequireAuthorization();
*/

app.MapFallbackToFile("/index.html");

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

class MyUser : IdentityUser { }

class AppDbContext(DbContextOptions<AppDbContext> options) :
    IdentityDbContext<MyUser>(options)
{
}




//-------------------

/*
      CREATE TABLE [AspNetRoles] (
          [Id] nvarchar(450) NOT NULL,
          [Name] nvarchar(256) NULL,
          [NormalizedName] nvarchar(256) NULL,
          [ConcurrencyStamp] nvarchar(max) NULL,
          CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
      );
Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (3ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE [AspNetUsers] (
          [Id] nvarchar(450) NOT NULL,
          [UserName] nvarchar(256) NULL,
          [NormalizedUserName] nvarchar(256) NULL,
          [Email] nvarchar(256) NULL,
          [NormalizedEmail] nvarchar(256) NULL,
          [EmailConfirmed] bit NOT NULL,
          [PasswordHash] nvarchar(max) NULL,
          [SecurityStamp] nvarchar(max) NULL,
          [ConcurrencyStamp] nvarchar(max) NULL,
          [PhoneNumber] nvarchar(max) NULL,
          [PhoneNumberConfirmed] bit NOT NULL,
          [TwoFactorEnabled] bit NOT NULL,
          [LockoutEnd] datetimeoffset NULL,
          [LockoutEnabled] bit NOT NULL,
          [AccessFailedCount] int NOT NULL,
          CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
      );
Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (3ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE [AspNetRoleClaims] (
          [Id] int NOT NULL IDENTITY,
          [RoleId] nvarchar(450) NOT NULL,
          [ClaimType] nvarchar(max) NULL,
          [ClaimValue] nvarchar(max) NULL,
          CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
          CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
      );
Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (2ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE [AspNetUserClaims] (
          [Id] int NOT NULL IDENTITY,
          [UserId] nvarchar(450) NOT NULL,
          [ClaimType] nvarchar(max) NULL,
          [ClaimValue] nvarchar(max) NULL,
          CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
          CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
      );
Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (3ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE [AspNetUserLogins] (
          [LoginProvider] nvarchar(450) NOT NULL,
          [ProviderKey] nvarchar(450) NOT NULL,
          [ProviderDisplayName] nvarchar(max) NULL,
          [UserId] nvarchar(450) NOT NULL,
          CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
          CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
      );
Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (15ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE [AspNetUserRoles] (
          [UserId] nvarchar(450) NOT NULL,
          [RoleId] nvarchar(450) NOT NULL,
          CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
          CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
          CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
      );
Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (3ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
      CREATE TABLE [AspNetUserTokens] (
          [UserId] nvarchar(450) NOT NULL,
          [LoginProvider] nvarchar(450) NOT NULL,
          [Name] nvarchar(450) NOT NULL,
          [Value] nvarchar(max) NULL,
          CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
          CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
      );
 */
