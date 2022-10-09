using System.Reflection;

namespace MiCake.Core.Util.Reflection
{
    public static class MemberInfoExtensions
    {
        public static Type? GetMemberType(this MemberInfo memberInfo)
            => (memberInfo as PropertyInfo)?.PropertyType ?? ((FieldInfo)memberInfo)?.FieldType;

        public static string GetSimpleMemberName(this MemberInfo member)
        {
            var name = member.Name;
            var index = name.LastIndexOf('.');
            return index >= 0 ? name[(index + 1)..] : name;
        }
    }
}
