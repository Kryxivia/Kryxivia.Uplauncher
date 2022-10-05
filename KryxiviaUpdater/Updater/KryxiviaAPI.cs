using KryxiviaUpdater.Core;
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
        public JwtSecurityToken jwtSecurityToken;
        public string jwtRaw;
        public LoginToken LoginToken
        {
            get;
            private set;
        }

        public KryxiviaAPI(string urlApi, string websiteAuthentification, Action<UpdaterState> updateState
            ,Action<string> updateAddress)
        {
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
                LoginToken = JsonConvert.DeserializeObject<LoginToken>(File.ReadAllText("kryxiviaToken.json"));

            while (LoginToken == null)
            {
                await LoadNewToken();
            }

            if (LoginToken?.jwtAttached == null)
                return UpdaterState.Connecting;
            else
            {
                jwtRaw = LoginToken.jwtAttached;
                jwtSecurityToken = new JwtSecurityToken(LoginToken?.jwtAttached);
                if (DateTime.Now > jwtSecurityToken.ValidTo)
                {
                    LoginToken = null;
                    while (LoginToken == null)
                    {
                        await LoadNewToken();
                    }

                    return UpdaterState.Connecting;
                }
                else
                {
                    var publicKey = jwtSecurityToken.Claims.First(x => x.Type == "publickey");
                    _updateAddress(publicKey.Value);
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

        public void OpenWebSite()
        {
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
                    var callback = await CallBackAuthentification();
                    while(callback == null || callback.jwtAttached == null)
                    {
                        await Task.Delay(1000);
                        callback = await CallBackAuthentification();
                    }
                    File.WriteAllText("kryxiviaToken.json", JsonConvert.SerializeObject(callback));
                    jwtSecurityToken = new JwtSecurityToken(callback?.jwtAttached);
                    jwtRaw = callback?.jwtAttached;
                    var publicKey = jwtSecurityToken.Claims.First(x => x.Type == "publickey");
                    _updateAddress(publicKey.Value);
                    _updateState(UpdaterState.Playing);
                    _authPooler = null;
                });
                _authPooler.Start();
            }
        }
    }
}
