using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Collabco.Security
{
    public class AuthString
    {
        public string UserID    { get; set; }
        public string TimeStamp { get; set; }
        public string Hash
        {
            get
            { return _hash; }
        }

        private string _hash = string.Empty;
        private string _secret = string.Empty;

        AuthString()
        {

        }

        /// <summary>
        /// Builds an authstring from a full URL
        /// </summary>
        /// <param name="url">Full URL with auth parameters</param>
        public AuthString(string url, string secret)
        {
            int paramsInCount = 0;
            string[] queryStr = url.Split('?');
            string[] paramsIn = queryStr[1].Split('&');
            string hashCheck = string.Empty;

            foreach(string s in paramsIn)
            {
                string[] prm = s.Split('=');
                switch (prm[0].ToLower())
                {
                    case "id":
                        UserID = prm[1];
                        paramsInCount++;
                        break;
                    case "ts":
                        TimeStamp = prm[1];
                        paramsInCount++;
                        break;
                    case "hash":
                        hashCheck = prm[1];
                        paramsInCount++;
                        break;
                    default:
                        break;
                }
            }
            if (paramsInCount < 3)
            {
                throw new IncorrectHashException("Not enough parameters entered in query string");
            }
            _secret = secret;
            string hch = CreateInstanceMD5Hash();
            if (hashCheck != hch)
            {
                throw new IncorrectHashException("Bad hash detected");
            }
            else
            {
                _hash = hashCheck;
            }
            
        }

        /// <summary>
        /// Creates an access query string
        /// </summary>
        /// <param name="userID">User ID</param>
        /// <param name="utcTimeStamp">A UTC DateTime TimeStamp</param>
        /// <param name="secret">Shhh!</param>
        public AuthString(string userID, DateTime utcTimeStamp, string secret)
        {
            //populate Properties
            UserID = userID;
            TimeStamp = utcTimeStamp.ToUniversalTime().ToString("u");
            _secret = secret;
            _hash = CreateInstanceMD5Hash();
        }

        /// <summary>
        /// Creates an MD5 hash of the values this instance was created with.
        /// </summary>
        /// <returns>Hash of the UserID, UTC TimeStamp and secret</returns>
        public string CreateInstanceMD5Hash()
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(UserID + TimeStamp + _secret);
            return HashInput(inputBytes);            
        }

        /// <summary>
        /// Updates this instance with a fresh timestamp and hash
        /// </summary>
        public void RefreshInstanceMD5Hash()
        {
            TimeStamp =  DateTime.UtcNow.ToString();
            byte[] inputBytes = Encoding.ASCII.GetBytes(UserID + TimeStamp + _secret);
            _hash = HashInput(inputBytes);
        }

        /// <summary>
        /// Tests this instance to check that it's current
        /// </summary>
        /// <param name="seconds">Number of seconds considered 'current'</param>
        /// <returns></returns>
        public bool IsCurrent(int seconds=30)
        {
            DateTime dt = DateTime.Parse(TimeStamp);
            dt = dt.AddSeconds(seconds).ToUniversalTime();
            if ((DateTime.UtcNow) < dt)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether an AuthString is valid, based on this Authstring instance
        /// </summary>
        /// <param name="authStr">AuthString object to compare it to</param>
        /// <param name="seconds">Number of seconds considered valid</param>
        /// <returns></returns>
        public bool IsValid(string hash, int seconds = 30)
        {
            bool isCurrent = IsCurrent(seconds);
           return ((isCurrent) && (hash == Hash));
        }

        private string HashInput(byte[] inputBytes)
        {
            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
    
}
