namespace DailyVitals.Web.Services;

public sealed class LocalLoginSession
{
    public bool IsSignedIn { get; private set; }
    public string? UserName { get; private set; }
    public long? PersonId { get; private set; }

    public void SignIn(string userName, long? personId)
    {
        IsSignedIn = true;
        UserName = userName;
        PersonId = personId;
    }

    public void SignOut()
    {
        IsSignedIn = false;
        UserName = null;
        PersonId = null;
    }
}
