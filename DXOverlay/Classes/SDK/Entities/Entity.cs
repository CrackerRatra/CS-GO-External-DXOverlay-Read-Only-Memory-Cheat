using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Memory.Memorys;

namespace DXOverlay.Classes.SDK.Entities
{
    class Entity
    {
        public int Address = -1;

        public Entity(int address)
        {
            Address = address;
        }

        public int Health => Read<int>(Address + Game.Offsets.netvars.m_iHealth);
        public int Armor => Read<int>(Address + Game.Offsets.netvars.m_ArmorValue);
        public Variables.Enums.Team Team => (Variables.Enums.Team)Read<int>(Address + Game.Offsets.netvars.m_iTeamNum);
        public bool Spotted => Read<bool>(Address + Game.Offsets.netvars.m_bSpottedByMask);
        public Vector3 Origin => Read<Vector3>(Address + Game.Offsets.netvars.m_vecOrigin);
        public int BoneMatrix => Read<int>(Address + Game.Offsets.netvars.m_dwBoneMatrix);
        public bool Dormant => Read<bool>(Address + Game.Offsets.signatures.m_bDormant);
        public Vector2 PunchAngle => Read<Vector2>(Address + Game.Offsets.netvars.m_aimPunchAngle);
        public int ObserverTarget => Read<int>(Address + Game.Offsets.netvars.m_hObserverTarget);
        public int ShotsFired => Read<int>(Address + Game.Offsets.netvars.m_iShotsFired);
        public Vector3 Bone(int boneId)
        {
            return new Vector3()
            {
                X = Read<float>(BoneMatrix + 0x30 * boneId + 0x0C),
                Y = Read<float>(BoneMatrix + 0x30 * boneId + 0x1C),
                Z = Read<float>(BoneMatrix + 0x30 * boneId + 0x2C)
            };
        }
    }
}
