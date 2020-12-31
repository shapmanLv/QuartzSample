using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuartzAggregate.Crontab
{
    public class JobFactory : IJobFactory
    {
        /// <summary>
        /// ioc
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 构造注入
        /// </summary>
        /// <param name="serviceProvider"></param>
        public JobFactory(IServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

        /// <summary>
        /// 按照startup里批量注册的job，创建一个指定类型的job
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var serviceScope = _serviceProvider.CreateScope(); // 获得一个ioc对象，指定创建scope级别的实例（在job里面需要依赖注入ef，但是startup里面配置的ef是scope级别的，必须指定为scope，不然报错）。不写的话，默认是单例。
            return serviceScope.ServiceProvider.GetService(bundle.JobDetail.JobType) as IJob; // 依赖注入一个 job 然后返回
        }

        public void ReturnJob(IJob job) { }
    }
}
