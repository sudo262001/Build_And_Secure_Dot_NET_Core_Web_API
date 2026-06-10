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
 `StudentApi
		Controllers
			StudentsController.cs   (student CRUD)
			AuthController.cs       (authentication)
  		Models
		 	Student.cs
		 	LoginRequest.cs
  		DataSimulation
			StudentDataSimulation.cs
  Program.cs`
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
- `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer
	(options =>
    {
        // TokenValidationParameters define how incoming JWTs will be validated.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Ensures the token was issued by a trusted issuer.
            ValidateIssuer = true,`
  		
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
		});`
 -  `This enables attributes like [Authorize] and role-based authorization.
builder.Services.AddAuthorization();`
 - `[Authorize] attribute if defined globally it enforces jwt on all endpoints`
 - Configure Swagger to test api with jwt token:
 - in program.cs: `using Microsoft.OpenApi.Models`
 - Register Swagger generator and customize its behavior:
`builder.Services.AddSwaggerGen(options =>{
	// ===============================
    // 1) Define the JWT Bearer security scheme
    // ===============================
    //
    // This tells Swagger that our API uses JWT Bearer authentication
    // through the HTTP Authorization header.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        // The name of the HTTP header where the token will be sent.
        Name = "Authorization",`
		
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
		});`
