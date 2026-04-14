using applanch.Infrastructure.Dialogs;
using Xunit;

namespace applanch.Tests.Infrastructure.Dialogs;

public class UserInteractionServiceTests
{
    [Fact]
    public void UserInteractionService_DefaultConstructor_CreatesInstance()
    {
        var sut = new UserInteractionService();

        Assert.NotNull(sut);
    }
}
