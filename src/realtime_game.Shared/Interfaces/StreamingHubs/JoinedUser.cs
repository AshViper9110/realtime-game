using MessagePack;
using realtime_game.Server.Models.Entities;
using System;

namespace realtime_game.Server.StreamingHubs
{
    [MessagePackObject]
    public class JoinedUser
    {
        [Key(0)]
        public Guid ConnectionId { get; set; }
        [Key(1)]
        public string UserName { get; set; }
        [Key(2)]
        public int JoinOrder { get; set; }
        [Key(3)]
        public bool IsReady { get; set; } = false;
        [Key(4)]
        public bool IsOwner { get; set; } = false; // 新規
    }
}
