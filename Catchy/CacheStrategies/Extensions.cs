using Murmur;
using System.Text;

namespace Catchy.CacheStrategies
{
    public static class Extensions
    {
        public static string GetHash(this string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashedBytes = MurmurHash.Create128().ComputeHash(bytes);
            var hash = Encoding.UTF8.GetString(hashedBytes);
            return hash;
        }
    }
}
