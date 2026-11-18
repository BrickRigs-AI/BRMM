using DiscordRPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BRMM.Classes
{
    public class ModCard
    {
        public string name { get; set; }
        public string description { get; set; }
        public string descriptionmd { get; set; }
        public string[] links { get; set; }
        public User[] members { get; set; }
        public string[] tags { get; set; }
        public string[] supported_versions { get; set; }
        public DateTime create_time { get; set; }
        public User owner { get; set; }
        public User[] betatesters { get; set; }

        public List<ModCard> additional_mods { get; set; }
        public string permamenturl { get; set; }
        public int download { get; set; }
        public int follows { get; set; }
        public string organizationid { get; set; }
        public int modversion { get; set; }
        public bool modpack { get; set; }
        public ModVersions[] versions { get; set; }

        public ModCardState Undereview { get; set; }
        public ModCard()
        {
        }
        public ModCard(
        string name,
        string permamentUrl,
        User owner,
        bool modType,
        string description = "",
        string descriptionMd = "",
        string organizationId = null)
        {
            this.name = name;
            this.permamenturl = permamentUrl;
            this.owner = owner;
            this.modpack = modType;
            this.description = description;
            this.descriptionmd = descriptionMd;
            this.organizationid = organizationId;

            this.links = Array.Empty<string>();
            this.members = Array.Empty<User>();
            this.tags = Array.Empty<string>();

            this.create_time = DateTime.UtcNow;
            this.download = 0;
            this.follows = 0;
            this.modversion = 1;
            this.Undereview = new ModCardState(0, "");
        }
    }
    public class ModVersions
    {
        public string Version_name { get; set; }
        public int type { get; set; }
        public int modversion { get; set; }
    }
    public class ModCardState
    {
        public int status { get; set; }
        public string message { get; set; }

        public ModCardState(int status, string message)
        {
            this.status = status;
            this.message = message;
        }
    }
}
