using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Configuration;

namespace UnmarkedRegistersEndpoint.Models
{
    public class DBAccess:IDisposable
    {
        #region SQL
        const string UNMARKED_REG_SQL = @"
        SELECT {1} 
        FROM 
            [MD_ActiveUsers] AS ACL WITH (NOLOCK), 
            [MD_UnmarkedRegisters] AS URS WITH (NOLOCK)
        WHERE
            LOWER(ACL.Username) = LOWER('{0}')
        AND LOWER(ACL.AccessLevel) = URS.Dept
        OR  
            LOWER(ACL.Username) = LOWER('{0}')
        AND LOWER(ACL.AccessLevel) = 'own'
        AND LOWER(URS.Owner) = LOWER(ACL.Username)
        OR
            LOWER(ACL.Username) = LOWER('{0}')
        AND LOWER(ACL.AccessLevel) = 'all'
        {2}
";
        const string UNMARKED_REG_GROUP_BY_DEPT_SELECT = @"count(*) as No_REGISTERS, URS.Dept as DEPARTMENT";
        const string UNMARKED_REG_GROUP_BY_DEPT = @"Group by URS.Dept";
        const string UNMARKED_REG_GROUP_BY_USER_SELECT = @"count(*) as No_REGISTERS, URS.Owner as OWNER";
        const string UNMARKED_REG_GROUP_BY_USER = @"Group by URS.Owner";
        const string UNMARKED_REG_LECTURER_SELECT = @"RegisterNo, RegisterTitle, urs.Date AS Date, SessionDateTime, CollegeLevelName";
        const string UNMARKED_REG_LECTURER_GROUP_BY = "";
        #endregion

        private SqlCommand _cmd = null;
        private SqlConnection _conn = null;
        private static DBAccess _instance = null;
        private string _connStr = string.Empty;
        
        // Singleton Pattern

        /// <summary>
        /// An instance of the DBAccess object
        /// </summary>
        public static DBAccess Instance
        {
            get
            {
                if (_instance == null)
                {
                    object o = new object();
                    lock (o)
                    {
                        if (_instance == null)
                        {
                            ConnectionStringSettingsCollection settings = ConfigurationManager.ConnectionStrings;
                            string conStr = settings["hbConnStr"].ConnectionString;
                            _instance = new DBAccess(conStr);
                        }
                    }
                }
                return _instance;
            }       
        }

        DBAccess (string connectionString)
        {
            _connStr = connectionString;
            _conn = new SqlConnection(_connStr);
            _conn.Open();
        }

        private void CheckConn()
        {
            if (_conn.State != System.Data.ConnectionState.Open)
            {
                _conn.Open();
            }
        }

        /// <summary>
        /// Returns the rights for the user, 0 = lecturer, 1 = department head, 2= College Manager
        /// </summary>
        /// <param name="id">Username</param>
        /// <returns></returns>
        public int Rights(string id)
        {
            CheckConn();
            int rights = -1;
            try
            {
                string sql = string.Format("select [MD_ActiveUsers].AccessLevel from [MD_ActiveUsers] where LOWER(Username) = LOWER('{0}')", id);
                _cmd = new SqlCommand(sql, _conn);
                List<string> depts = new List<string>() {"av-ec", "av-hc", "av-sa", "av-vs", "bs-ss", "ca-yp", "ee-bd"};
                using (SqlDataReader rdr = _cmd.ExecuteReader())
                {
                    if(rdr.Read())
                    {
                        string acl = rdr.GetString(0).ToLower();
                        if (depts.Find(d=> d == acl) != null)
                        {acl = "dept";}

                        switch(acl.Trim())
                        {
                            case "own":
                                rights = 0;
                                break;
                            case "dept":
                                rights = 1;
                                break;
                            case "all":
                                rights = 2;
                                break;
                            default:
                                rights = -1;
                                break;
                        }
                    }
                }
            }
            catch
            {
                return -1;
            }
            return rights;
        }

        /// <summary>
        /// Returns an int of the total number of unmarked registers for the user, depending upon their access level
        /// </summary>
        /// <param name="userID">The id of the user as held by the system</param>
        /// <returns>An int of the total number of unmarked registers</returns>
        public List<Models.UnmarkedRegisters> LecturerUnmarkedRegisters (string userID)
        {
            CheckConn();
            List<Models.UnmarkedRegisters> umrCollection = new List<UnmarkedRegisters>();
            try
            {

                string sql = string.Format(UNMARKED_REG_SQL, userID, UNMARKED_REG_LECTURER_SELECT, UNMARKED_REG_LECTURER_GROUP_BY);
                _cmd = new SqlCommand(sql,_conn);
                using (SqlDataReader rdr = _cmd.ExecuteReader())
                {

                    while (rdr.Read())
                    {
                        Models.UnmarkedRegisters ur = new UnmarkedRegisters();
                        ur.RegisterNo = rdr.GetString(0);
                        ur.RegisterTitle = rdr.GetString(1);
                        ur.Date = rdr.GetDateTime(2);
                        ur.SessionDateTime = rdr.GetDateTime(3);
                        ur.CollegeLevelCode = rdr.GetString(4);
                        umrCollection.Add(ur);
                    }
                }
            }
            catch
            {
                return null;
            }
            return umrCollection;
        }

        



        /// <summary>
        /// Returns a dictionary of unmarked registers per department
        /// </summary>
        /// <param name="userID">The id of the user as held by the system</param>
        /// <returns>A dictionary of unmarked registers per department</returns>
        public Dictionary<string, int> UnmarkedRegistersByDept(string userID)
        {
            CheckConn();
            Dictionary<string, int> dic = new Dictionary<string, int>();
            try
            {
                _cmd = new SqlCommand(string.Format(UNMARKED_REG_SQL, userID, UNMARKED_REG_GROUP_BY_DEPT_SELECT, UNMARKED_REG_GROUP_BY_DEPT), _conn);
                using (SqlDataReader rdr = _cmd.ExecuteReader())
                {

                    while (rdr.Read())
                    {
                        dic.Add(rdr.GetString(1), rdr.GetInt32(0));
                    }
                }
            }
            catch
            {

            }
            return dic;
            
        }
        
        /// <summary>
        /// Returns a dictionary of unmarked registers per lecturer/owner 
        /// </summary>
        /// <param name="userID">The id of the user as held by the system</param>
        /// <returns>A dictionary of unmarked registers per lecturer/owner</returns>
        public Dictionary<string, int> UnmarkedRegistersByLecturer(string userID)
        {
            CheckConn();
            Dictionary<string, int> dic = new Dictionary<string, int>();
            try
            {
                _cmd = new SqlCommand(string.Format(UNMARKED_REG_SQL, userID, UNMARKED_REG_GROUP_BY_USER_SELECT, UNMARKED_REG_GROUP_BY_USER), _conn);
                using (SqlDataReader rdr = _cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        dic.Add(rdr.GetString(1), rdr.GetInt32(0));
                    }
                }
            }
            catch
            {

            }
            return dic;
            
        }

        public void Dispose()
        {
            if(_conn.State == System.Data.ConnectionState.Open)
            {
                _conn.Close();
            }
        }
    }
}