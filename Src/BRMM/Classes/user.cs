using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRMM.Classes
{
    class user
    {
        public string id { get; set; }
        public string username { get; set; }
        public string bio { get; set; }
        public int supporttier { get; set; }
        public DateTime joindate { get; set; }

        public user(string id, string username, string bio, int supporttier, DateTime joindate)
        {
            this.id = id;
            this.username = username;
            this.bio = bio;
            this.supporttier = supporttier;
            this.joindate = joindate;
        }
    }
}
