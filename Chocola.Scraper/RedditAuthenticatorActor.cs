namespace Chocola.Scraper
{
    using System;

    using Akka.Actor;
    using Akka.Event;

    using Newtonsoft.Json;

    using RestSharp;
    using RestSharp.Authenticators;

    public class RedditAuthenticatorActor : ReceiveActor
    {
        private readonly ILoggingAdapter log = Logging.GetLogger(Context);
        private readonly RedditSettings settings;

        public RedditAuthenticatorActor(RedditSettings settings)
        {
            this.settings = settings;
            this.Receive<AuthenticateMessage>(message =>
            {
                this.log.Info("Authentication request received.");
                var request = new RestRequest();
                request.Method = Method.POST;
                request.Resource = "/api/v1/access_token";
                request.AddParameter("grant_type", "client_credentials");
                var client = new RestClient();
                client.Authenticator = new HttpBasicAuthenticator(this.settings.ClientId, this.settings.ClientSecret);
                client.BaseUrl = new Uri("https://www.reddit.com");
                var response = client.Execute<AccessTokenResponse>(request);
                this.Sender.Tell(new AuthenticationSuccessfulMessage(response.Data.AccessToken, response.Data.ExpiresIn));
                return true;
            });
        }

        public static Props GetProps(RedditSettings settings)
        {
            return Props.Create<RedditAuthenticatorActor>(settings);
        }

        public class AuthenticateMessage
        {
        }

        public class AuthenticationSuccessfulMessage
        {
            public AuthenticationSuccessfulMessage(string accessToken, int expiresIn)
            {
                this.AccessToken = accessToken;
                this.ExpiresIn = expiresIn;
            }

            public string AccessToken { get; }
            public int ExpiresIn { get; }
        }

        private class AccessTokenResponse
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("scope")]
            public string Scope { get; set; }
        }
    }
}
