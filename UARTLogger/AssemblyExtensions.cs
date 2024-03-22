using System.Reflection;

namespace Plugins.UARTLogger
{
    public static class AssemblyExtensions
    {
        public static string GetAssemblyConfiguration(this Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);

            AssemblyConfigurationAttribute attribute = null;
            if (attributes.Length > 0)
            {
                attribute = attributes[0] as AssemblyConfigurationAttribute;
            }
            return (attribute?.Configuration ?? "");
        }
    }
}
