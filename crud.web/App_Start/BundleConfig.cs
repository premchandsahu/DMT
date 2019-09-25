using System.Web;
using System.Web.Optimization;

namespace crud.web
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/site.css"));

            bundles.Add(new ScriptBundle("~/bundles/Crud")
                        .Include("~/Scripts/dirPagination.js")
                        .IncludeDirectory("~/Scripts/Controllers", "*.js")
                        .Include("~/Scripts/Crud.js"));

            //BundleTable.EnableOptimizations = true;
        }
    }
}
