using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShrekBot.Modules.Swamp.Helpers
{
    internal static class Utilities
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="internalHash"></param>
        /// <returns>Returns the converted Uint if the conversion succeeds, else 0 if it fails</returns>
        public static ulong CheckAndConvertUInt(string internalHash)
        {
            if (internalHash.Length > 20) //overflow
                return 0;
            if (ulong.TryParse(internalHash, out _))
                return Convert.ToUInt64(internalHash);
            return 0; 
        }
    }
}
