using UnityEngine;

namespace realtime_game.Server.StreamingHubs
{
    public class RoomUserData
    {
        public JoinedUser JoinedUser;

        public Vector3 pos;

        public Quaternion rot;

        public Vector3 vel;

        public bool IsGoal;
    }
}
