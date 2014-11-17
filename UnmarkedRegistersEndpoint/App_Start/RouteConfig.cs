using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace UnmarkedRegistersEndpoint
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Rights",
                url: "Rights/{id}",
                defaults: new { controller = "Home", action = "Rights", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "LecturerUnmarkedRegisters",
                url: "LecturerUnmarkedRegisters/{id}",
                defaults: new { controller = "Home", action = "Rights", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "UnmarkedRegistersByDept",
                url: "UnmarkedRegistersByDept/{id}",
                defaults: new { controller = "Home", action = "Rights", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "UnmarkedRegistersByLecturer",
                url: "UnmarkedRegistersByLecturer/{id}",
                defaults: new { controller = "Home", action = "Rights", id = UrlParameter.Optional }
            );

            // catch all
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

        }
    }
}