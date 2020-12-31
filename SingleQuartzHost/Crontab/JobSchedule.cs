using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SingleQuartzHost.Crontab
{
    /// <summary>
    /// 在定义一个定时作业计划时所需要的数据
    /// 可以看成一个dto，为job创建schedule时用的
    /// </summary>
    public class JobSchedule
    {
        public JobSchedule(Type jobType, string cronExpression)
        {
            JobType = jobType;
            CronExpression = cronExpression;
        }

        /// <summary>
        /// 作业类型
        /// </summary>
        public Type JobType { get; }
        /// <summary>
        /// cron 表达式
        /// </summary>
        public string CronExpression { get; }
    }
}
