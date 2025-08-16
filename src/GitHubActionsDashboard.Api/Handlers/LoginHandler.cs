﻿namespace GitHubActionsDashboard.Api.Handlers;

public static class LoginHandler
{
    public static IResult Handle(HttpContext http, IConfiguration configuration)
    {
        string clientId = configuration.GetValue<string>("ClientId") ?? throw new InvalidOperationException("ClientId is missing");
        string redirectUri = configuration.GetValue<string>("RedirectUri") ?? throw new InvalidOperationException("RedirectUri is missing");

        var state = http.Session.GetString("oauth_state");
        if (String.IsNullOrEmpty(state))
        {
            state = Guid.NewGuid().ToString("N");
            http.Session.SetString("oauth_state", state);
        }

        var url = new UriBuilder("https://github.com/login/oauth/authorize");
        var query = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["state"] = state,
            ["scope"] = "read:user repo"
        };
        url.Query = String.Join('&', query.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));

        return Results.Redirect(url.ToString());
    }
}
