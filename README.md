# Building an open simple CRUD API then applying security best practices as layers
## Endpoints:
- GET
	/api/Students/All
- GET
	/api/Students/Passed
- GET
	/api/Students/Avg
- GET
	/api/Students/{id}
- DELETE
	/api/Students/{id}
- PUT
	/api/Students/{id}
- POST
	/api/Students
## Security Layers
### First: HTTPS & CORS
#### HTTPS:
For HTTPS it's all about configuration:
`app.UseHttpsRedirection();` must be in program.cs to allow redirection from http request to https
#### CORS:
- Step 1: Create a named policy
	- Before `builder.Build()`:
`builder.Services.AddCors(options =>
{
    options.AddPolicy("StudentApiCorsPolicy", policy =>
    {
        policy
            .WithOrigins(
                "https://example.com",
                "http://example.com"
            )//What to allow
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});`
   ** Wildcard for allowing subdomains can be used but dangerous for security reasons:
    `policy.WithOrigins("https://*.example.com").SetIsOriginAllowedToAllowWildcardSubdomains();`
   ** Credentials can be included with: `.AllowCredentials();`
  
 - Step 2: Use the named CORS policy:
    - Before `app.MapControllers()`:
    `app.UseCors("StudentApiCorsPolicy")`
   ** Note: This will apply cors globally on all endpoints if Named Policy is used
 - Or by attributes the named policy can be assigned to a specific endpoint
    `[EnableCors("Policy1")]`
 ** `app.UseCors();` in program.cs
   
### Second: Authentication and JWT

#### Authentication:
- Fields required:
    Email, Password Hash, and Role
** BCrypt handles hashing and automatic salt generation
- Separate CRUD operations from authentication (design best practice)
Project Structure for now:
  StudentApi
		Controllers
			StudentsController.cs   (student CRUD)
			AuthController.cs       (authentication)
  		Models
		 	Student.cs
		 	LoginRequest.cs
  		DataSimulation
			StudentDataSimulation.cs
  Program.cs

  
#### JWT

##### Creation
- Packages needed:
`using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;`
- Create claims that represent the authenticated user's identity
`var claims = new[]
            {
                // Unique identifier for the student
                new Claim(ClaimTypes.NameIdentifier, student.Id.ToString()),`
- Create the security key used to sign the JWT
- Define the signing credentials
`var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);`
- Create the JWT token.
     The token includes issuer, audience, claims, expiration, and signature.
- Return the serialized JWT token to the client
##### Verification (Middleware)
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- in program.cs
- Authentication Configuration (Must be before authorization):
- ```csharp
	builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer
	(options =>
    {
        // TokenValidationParameters define how incoming JWTs will be validated.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Ensures the token was issued by a trusted issuer.
            ValidateIssuer = true,
  		
            // Ensures the token is intended for this API (audience check).
            ValidateAudience = true,


            // Ensures the token has not expired.
            ValidateLifetime = true,


            // Ensures the token signature is valid and was signed by the API.
            ValidateIssuerSigningKey = true,


            // The expected issuer value (must match the issuer used when creating the JWT).
            ValidIssuer = "StudentApi",


            // The expected audience value (must match the audience used when creating the JWT).
            ValidAudience = "StudentApiUsers",


            // The secret key used to validate the JWT signature.
            // This must be the same key used when generating the token.
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("THIS_IS_A_VERY_SECRET_KEY_123456"))
        };
		});
	```
 -  `This enables attributes like [Authorize] and role-based authorization.
builder.Services.AddAuthorization();`
 - `[Authorize] attribute if defined globally it enforces jwt on all endpoints`
 - Configure Swagger to test api with jwt token:
 - in program.cs: `using Microsoft.OpenApi.Models`
 - Register Swagger generator and customize its behavior:
```csharp
builder.Services.AddSwaggerGen(options =>{
	// ===============================
    // 1) Define the JWT Bearer security scheme
    // ===============================
    //
    // This tells Swagger that our API uses JWT Bearer authentication
    // through the HTTP Authorization header.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        // The name of the HTTP header where the token will be sent.
        Name = "Authorization",
		
        // Indicates this is an HTTP authentication scheme.
        Type = SecuritySchemeType.Http,


        // Specifies the authentication scheme name.
        // Must be exactly "Bearer" for JWT Bearer tokens.
        Scheme = "Bearer",


        // Optional metadata to describe the token format.
        BearerFormat = "JWT",


        // Specifies that the token is sent in the request header.
        In = ParameterLocation.Header,


        // Text shown in Swagger UI to guide the user.
        Description = "Enter: Bearer {your JWT token}"
		});
		// ===============================
		// 2) Require the Bearer scheme for secured endpoints
		// ===============================
		//
		// This tells Swagger that endpoints protected by [Authorize]
		// require the Bearer token defined above.
		options.AddSecurityRequirement(new OpenApiSecurityRequirement
		{
        {
            new OpenApiSecurityScheme
            {
                // Reference the previously defined "Bearer" security scheme.
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },


            // No scopes are required for JWT Bearer authentication.
            // This array is empty because JWT does not use OAuth scopes here.
            new string[] {}
        }
		});
		});
```

### Third: Authorization

- Step 1: Identify Public Endpoints
    - GET /api/Students/Passed
    - GET /api/Students/AverageGrade

    ** By adding `[AllowAnonymous]` attribute

- Step 2: `[Authorize] Globally for the controller --Already done for JWT`

- Step 3: Restrict Admin Only Endpoints
    - GET /api/Students/All
    - POST /api/Students
    - PUT /api/Students/{id}
    - DELETE /api/Students/{id}

    ** By adding `[Authorize(Roles = "Admin")]` attribute

    #### Owenership

    Authorization step earlier only classified endpoints as public or for admins, how about an authenticated student
    who is trying to access other student/s information (horizontal privilege escalation)
    
    - Step 1: Extracting claims defined in the jwt (role & id)
    - Step 2: Allowing only admins and the owner of the information to access endpoints that return info of a certain student
    ```
            var userID = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.FindFirstValue(ClaimTypes.Role);
    ```
    `User` represents the authenticated user who made the request

    #### Policy-based authorization

    For a more structured and modern way to implement authorization rules on each endpoint
    define a rule once and apply it through the authorization system
    `AuthorizeAsync(User, id, "StudentOwnerOrAdmin")`
    ##### How the evaluation actually works:
    - Controller receives the request
    - Controller knows the resource ID (id)
    - Controller asks the authorization system
    - ASP.NET Core then:
    Finds the policy
    Finds the requirement
    Finds the handler
    Passes the id to the handler
    Makes the final authorization decision
    ##### Implementation
- Create a new folder Authorization:
    This class represents the authorization rule itself.
    It does NOT contain logic it simply defines the requirement "Owner OR Admin can access the student resource".
    Inherits from IAuthorizationRequirement
    - StudentOwnerOrAdminRequirement.cs
    This authorization handler enforces the ownership rule for student resources.
    Inherits from AuthorizationHandler<StudentOwnerOrAdminRequirement, int>
    - StudentOwnerOrAdminHandler.cs
 - Register the handler in `program.cs`:
    `builder.Services.AddSingleton<IAuthorizationHandler, StudentOwnerOrAdminHandler>();`
 - Register the policy in `program.cs`:
```
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StudentOwnerOrAdmin", policy =>
        policy.Requirements.Add(new StudentOwnerOrAdminRequirement()));
});
```
 - Use the policy in the endpoint:
```
[HttpGet("{id}", Name = "GetStudentById")]
public async Task<ActionResult<Student>> GetStudentById(
    int id,
    [FromServices] IAuthorizationService authorizationService)
{
    if (id < 1)
        return BadRequest("Invalid student id.");

    var student = StudentDataSimulation.StudentsList
        .FirstOrDefault(s => s.Id == id);

    if (student == null)
        return NotFound("Student not found.");

    var authResult = await authorizationService.AuthorizeAsync(
        User,
        id,
        "StudentOwnerOrAdmin");

    if (!authResult.Succeeded)
        return Forbid(); // 403

    return Ok(student);
}
```
### Fifth: Refresh tokens
- Step 1: Add Auth DTOs
    TokenResponse.cs
    RefreshRequest.cs
    LogoutRequest.cs
    LoginRequest.cs
- Step 2: Update Student Model (Store Refresh Token State)
```
public string RefreshTokenHash { get; set; }
public DateTime? RefreshTokenExpiresAt { get; set; }
public DateTime? RefreshTokenRevokedAt { get; set; }
```
- Step 3: Add Refresh Token Generator (Secure Random)
in `authController.cs`
```
using System.Security.Cryptography;

private static string GenerateRefreshToken()
{
    var bytes = new byte[64];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(bytes);
    return Convert.ToBase64String(bytes);
}
```
- Step 4: Update Login Endpoint (Return Both Tokens)
After verifying credentials and generating the jwt:
```
var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

// Create refresh token (random)
var refreshToken = GenerateRefreshToken();

// Store refresh token securely (hash + expiry + not revoked)
student.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
student.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
student.RefreshTokenRevokedAt = null;

return Ok(new TokenResponse
{
    AccessToken = accessToken,
    RefreshToken = refreshToken
});
```
 - Step 5: Add Refresh Endpoint (Rotation)
 ```
 [HttpPost("refresh")]
public IActionResult Refresh([FromBody] RefreshRequest request)
{
    var student = StudentDataSimulation.StudentsList
        .FirstOrDefault(s => s.Email == request.Email);

    if (student == null)
        return Unauthorized("Invalid refresh request");

    if (student.RefreshTokenRevokedAt != null)
        return Unauthorized("Refresh token is revoked");

    if (student.RefreshTokenExpiresAt == null || student.RefreshTokenExpiresAt <= DateTime.UtcNow)
        return Unauthorized("Refresh token expired");

    bool refreshValid = BCrypt.Net.BCrypt.Verify(request.RefreshToken, student.RefreshTokenHash);
    if (!refreshValid)
        return Unauthorized("Invalid refresh token");

    // Issue NEW access token (same claims & signing settings as login)
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, student.Id.ToString()),
        new Claim(ClaimTypes.Email, student.Email),
        new Claim(ClaimTypes.Role, student.Role)
    };

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes("THIS_IS_A_VERY_SECRET_KEY_123456"));

    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var jwt = new JwtSecurityToken(
        issuer: "StudentApi",
        audience: "StudentApiUsers",
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(30),
        signingCredentials: creds
    );

    var newAccessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

    // Rotation: replace refresh token
    var newRefreshToken = GenerateRefreshToken();
    student.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
    student.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
    student.RefreshTokenRevokedAt = null;

    return Ok(new TokenResponse
    {
        AccessToken = newAccessToken,
        RefreshToken = newRefreshToken
    });
}
 ```
 - Step 6: Add Logout Endpoint (Revoke Refresh Token)
 ```
 [HttpPost("logout")]
public IActionResult Logout([FromBody] LogoutRequest request)
{
    var student = StudentDataSimulation.StudentsList
        .FirstOrDefault(s => s.Email == request.Email);

    if (student == null)
        return Ok(); // Do not reveal if user exists

    bool refreshValid = BCrypt.Net.BCrypt.Verify(request.RefreshToken, student.RefreshTokenHash);
    if (!refreshValid)
        return Ok();

    student.RefreshTokenRevokedAt = DateTime.UtcNow;
    return Ok("Logged out successfully");
}

 ```
 ### Sixth: Rate Limiting
 - Step 1: 
 ```
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
 ```
 - Step 2: Register Rate Limiting Service (DI)
 in `program.cs` before `builder.Services.AddControllers();`:
 ```
 builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("AuthLimiter", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});
//  5 requests per minute per IP
//  No queueing for login attempts
//  Exceeding limit → 429 automatically
 ```
 - Step 3: Add Rate Limiting Middleware
 before authentication and authorization
 `app.UseRateLimiter();`
 - Step 4: Apply Rate Limit Policy to Login and Refresh Endpoints
 `[EnableRateLimiting("AuthLimiter")]`
 - Step 5: Optional Message (Without Exposing The Limits)
 after `app.UseRateLimiter();`:
 ```
 app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
    {
        await context.Response.WriteAsync("Too many login attempts. Please try again later.");
    }
});
 ```
 ### Seventh: Logging
 ASP.NET Core already provides a secure logging abstraction `ILogger<T>`
 - Step 1: Inject Logger into Controller
 ```
 private readonly ILogger<AuthController> _logger;

public AuthController(ILogger<AuthController> logger)
{
    _logger = logger;
}
 ```
 - Step 2: Log Failed and Successful Login Attempts
 ```
         [HttpPost("Login")]
        [EnableRateLimiting("AuthLimiter")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var student = _Students.FirstOrDefault(s=> s.Email == request.Email);
            if(student == null)
            {
                _logger.LogWarning(
                "Failed login attempt (email not found). Email={Email}, IP={IP}",
                request.Email,
                ip
                );

                return Unauthorized("Invalid credentials");
            }

            bool isValidPass = BCrypt.Net.BCrypt.Verify(request.Password, student.PasswordHash);
            if (!isValidPass)
            {
                _logger.LogWarning(
                "Failed login attempt (bad password). Email={Email}, IP={IP}",
                request.Email,
                ip
                );

                return Unauthorized("Invalid credentials");
            }

            //Login is successful => issue claims (JWT payload fields)
            var Claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, student.Id.ToString()),
                new Claim(ClaimTypes.Email, student.Email),
                new Claim(ClaimTypes.Role, student.Role)
            };
            //Payload is created successfully => next is to sign the header and payload
            //Symmetric key will be used to sign
            var Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Shared_Key_To_Verify_Must_Be_>256"));
            //Specify the algorithm that'll sign the header and payload
            var Creds = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256);
            //Create the JWT
            var Token = new JwtSecurityToken(
                issuer: "StudentApi",
                audience: "StudentApiUsers",
                claims: Claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: Creds);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(Token);

            // Create refresh token (random)
            var refreshToken = GenerateRefreshToken();

            // Store refresh token securely (hash + expiry + not revoked)
            student.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
            student.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7);
            student.RefreshTokenRevokedAt = null;


            _logger.LogInformation(
           "Successful login. UserId={UserId}, Email={Email}, IP={IP}",
            student.Id,
            student.Email,
            ip
            );


            return Ok(new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }
 ```
 - Step 3: Log Refresh Token Abuse
 ```
 _logger.LogWarning(
    "Invalid refresh token attempt. Email={Email}, IP={IP}",
    request.Email,
    ip
);
 ```
 - Step 4: Log Forbidden Access (403) Globally
 before `app.MapControllers();` and after authentication and authorization
 ```
 app.Use(async (context, next) =>
{
    await next();


    if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.ToString();


        // Centralized security log for authorization abuse
        app.Logger.LogWarning(
            "Forbidden access. UserId={UserId}, Path={Path}, IP={IP}",
            userId,
            path,
            ip
        );
    }
});
 ```
 - Step 5: Audit Admin Actions (example delete endpoint)
    - Inject Logger
```
private readonly ILogger<StudentsController> _logger;

        public StudentsController(ILogger<StudentsController> logger)
        {
            _logger = logger;
        }
```

    - Delete Endpoint: 

```
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}",Name = nameof(DeleteStudent))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public ActionResult<Student> DeleteStudent(int id)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";


            var student = _Students.FirstOrDefault(s=> s.Id == id);
            if (student == null)
            {
                // Audit: admin attempted to delete a non-existing student
                _logger.LogWarning(
                    "Admin action failed (target not found). AdminId={AdminId}, Action=DeleteStudent, TargetId={TargetId}, IP={IP}",
                    adminId,
                    id,
                    ip
                );

                return NotFound($"Student with ID {id} not found.");
            }
            // If delete throws or fails later, you still have the audit record of the attempt.
            _logger.LogInformation(
            "Admin action started. AdminId={AdminId}, Action=DeleteStudent, TargetId={TargetId}, TargetEmail={TargetEmail}, IP={IP}",
            adminId,
            student.Id,
            student.Email,
            ip
            );

            _Students.Remove(student);
            // Optional log after deletion

            _logger.LogInformation(
            "Admin action succeeded. AdminId={AdminId}, Action=DeleteStudent, TargetId={TargetId}, IP={IP}",
            adminId,
            id,
            ip
            );

            return NoContent();
        }
```
