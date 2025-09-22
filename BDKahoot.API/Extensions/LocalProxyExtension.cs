using System.Net;

namespace BDKahoot.API.Extensions
{
    public static class LocalProxyExtension
    {
        public static void LocalUseProxy(this WebApplicationBuilder builder)
        {
            if (builder.Environment.EnvironmentName == "Local")
            {
                ICredentials credentials = new NetworkCredential(builder.Configuration["Proxy:Credentials:Username"], builder.Configuration["Proxy:Credentials:Password"]);
                HttpClient.DefaultProxy = new WebProxy(builder.Configuration["Proxy:Address"], false, null, credentials);
            }
        }
    }
}
