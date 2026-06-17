namespace DailyVitals.Web.Services;

public sealed class LocalLoginSession
{
    public bool IsSignedIn { get; private set; }
    public string? UserName { get; private set; }

    public void SignIn(string userName)
    {
        IsSignedIn = true;
        UserName = userName;
    }

    public void SignOut()
    {
        IsSignedIn = false;
        UserName = null;
    }
}
