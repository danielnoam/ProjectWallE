using UnityEngine;

namespace GruelTerraSplines
{
    public static class UnityObjectIdUtility
    {
        public static EntityId GetEntityId(Object obj)
        {
            return obj != null ? obj.GetEntityId() : default;
        }

        public static int GetObjectHash(Object obj)
        {
            return obj != null ? obj.GetEntityId().GetHashCode() : 0;
        }
    }
}