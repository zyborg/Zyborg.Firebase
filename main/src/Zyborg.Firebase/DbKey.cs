using System.Text;

namespace Zyborg.Firebase
{
    public static class DbKey
    {
        private static bool[] _dbKeyBadChars = new bool[128];

        static DbKey()
        {
            for (int i = 0; i <= 31; ++i)
                _dbKeyBadChars[i] = true;
            _dbKeyBadChars[127] = true;
            _dbKeyBadChars['.'] = true;
            _dbKeyBadChars['$'] = true;
            _dbKeyBadChars['#'] = true;
            _dbKeyBadChars['['] = true;
            _dbKeyBadChars[']'] = true;
            _dbKeyBadChars['/'] = true;
        }

        /// <summary>
        /// Based on rules defined at
        ///    https://firebase.google.com/docs/database/rest/structure-data
        /// </summary>
        /// <remarks>
        /// Quote:
        /// <code>
        /// If you create your own keys, they must be UTF-8 encoded, can
        /// be a maximum of 768 bytes, and cannot contain ., $, #, [, ],
        /// /, or ASCII control characters 0-31 or 127.
        /// </code>
        /// </remarks>
        public static bool IsValidKey(string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length > 768)
                return false;
            
            foreach (var ch in key.ToCharArray())
                if (_dbKeyBadChars[ch])
                    return false;
            
            return true;
        }
    }
}
