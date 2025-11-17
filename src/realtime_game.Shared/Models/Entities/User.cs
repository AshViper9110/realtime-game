
using MessagePack;
using System;

namespace realtime_game.Server.Models.Entities
{
    [MessagePackObject]
    public class User
    {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public string Name { get; set; }
        [Key(2)]
        public string Token { get; set; }
        [Key(3)]
        public DateTime Created_at { get; set; }
        [Key(4)]
        public DateTime Updated_at { get; set; }
    }
}
