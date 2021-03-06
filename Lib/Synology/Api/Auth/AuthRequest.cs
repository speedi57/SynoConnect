﻿using Synology;
using Synology.Classes;
using Synology.Utilities;
using Synology.Api.Auth.Parameters;
using Synology.Api.Auth.Results;
using Synology.Attributes;
using Synology.Parameters;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace Synology.Api.Auth
{
    /// <inheritdoc cref="MainApiRequest" />
    /// <summary>
    /// </summary>
    [Request("Auth")]
    internal sealed class AuthRequest : MainApiRequest, IAuthRequest
    {
        private string _sessionNumber;

        public AuthRequest(IApi api) : base(api)
        {
        }

        [RequestMethod("login")]
        public async Task<ResultData<IAuthResult>> LoginAsync(LoginParameters parameters = null)
        {
            parameters = parameters ?? new LoginParameters
            {
                Username = Api.Connection.Settings.Username,
                Password = Api.Connection.Settings.Password,
                SessionName = Api.Connection.Settings.SessionName,
            };

            _sessionNumber = parameters.SessionName;

            Api.Connection.Logger.LogDebug($"Logging in as {parameters.Username} for session {_sessionNumber}");

            var result = await this.GetDataAsync<AuthResult>(new SynologyRequestParameters(this)
            {
                Version = 2,
                Additional = parameters
            });

            if (result.Success && !string.IsNullOrWhiteSpace(result.Data?.Sid))
                Api.Connection.SetSid(result.Data?.Sid);

            return ResultData<IAuthResult>.From(result);
        }

        [RequestMethod("logout")]
        public async Task<ResultData> LogoutAsync()
        {
            var parameters = new[]
            {
                new QueryStringParameter("session", _sessionNumber),
            };

            Api.Connection.Logger.LogDebug($"Logging out for session {_sessionNumber}");

            var result = await this.GetDataAsync(new SynologyRequestParameters(this)
            {
                Additional = parameters
            });

            if (result.Success)
                Api.Connection.SetSid(null);

            return result;
        }
    }
}