using System.Windows;
using applanch.Infrastructure.Dialogs;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Infrastructure.Dialogs;

[Collection("WpfTests")]
public class UserInteractionServiceTests
{
    [Fact]
    public void Confirm_WhenDialogReturnsTrue_ReturnsTrue()
    {
        WpfTestHost.RunInSta(() =>
        {
            string capturedMessage = string.Empty;
            string capturedCaption = string.Empty;
            Window? capturedOwner = null;
            var expectedOwner = new Window();

            var sut = new UserInteractionService((message, caption, owner) =>
            {
                capturedMessage = message;
                capturedCaption = caption;
                capturedOwner = owner;
                return true;
            });

            var result = sut.Confirm("Launch app?", "Confirmation", expectedOwner);

            Assert.True(result);
            Assert.Equal("Launch app?", capturedMessage);
            Assert.Equal("Confirmation", capturedCaption);
            Assert.Same(expectedOwner, capturedOwner);
        });
    }

    [Fact]
    public void Confirm_WhenDialogReturnsFalse_ReturnsFalse()
    {
        WpfTestHost.RunInSta(() =>
        {
            var sut = new UserInteractionService((_, _, _) => false);

            var result = sut.Confirm("Delete app?", "Confirmation", new Window());

            Assert.False(result);
        });
    }

}
