using System.Windows;
using System.Runtime.ExceptionServices;
using applanch.Infrastructure.Dialogs;
using Xunit;

namespace applanch.Tests.Infrastructure.Dialogs;

public class UserInteractionServiceTests
{
    [Fact]
    public void Confirm_WhenDialogReturnsTrue_ReturnsTrue()
    {
        RunInSta(() =>
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
        RunInSta(() =>
        {
            var sut = new UserInteractionService((_, _, _) => false);

            var result = sut.Confirm("Delete app?", "Confirmation", new Window());

            Assert.False(result);
        });
    }

    private static void RunInSta(Action action)
    {
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }
    }
}
