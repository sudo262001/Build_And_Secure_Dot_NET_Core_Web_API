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
   ** Note: This will apply cors globally on all endpoints
 - Or by attributes the named policy can be assigned to a specific endpoint
    `[EnableCors("Policy1")]`