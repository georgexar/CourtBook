using CourtBook.Application.Common.Interfaces;

namespace CourtBook.UnitTests.TestData;

public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; } = Guid.CreateVersion7();

    public bool IsAuthenticated => true;

    public List<string> Roles { get; } = [];

    public bool IsInRole(string role) => Roles.Contains(role);
}