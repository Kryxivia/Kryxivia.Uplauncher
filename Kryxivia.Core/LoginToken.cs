using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KryxiviaUpdater.Core
{
    public class LoginToken
    {
        public string tokenHash { get; set; }
        public string date { get; set; }
        public bool validated { get; set; }
        public string? jwtAttached { get; set; }
    }
}
