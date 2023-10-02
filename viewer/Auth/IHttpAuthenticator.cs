using System.Net.Http;
using System.Threading.Tasks;

namespace viewer.Auth;

public interface IHttpAuthenticator
{
    Task AddAuthenticationAsync(HttpRequestMessage message, string secret);
}
