using MessagePack;
using realtime_game.Server.Models.Entities;
using System;
using UnityEngine;

namespace realtime_game.Server.StreamingHubs
{
    [MessagePackObject]
    public class JoinedUser
    {
        [Key(0)]
        public Guid ConnectionId { get; set; }
        [Key(1)]
        public User UserData { get; set; }
        [Key(2)]
        public int JoinOrder { get; set; }
    }
}
