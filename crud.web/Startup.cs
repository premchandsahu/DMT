using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(crud.web.Startup))]
namespace crud.web
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
