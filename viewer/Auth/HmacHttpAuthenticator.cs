using Microsoft.Net.Http.Headers;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System;
using viewer.Shared;

namespace viewer.Auth;

public class HmacHttpAuthenticator : IHttpAuthenticator
{
    private byte[] _secret;

    public virtual string AuthorizationKey => Shared.HeaderNames.MsAuthorization;
    public virtual string SignatureKey => Shared.HeaderNames.Signature;
    public virtual string DateKey => Shared.HeaderNames.MsDate;
    public virtual string HostKey => Shared.HeaderNames.MsHost;

    public HmacHttpAuthenticator()
    {
    }

    public virtual async Task AddAuthenticationAsync(HttpRequestMessage message, string secret)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentNullException(nameof(secret));
        }

        _secret = Convert.FromBase64String(secret);

        var date = GetDate().ToString("r", CultureInfo.InvariantCulture);
        var authority = GetAuthority(message.RequestUri);

        string content = string.Empty;
        if (message.Content != null)
        {
            content = await ReadAsStringWithResetAsync(message.Content);
        }

        var contentHash = ComputeContentHash(content);

        var hmacHeader = GetAuthorizationHeader(
            message.Method,
            message.RequestUri,
            authority,
            contentHash,
            date);

        message.Headers.Add(AuthorizationKey, hmacHeader);
        message.Headers.Add(SignatureKey, contentHash);
        message.Headers.Add(DateKey, date);
        message.Headers.Add(HostKey, authority);
    }

    protected virtual DateTimeOffset GetDate()
        => DateTimeOffset.UtcNow;

    protected virtual string GetAuthority(Uri requestUri)
        => requestUri?.Authority;

    protected virtual string GetAuthorizationHeader(HttpMethod method, Uri uri, string authority, string contentHash, string date)
    {
        // null check method and uri
        if (uri == null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (method == null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        var pathAndQuery = uri.IsAbsoluteUri ? uri.PathAndQuery : $"/{uri.OriginalString}";

        var phrase = $"{method.Method}\n{pathAndQuery}\n{date};{authority};{contentHash}";
        var hash = ComputesSignature(phrase);

        return $"HMAC-SHA256 SignedHeaders={DateKey};{AuthorizationKey};{SignatureKey}&Signature={hash}";
    }

    protected virtual string ComputesSignature(string phrase)
    {
        using (var hmacsha256 = new HMACSHA256(_secret))
        {
            var bytes = Encoding.ASCII.GetBytes(phrase);
            var hashedBytes = hmacsha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashedBytes);
        }
    }

    protected virtual string ComputeContentHash(string rawData)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(rawData);
            byte[] hashedBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public async Task<string> ReadAsStringWithResetAsync(HttpContent content)
    {
        await content.LoadIntoBufferAsync();
        var response = await content.ReadAsStringAsync();
        await ResetAsync(content);
        return response;
    }

    public async Task ResetAsync(HttpContent content)
    {
        var stream = await content.ReadAsStreamAsync();
        stream.Position = 0;
    }
}
