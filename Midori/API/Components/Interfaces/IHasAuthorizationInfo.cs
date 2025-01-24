namespace Midori.API.Components.Interfaces;

public interface IHasAuthorizationInfo
{
    bool IsAuthorized { get; }
    string AuthorizationError { get; }
}
