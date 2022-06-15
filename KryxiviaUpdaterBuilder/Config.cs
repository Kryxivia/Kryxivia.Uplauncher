using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KryxiviaUpdater.Builder
{
    public class Config
    {
        public string URL { get; set; } = "";
        public string PathFolder { get; set; } = "";

        public string Server { get; set; } = "";
        public int Port { get; set; }
        public string User { get; set; } = "";
        public string Password { get; set; } = "";

        public string SftpPath { get; set; } = "";
    }
}
