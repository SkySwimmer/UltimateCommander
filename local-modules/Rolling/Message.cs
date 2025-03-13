using System.Collections.Generic;
using System.Xml.Serialization;
using CMDR;
using Discord;

namespace Rolling
{
    public class Message {
        public ulong id = 0;
        public ulong channel = 0;
        public ulong guild = 0;

        public List<ulong> roles = new List<ulong>();
        public List<MemberMeta> members =  new List<MemberMeta>();

        public class MemberMeta {
            public ulong role = 0;
            public string reactionIcon = "";
            public List<ulong> users = new List<ulong>();
        }

        
        [XmlIgnoreAttribute]
        public IMessage message;
    }
}
