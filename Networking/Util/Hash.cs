using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KMP.Networking.Util
{
    public class Hash
    {
        public static long MD5(byte[] data)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(data);
                byte[] longBytes = new byte[8];
                for (int i = 0, j = 0; i < hash.Length; i++, j++)
                {
                    if (j >= 8) j = 0;
                    longBytes[j] ^= hash[i];
                }
                return BitConverter.ToInt64(longBytes, 0);
            }
        }
    }
}
