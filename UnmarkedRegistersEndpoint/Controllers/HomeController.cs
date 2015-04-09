using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Collabco.Security;
using System.Web.Script.Serialization;
using UnmarkedRegistersEndpoint.Models;
using System.Web.Cors;
using System.Web.Http.Cors;


namespace UnmarkedRegistersEndpoint.Controllers
{

    public class HomeController : Controller
    {
        //
        // GET: /Home/ **
        public ActionResult Index(string id = "Lynn.Hobbs")
        {
            
            if (id == "") { return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest); }

            DateTime dt = DateTime.Now;
            AuthString aStr = new AuthString(id, dt , GetSecret());
            ViewBag.authStr = aStr;
            ViewBag.time = dt;
            ViewBag.user = id;

            return View();
        }



        private string GetSecret()
        {
            System.Configuration.Configuration webConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/UnmarkedRegistersEndpoint");
            System.Configuration.ConnectionStringSettings connStr = null;

            if (webConfig.ConnectionStrings.ConnectionStrings.Count > 0)
            {
                connStr = webConfig.ConnectionStrings.ConnectionStrings["secret"];
            }

            if (connStr != null)
            {
                return connStr.ToString();
            }
            else
            {
                return null;
            }
        }

        public ActionResult Rights(string id, DateTime ts, string hash)
        {
            if (id == "") { return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest); }

            // Check the hash and passed URL
            AuthString authStr = new AuthString(Request.Url.ToString(), GetSecret());
            if (authStr.IsValid(hash))
            {
                int rights = -1;
                using(Models.DBAccess db = new DBAccess())
                {
                    rights = db.Rights(id);
                }

                return Json(rights, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }
        }


        /// <summary>
        /// Gets the Unmarked Registers for the user
        /// </summary>
        /// <param name="id">User Id</param>
        /// <param name="currentUser">User Id</param>
        /// <returns>JSON Int of the number of unmarked Registers</returns>
        public ActionResult LecturerUnmarkedRegisters(string id, DateTime ts, string hash)
        {
            // No Id, bad request
            if (id == "") { return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest); }

            // Check the hash and passed URL
            AuthString authStr = new AuthString(Request.Url.ToString(), GetSecret());
            if (authStr.IsValid(hash))
            {
                List<Models.UnmarkedRegisters> urCollection;
                Models.UnmarkedRegisters umr = new Models.UnmarkedRegisters();
                using (Models.DBAccess db = new DBAccess())
                {
                    urCollection = db.LecturerUnmarkedRegisters(id);
                }
                //string jStr = new JavaScriptSerializer().Serialize(urCollection);
                return Json(urCollection, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

        }

        /// <summary>
        /// Gets a JSON dictionary object of unmarked registers by department
        /// </summary>
        /// <param name="id">User Id</param>
        /// <returns>JSON dictionary</returns>
        public ActionResult UnmarkedRegistersByDept(string id, DateTime ts, string hash)
        {
            if (id == "") { return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest); }

            // Check the hash and passed URL
            AuthString authStr = new AuthString(Request.Url.ToString(), GetSecret());
            if (authStr.IsValid(hash))
            {
                List<Department> byDept = null;
                using (Models.DBAccess db = new DBAccess())
                {
                    byDept = db.UnmarkedRegistersByDept(id);
                }

                return Json(byDept, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Gets a JSON dictionary object of unmarked registers by lecturer/owner
        /// </summary>
        /// <param name="id">User Id</param>
        /// <returns>JSON dictionary</returns>
        public ActionResult UnmarkedRegistersByLecturer(string id, DateTime ts, string hash)
        {
            if (id == "") { return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest); }

            // Check the hash and passed URL
            AuthString authStr = new AuthString(Request.Url.ToString(), GetSecret());
            if (authStr.IsValid(hash))
            {

                Dictionary<string, int> byOwner;
                using (Models.DBAccess db = new DBAccess())
                {
                    byOwner = db.UnmarkedRegistersByLecturer(id);
                }

                return Json(byOwner, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

        }

    }
}
