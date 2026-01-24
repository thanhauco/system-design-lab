using Sdmp.Monolith.Domain;

namespace Sdmp.Monolith.Features.Users;

public sealed record CreateUserRequest(string Email, string DisplayName);

/// <summary>Users vertical slice: endpoints + contracts for account management.</summary>
public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsers(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users").WithTags("Users");

        group.MapGet("/", async (IRepository<User> repo, CancellationToken ct) =>
                Results.Ok(await repo.ListAsync(ct)))
            .WithName("ListUsers")
            .WithSummary("List all users");

        group.MapGet("/{id:guid}", async (Guid id, IRepository<User> repo, CancellationToken ct) =>
                await repo.GetAsync(id, ct) is { } user ? Results.Ok(user) : Results.NotFound())
            .WithName("GetUser")
            .WithSummary("Get a user by id");

        group.MapPost("/", async (CreateUserRequest req, IRepository<User> repo, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["email"] = ["A valid email is required."]
                    });

                var user = new User(Guid.NewGuid(), req.Email.Trim(), req.DisplayName.Trim(), DateTimeOffset.UtcNow);
                await repo.AddAsync(user, ct);
                return Results.Created($"/api/v1/users/{user.Id}", user);
            })
            .WithName("CreateUser")
            .WithSummary("Create a user");

        return app;
    }
}
