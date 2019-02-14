using System.IO;

namespace RunSqlScript.Extensions
{
    public static class StringExtensions
    {
        public static bool HasExtension(this string path, string extension)
        {
            var ext = Path.GetExtension(path);
            return ext != null && ext.Equals(extension);
        }
    }
}
