using NUnit.Framework;
using TourPlanner.BusinessLayer.Services.Auth;

namespace TourPlanner.Tests.Auth;

[TestFixture]
public class DefaultPasswordPolicyTests
{
    private DefaultPasswordPolicy _policy = null!;

    [SetUp]
    public void SetUp() => _policy = new DefaultPasswordPolicy();

    [Test]
    public void Validate_NullOrWhitespace_Rejected()
    {
        Assert.That(_policy.Validate(""), Is.EqualTo("Password must not be empty."));
        Assert.That(_policy.Validate("   "), Is.EqualTo("Password must not be empty."));
    }

    [TestCase("short")]
    [TestCase("1234567")]
    public void Validate_TooShort_Rejected(string pwd)
    {
        Assert.That(_policy.Validate(pwd), Does.Contain("at least 8 characters"));
    }

    [TestCase("password")]
    [TestCase("PASSWORD")]
    [TestCase("Password1")]
    [TestCase("12345678")]
    [TestCase("qwerty12")]
    public void Validate_CommonPassword_Rejected(string pwd)
    {
        Assert.That(_policy.Validate(pwd), Does.Contain("commonly-used"));
    }

    [Test]
    public void Validate_PasswordEqualsEmail_Rejected()
    {
        var result = _policy.Validate("HelloAda@example.com", email: "HelloAda@example.com");
        Assert.That(result, Does.Contain("email"));
    }

    [Test]
    public void Validate_PasswordEqualsName_Rejected()
    {
        var result = _policy.Validate("AdaLovelace", name: "AdaLovelace");
        Assert.That(result, Does.Contain("display name"));
    }

    [TestCase("correct horse battery staple")]
    [TestCase("T0ur-Planner-Rocks!")]
    [TestCase("Xk!p9$mQvL2^rZ")]
    public void Validate_StrongPassword_Accepted(string pwd)
    {
        Assert.That(_policy.Validate(pwd, email: "user@example.com", name: "Some User"),
            Is.Null);
    }

    [Test]
    public void Validate_Comparison_IsCaseInsensitiveForCommonList()
    {
        Assert.That(_policy.Validate("PaSSwOrD"), Does.Contain("commonly-used"));
    }
}
