using System.Reflection;

namespace FrozTLDMods.FrozTLDMod
{
    internal static class Il2CppReflection
    {
        // Reads a public or private field/property without requiring a compile-time IL2CPP wrapper type.
        internal static object GetObjectMember(object target, string memberName)
        {
            if (target == null)
            {
                return null;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var field = target.GetType().GetField(memberName, flags);
            if (field != null)
            {
                return field.GetValue(target);
            }

            var property = target.GetType().GetProperty(memberName, flags);
            return property != null && property.GetIndexParameters().Length == 0 ? property.GetValue(target) : null;
        }

        // Writes a public or private field/property when the reflected member supports assignment.
        internal static bool SetObjectMember(object target, string memberName, object value)
        {
            if (target == null)
            {
                return false;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var field = target.GetType().GetField(memberName, flags);
            if (field != null)
            {
                field.SetValue(target, value);
                return true;
            }

            var property = target.GetType().GetProperty(memberName, flags);
            if (property != null && property.CanWrite && property.GetIndexParameters().Length == 0)
            {
                property.SetValue(target, value);
                return true;
            }

            return false;
        }
    }
}
