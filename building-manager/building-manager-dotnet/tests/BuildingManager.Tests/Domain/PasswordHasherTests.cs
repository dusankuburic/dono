using BuildingManager.Core.Security;
using FluentAssertions;
using Xunit;

namespace BuildingManager.Tests.Domain;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_Then_Verify_Succeeds()
    {
        var hash = PasswordHasher.Hash("S3curePass!");

        PasswordHasher.Verify("S3curePass!", hash).Should().BeTrue();
    }

    [Fact]
    public void Wrong_Password_Fails()
    {
        var hash = PasswordHasher.Hash("S3curePass!");

        PasswordHasher.Verify("S3curePass?", hash).Should().BeFalse();
    }

    [Fact]
    public void Same_Password_Produces_Different_Hashes()
    {
        // Random salts must make hashes unique.
        var first = PasswordHasher.Hash("S3curePass!");
        var second = PasswordHasher.Hash("S3curePass!");

        first.Should().NotBe(second);
        PasswordHasher.Verify("S3curePass!", first).Should().BeTrue();
        PasswordHasher.Verify("S3curePass!", second).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not-a-valid-hash")]
    [InlineData("PBKDF2-SHA256.abc.def.ghi")]
    public void Malformed_Stored_Hashes_Fail_Closed(string? stored)
    {
        PasswordHasher.Verify("anything", stored).Should().BeFalse();
    }

    [Fact]
    public void Empty_Password_Is_Rejected()
    {
        var act = () => PasswordHasher.Hash("");

        act.Should().Throw<ArgumentException>();
    }
}
