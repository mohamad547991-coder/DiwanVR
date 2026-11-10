using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
namespace DiwanVR.Editor
{
    public static class SourceFingerprint
    {
        public static string Code()
        {
            var text=new StringBuilder();
            foreach(string file in Directory.GetFiles("Assets/Diwan","*.cs",SearchOption.AllDirectories).OrderBy(p=>p.Replace('\\','/'),StringComparer.Ordinal))
                text.Append(file.Replace('\\','/')).Append('\n').Append(FileHash(file)).Append('\n');
            using(var sha=SHA256.Create())return Hex(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
        }
        public static string FileHash(string path){using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return Hex(sha.ComputeHash(stream));}
        private static string Hex(byte[] bytes)=>BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();
    }
}
