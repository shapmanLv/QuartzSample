using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MySql.Data.MySqlClient;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.AdoJobStore.Common;
using Quartz.Spi;
using QuartzAggregate.Crontab;
using QuartzAggregate.Crontab.Jobs;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace QuartzAggregate
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "QuartzAggregate", Version = "v1" });
            });

            #region quartz

            services.AddHostedService<QuartzHostedService>();
            services.AddSingleton<IJobFactory, JobFactory>();
            services.AddSingleton<ISchedulerFactory>(u => {
                DbProvider.RegisterDbMetadata("mysql-custom", new DbMetadata()
                {
                    AssemblyName = typeof(MySqlConnection).Assembly.GetName().Name,
                    ConnectionType = typeof(MySqlConnection),
                    CommandType = typeof(MySqlCommand),
                    ParameterType = typeof(MySqlParameter),
                    ParameterDbType = typeof(DbType),
                    ParameterDbTypePropertyName = "DbType",
                    ParameterNamePrefix = "@",
                    ExceptionType = typeof(MySqlException),
                    BindByName = true
                });
                var properties = new NameValueCollection
                {
                    ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz", // 配置Quartz以使用JobStoreTx
                    ["quartz.jobStore.useProperties"] = "true", // 配置AdoJobStore以将字符串用作JobDataMap值
                    ["quartz.jobStore.dataSource"] = "myDS", // 配置数据源名称
                    ["quartz.jobStore.tablePrefix"] = "QRTZ_", // quartz所使用的表，在当前数据库中的表前缀
                    ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.StdAdoDelegate, Quartz",  // 配置AdoJobStore使用的DriverDelegate
                    ["quartz.dataSource.myDS.connectionString"] = "server=shapman.cn;uid=root;pwd=@@Shapman123$$;database=quartzsample", // 配置数据库连接字符串，自己处理好连接字符串，我这里就直接这么写了
                    ["quartz.dataSource.myDS.provider"] = "mysql-custom", // 配置数据库提供程序（这里是自定义的，定义的代码在上面）
                    ["quartz.jobStore.lockHandler.type"] = "Quartz.Impl.AdoJobStore.UpdateLockRowSemaphore, Quartz",
                    ["quartz.serializer.type"] = "binary",
                    ["quartz.jobStore.clustered"] = "true",    //  指示Quartz.net的JobStore是应对 集群模式
                    ["quartz.scheduler.instanceId"] = "AUTO"
                };
                return new StdSchedulerFactory(properties);
            });

            services.AddTransient<MyJob1>();
            services.AddTransient(u => new JobSchedule(
                jobType: typeof(MyJob1),
                cronExpression: "0/10 * * * * ?")); // 10s执行一次

            services.AddTransient<MyJob2>();
            services.AddTransient(u => new JobSchedule(
                jobType: typeof(MyJob2),
                cronExpression: "0/15 * * * * ?")); // 15s执行一次

            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuartzAggregate v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
