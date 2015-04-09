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
        const string UNMARKED_REG_SQL_old = @"
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
        const string UNMARKED_REG_SQL = @"
        SELECT {1} 
        FROM 
            [MD_ActiveUsers] AS ACL WITH (NOLOCK), 
            [MD_UnmarkedRegisters] AS URS WITH (NOLOCK)
        WHERE
        {0}
        {2}
";
        const string UNMARKED_REG_GROUP_BY_DEPT_SELECT = @"count(*) as No_REGISTERS, URS.Dept as DEPARTMENT";
        const string UNMARKED_REG_GROUP_BY_DEPT = @"Group by URS.Dept";
        const string UNMARKED_REG_GROUP_BY_USER_SELECT = @"count(*) as No_REGISTERS, URS.Owner as OWNER";
        const string UNMARKED_REG_GROUP_BY_USER = @"Group by URS.Owner";
        const string UNMARKED_REG_LECTURER_SELECT = @"RegisterNo, RegisterTitle, urs.Date AS Date, SessionDateTime, CollegeLevelName";
        const string UNMARKED_REG_LECTURER_GROUP_BY = "";
        const string UNMARKED_REG_BY_DEPT = @"select Owner, COUNT(*)  from MD_UnmarkedRegisters where Dept = '{0}' group by Owner";
        const string RIGHTS_OWN = @"
            LOWER(ACL.Username) = LOWER('{0}')
        AND LOWER(ACL.AccessLevel) = 'own'
        AND LOWER(URS.Owner) = LOWER(ACL.Username)
";
        const string RIGHTS_DEPT = @"
            LOWER(ACL.Username) = LOWER('{0}')
        AND LOWER(ACL.AccessLevel) = URS.Dept
";

        const string RIGHTS_ALL = @"
            LOWER(ACL.Username) = LOWER('{0}')
        AND LOWER(ACL.AccessLevel) = 'all'
";
        #endregion

        private SqlCommand _cmd = null;
        private SqlConnection _conn = null;
        private string _connStr = string.Empty;
        private string _rightsStr = string.Empty;
        private System.Diagnostics.EventLog _applog = new System.Diagnostics.EventLog("Application");        


        public DBAccess ()
        {
            ConnectionStringSettingsCollection settings = ConfigurationManager.ConnectionStrings;
            _connStr = settings["hbConnStr"].ConnectionString;
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
                                _rightsStr = string.Format(RIGHTS_OWN,id);
                                break;
                            case "dept":
                                rights = 1;
                                _rightsStr = string.Format(RIGHTS_DEPT, id);
                                break;
                            case "all":
                                rights = 2;
                                _rightsStr = string.Format(RIGHTS_ALL, id);
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
        public List<UnmarkedRegisters> LecturerUnmarkedRegisters (string userID)
        {
            _applog.Source = "Hugh Baird Unmarked Registers App";
            CheckConn();
            Rights(userID);
            List<Models.UnmarkedRegisters> umrCollection = new List<UnmarkedRegisters>();
            try
            {

                string sql = string.Format(UNMARKED_REG_SQL, _rightsStr, UNMARKED_REG_LECTURER_SELECT, UNMARKED_REG_LECTURER_GROUP_BY);
                _cmd = new SqlCommand(sql,_conn);
                using (SqlDataReader rdr = _cmd.ExecuteReader())
                {

                    while (rdr.Read())
                    {
                        Models.UnmarkedRegisters ur = new UnmarkedRegisters();
                        ur.RegisterNo = rdr.IsDBNull(0) ? "":rdr.GetString(0);
                        ur.RegisterTitle = rdr.IsDBNull(1) ? "" : rdr.GetString(1);
                        ur.Date = rdr.IsDBNull(2) ? DateTime.MinValue : rdr.GetDateTime(2).ToUniversalTime();
                        ur.SessionDateTime = rdr.IsDBNull(3) ? DateTime.MinValue : rdr.GetDateTime(3).ToUniversalTime();
                        ur.CollegeLevelCode = rdr.IsDBNull(4) ? "" : rdr.GetString(4);
                        umrCollection.Add(ur);
                    }
                }
            }
            catch(Exception e)
            {
                _applog.WriteEntry("An error occurred in LecturerUnmarkedRegisters:" + GetErrorMsg(e), System.Diagnostics.EventLogEntryType.Error);
                return null;
            }
            return umrCollection;
        }

        private string GetErrorMsg(Exception e)
        {
            string result = e.Message;
            if(e.InnerException != null)
            {
                result += ": Inner Ex - " + GetErrorMsg(e.InnerException);
            }
            return result;
        }

        /// <summary>
        /// Returns a dictionary of unmarked registers per department
        /// </summary>
        /// <param name="userID">The id of the user as held by the system</param>
        /// <returns>A dictionary of unmarked registers per department</returns>
        public List<Department> UnmarkedRegistersByDept(string userID)
        {
            _applog.Source = "Hugh Baird Unmarked Registers App";
            CheckConn();
            Rights(userID);
            List<Department> dic = new List<Department>(); // Need to build this object On Monday
            try
            {
                System.Diagnostics.Stopwatch sWatch = new System.Diagnostics.Stopwatch();
                sWatch.Start();
                _cmd = new SqlCommand(string.Format(UNMARKED_REG_SQL, _rightsStr, UNMARKED_REG_GROUP_BY_DEPT_SELECT, UNMARKED_REG_GROUP_BY_DEPT), _conn);
                _cmd.CommandTimeout = 90;
                using (SqlDataReader rdr = _cmd.ExecuteReader())
                {

                    while (rdr.Read())
                    {
                        Department deptartment = new Department();
                        deptartment.DeptName = rdr.IsDBNull(1)? "": rdr.GetString(1);
                        deptartment.UnmarkedRegisters = rdr.IsDBNull(0)? -1: rdr.GetInt32(0);
                        dic.Add(deptartment);                        
                    }
                }
                sWatch.Stop();

                _applog.WriteEntry("Time to retrieve depts: " + sWatch.Elapsed.TotalSeconds.ToString(), System.Diagnostics.EventLogEntryType.Information);

                sWatch.Restart();
                //foreach (Department d in dic)
                //{
                //    _cmd = new SqlCommand(string.Format(UNMARKED_REG_BY_DEPT, d.DeptName), _conn);
                //    _cmd.CommandTimeout = 90;
                //    using(SqlDataReader rdr = _cmd.ExecuteReader())
                //    {
                //        Dictionary<string, int> deptContent = new Dictionary<string, int>();
                //        while(rdr.Read())
                //        {
                //            int i = 0;
                //            string s = "";
                //            if (rdr.IsDBNull(1))
                //            {
                //                s = "Error: Count is null!";
                //            }
                //            else
                //            {
                //                i = rdr.GetInt32(1);
                //            }
                            
                //            s = rdr.IsDBNull(0) ? s : rdr.GetString(0);
                //            deptContent.Add(s,i);
                //        }
                //        d.Lecturers = deptContent;
                //    }                 
                //}

                // run multiple statements for each department in order
                string sql = string.Empty;
                bool loopResult = true;

                //construct multiple select statement
                for(int idx = 0; idx < dic.Count;idx++) 
                {
                    sql += string.Format(UNMARKED_REG_BY_DEPT, dic[idx].DeptName) + "; ";
                }
                _cmd = new SqlCommand(sql, _conn);

                // get datasets back
                using(SqlDataReader rdr = _cmd.ExecuteReader())
                {
                    int idx = 0;
                    while (loopResult) // while there are more result sets to get
                    {
                        Dictionary<string, int> deptContent = new Dictionary<string, int>();
                        while (rdr.Read())
                        {
                            int i = 0;
                            string s = "";

                            if (rdr.IsDBNull(1))
                            {
                                s = "Error: Count is null!";
                            }
                            else
                            {
                                i = rdr.GetInt32(1);
                            }

                            s = rdr.IsDBNull(0) ? s : rdr.GetString(0);
                            deptContent.Add(s, i);
                        }
                        dic[idx].Lecturers = deptContent;   // put lecturer data into corresponding department
                        idx++;                              // next data set
                        loopResult = rdr.NextResult();
                    }
                }

                sWatch.Stop();
                _applog.WriteEntry("Time to retrieve lecturer data for depts: " + sWatch.Elapsed.TotalSeconds.ToString(), System.Diagnostics.EventLogEntryType.Information);
            }
            catch(Exception e)
            {
                _applog.WriteEntry("Error occured in UnmarkedRegistersByDept: " + GetErrorMsg(e), System.Diagnostics.EventLogEntryType.Error);
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
            _applog.Source = "Hugh Baird Unmarked Registers App";
            CheckConn();
            Rights(userID);
            Dictionary<string, int> dic = new Dictionary<string, int>();
            try
            {
                _cmd = new SqlCommand(string.Format(UNMARKED_REG_SQL, _rightsStr, UNMARKED_REG_GROUP_BY_USER_SELECT, UNMARKED_REG_GROUP_BY_USER), _conn);
                _cmd.CommandTimeout = 90;
                using (SqlDataReader rdr = _cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        string s = "";
                        int i = 0;
                        if(rdr.IsDBNull(0))
                        {
                            s = "Error: Count is null!";
                        }
                        else
                        {
                            rdr.GetInt32(0);
                        }
                        s = rdr.IsDBNull(1) ? s : rdr.GetString(1);
                        dic.Add(s, i);
                    }
                }
            }
            catch(Exception e)
            {
                _applog.WriteEntry("Error occured in UnmarkedRegistersByLecturer: " + GetErrorMsg(e), System.Diagnostics.EventLogEntryType.Error);
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