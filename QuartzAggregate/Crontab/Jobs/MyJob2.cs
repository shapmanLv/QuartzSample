using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuartzAggregate.Crontab.Jobs
{
    public class MyJob2 : IJob
    {
        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger _logger;

        public MyJob2(ILogger<MyJob2> logger)
            => _logger = logger;

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation(DateTime.Now.ToString() + "  执行了我，我是 【job2】");

            return Task.CompletedTask;
        }
    }
}
