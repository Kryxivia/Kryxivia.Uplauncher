using KryxiviaUpdater.Core;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KryxiviaUpdater.Updater
{
    public class KryxiviaAPI
    {
        private string _urlApi;
        private string _websiteAuthentification;
        private Task _authPooler;
        private Action<UpdaterState> _updateState;
        private Action<string> _updateAddress;
        private ILog _log;

        public JwtSecurityToken jwtSecurityToken;
        public string jwtRaw;
        public LoginToken LoginToken
        {
            get;
            private set;
        }

        public KryxiviaAPI(ILog log, string urlApi, string websiteAuthentification, Action<UpdaterState> updateState
            ,Action<string> updateAddress)
        {
            _log = log;
            _urlApi = urlApi;
            _websiteAuthentification = websiteAuthentification;
            _updateState = updateState;
            _updateAddress = updateAddress;
            LoginToken = null;
            _authPooler = null;
        }

        public async Task<UpdaterState> Setup()
        {
            if (File.Exists("kryxiviaToken.json"))
            {
                _log.Info("KRYXIVIATOKEN EXIST, WE REED IT !");
                LoginToken = JsonConvert.DeserializeObject<LoginToken>(File.ReadAllText("kryxiviaToken.json"));
            }

            while (LoginToken == null)
            {
                _log.Info("LOGIN TOKEN NULL, TRY TO LOAD NEW TOKEN");
                await LoadNewToken();
            }

            if (LoginToken?.jwtAttached == null)
            {
                _log.Info("JWT ATTACHED ARE NULL, WAITING FOR CONNECTING");
                return UpdaterState.Connecting;
            }
            else
            {
                jwtRaw = LoginToken.jwtAttached;
                jwtSecurityToken = new JwtSecurityToken(LoginToken?.jwtAttached);
                if (DateTime.Now > jwtSecurityToken.ValidTo)
                {
                    _log.Info("TOKEN EXPIRED");
                    LoginToken = null;
                    while (LoginToken == null)
                    {
                        _log.Info("LOGIN TOKEN NULL, TRY TO LOAD NEW TOKEN");
                        await LoadNewToken();
                    }

                    _log.Info("JWT ATTACHED ARE NULL, WAITING FOR CONNECTING");
                    return UpdaterState.Connecting;
                }
                else
                {
                    var publicKey = jwtSecurityToken.Claims.First(x => x.Type == "publickey");
                    _updateAddress(publicKey.Value);
                    _log.Info("TOKEN GOOD, CAN PLAYING");
                    return UpdaterState.Playing;
                }
            }

        }

        public async Task LoadNewToken()
        {
            var tokenApiUrl = $"{_urlApi}api/v1/login/token_auth";
            var result = "";
            using (HttpClient client = new HttpClient())
            {
                result = await client.GetStringAsync(tokenApiUrl);
            }

            LoginToken = JsonConvert.DeserializeObject<LoginToken>(result);
        }

        public async Task<LoginToken> CallBackAuthentification()
        {
            var tokenApiUrl = $"{_urlApi}api/v1/login/token_auth";
            var result = "";
            using (HttpClient client = new HttpClient())
            {
                var playload = new
                {
                    TokenHash =  LoginToken?.tokenHash
                };
                var httpContent = new StringContent(JsonConvert.SerializeObject(playload), Encoding.UTF8, "application/json");
                var httpResponse = await client.PostAsync($"{_urlApi}api/v1/login/token_auth_check", httpContent);
                if(httpResponse != null)
                {
                    result = await httpResponse.Content.ReadAsStringAsync();
                }
            }

            return JsonConvert.DeserializeObject<LoginToken>(result);
        }

        public async void OpenWebSite()
        {
            await LoadNewToken();
            var process = new System.Diagnostics.
                ProcessStartInfo($"{_websiteAuthentification}?auth-token={LoginToken?.tokenHash}")
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(process);
            if(_authPooler == null)
            {
                _authPooler = new Task(async () =>
                {
                    _log.Info("START CALLBACK AUTHENTIFICATION");
                    var callback = await CallBackAuthentification();
                    while(callback == null || callback.jwtAttached == null)
                    {
                        await Task.Delay(1000);
                        callback = await CallBackAuthentification();
                    }
                    _log.Info($"WALLET CONNECTED SUCCESSFULLY");
                    File.WriteAllText("kryxiviaToken.json", JsonConvert.SerializeObject(callback));
                    jwtSecurityToken = new JwtSecurityToken(callback?.jwtAttached);
                    jwtRaw = callback?.jwtAttached;
                    var publicKey = jwtSecurityToken.Claims.First(x => x.Type == "publickey");
                    _updateAddress(publicKey.Value);
                    _log.Info("TOKEN GOOD, CAN PLAYING");
                    _updateState(UpdaterState.Playing);
                    _authPooler = null;
                });
                _authPooler.Start();
            }
        }
    }
}
