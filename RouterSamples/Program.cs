using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Routing.Template;
using System.Linq;

namespace RouterSamples
{
    // Kaynak : https://www.buraksenyurt.com/post/AspNet-Core-Routing-Mekanizmas%C4%B1n%C4%B1-Kavramak
    /*
    Exe olarak yayınlamak için ise; console'a "dotnet publish -c debug -r win10-x64" yazmak yeterlidir.
    netcoreapp2.2 klasörü içerisine win10-x64 isimli klasör oluşturarak, debug ettiklerini buraya atacaktır.
    */
    
    class Program
    {
        static void Main(string[] args)
        {
            new Thread((y)=>{
                // http://localhost:6001/api/Teams
                var lasthost = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://localhost:6001")
                .UseStartup<BoosterV3>()
                .Build();
                lasthost.Run();
            }).Start();

            new Thread((x)=>
            {
                var secondhost = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://localhost:5001")
                .UseStartup<BoosterV2>()
                .Build();
                secondhost.Run();
            }).Start();

            var firsthost = new WebHostBuilder()
            .UseKestrel()
            .UseUrls("http://localhost:4001")
            .UseStartup<Booster>()
            .Build();
            firsthost.Run();
        }
    }

    class Booster
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app)
        {
            var rootBuilder = new RouteBuilder(app);

            rootBuilder.MapGet("", (context) =>
            {
                context.Response.Headers.Add("Content-Type", "text/html; charset=utf-8");
                return context.Response.WriteAsync($"<h1><p style='color:orange'>Hoşgeldin Sahip</p></h1><i>Bugün nasılsın?</i>");
            }
            );

            rootBuilder.MapGet("green/mile", (context) =>
            {
                var routeData = context.GetRouteData();
                context.Response.Headers.Add("Content-Type", "text/html; charset=utf-8");
                return context.Response.WriteAsync($"Vayyy <b>Gizli yolu</b> buldun!<br/>Tebrikler.");
            }
            );

            // rootBuilder.MapGet("{*urlPath}", (context) =>
            // {
            //     var routeData = context.GetRouteData();
            //     return context.Response.WriteAsync($"Path bilgisi : {string.Join(",", routeData.Values)}");
            // }
            // );

            rootBuilder.MapGet("whatyouwant/{wanted=1 Bitcoin Please}", (context) =>
            {
                var values = context.GetRouteData().Values;
                context.Response.Headers.Add("Content-Type", "text/html; charset=utf-8");
                return context.Response.WriteAsync($"İstediğin şey bu <h2>{values["wanted"]}<h2> Oldu bil :)");
            }
            );
            //http://localhost:4001/whatyouwant/AudiRS8 = wanted'e karşılık gelen parametre değeri yazdırır; yani "AudiRS8"
            //http://localhost:4001/whatyouwant/ = wanted default değeri yazdırır; yani "1 Bitcoin Please"

            app.UseRouter(rootBuilder.Build());
        }
    }

    class BoosterV2
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }
 
        public void Configure(IApplicationBuilder app)
        {
            var handler = new RouteHandler(context =>
            {
                var routeValues = context.GetRouteData().Values;
                var path = context.Request.Path;
                if (path == "/products/books")
                {
                    context.Response.Headers.Add("Content-Type", "application/json");
                    var books = File.ReadAllText("books.json");
                    return context.Response.WriteAsync(books);
                }
 
                context.Response.Headers.Add("Content-Type", "text/html;charset=utf-8");
                return context.Response.WriteAsync(
                    $@" 
                    <html>
                    <body>
                    <h2>Selam Patron! Bugün nasılsın?</h2>
                    {DateTime.Now.ToString()}
                    <ul>
                        <li><a href='/products/books'>Senin için bir kaç kitabım var. Haydi tıkla.</a></li>
                        <li><a href='https://github.com/buraksenyurt'>Bu ve diğer .Net Core örneklerine bakmak istersen Git!</a></li>
                    </ul>                     
                    </body>
                    </html>
                    ");
            });
            app.UseRouter(handler);
        }
    }

    class BoosterV3
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
        }
 
        public void Configure(IApplicationBuilder app)
        {
            /* apiSegment ve serviceNameSegment isimli iki TemplateSegment örneği oluşturuluyor. İlki Literal tipindeyken ikincisi parametre türünden. 
            Yani http://localhost:4001/api/collateral gibi bir path için api ifadesinin Literal olduğunu, collateral parçasının ise değişken türde 
            parametre olduğunu ifade edebiliriz.  */
            var apiSegment = new TemplateSegment();
            apiSegment.Parts.Add(TemplatePart.CreateLiteral("api"));
 
            var serviceNameSegment = new TemplateSegment();
            serviceNameSegment.Parts.Add(
                TemplatePart.CreateParameter("serviceName",
                    isCatchAll: false,
                    isOptional: true,
                    defaultValue: null,
                    inlineConstraints: new InlineConstraint[] { })
            );
 
            var segments = new TemplateSegment[] {
                apiSegment,
                serviceNameSegment
            };
 
            var routeTemplate = new RouteTemplate("default", segments.ToList());
            var templateMatcher = new TemplateMatcher(routeTemplate, new RouteValueDictionary());
 
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("Content-type", "text/html");
                var requestPath = context.Request.Path;
                var routeData = new RouteValueDictionary();
                var isMatch = templateMatcher.TryMatch(requestPath, routeData);
                await context.Response.WriteAsync($"Request Path is <i>{requestPath}</i><br/>Match state is <b>{isMatch}</b><br/>Requested service name is {routeData["serviceName"]}");
                await next.Invoke();
            });
 
            app.Run(async context =>
            {
                await context.Response.WriteAsync("");
            });
        }
    }
}