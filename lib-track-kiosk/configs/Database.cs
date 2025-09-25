using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_track_kiosk.configs
{
    internal class Database
    {
        public string Host { get; private set; }
        public string Name { get; private set; }
        public string User { get; private set; }
        public string Password { get; private set; }
        public int Port { get; private set; }

        public Database()
        {
            Host = "162.0.231.211";
            Name = "lib_track_db";
            User = "lib_tracker";
            Password = "Password123";
            Port = 3306;
        }
    }
}
