using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuartzAggregate.Crontab
{
    /// <summary>
    /// quartz 主机服务
    /// </summary>
    [DisallowConcurrentExecution]
    public class QuartzHostedService : IHostedService
    {
        /// <summary>
        /// 定时作业计划生成工厂，这一项在startup有配置集群模式
        /// </summary>
        private readonly ISchedulerFactory _schedulerFactory;
        /// <summary>
        /// 定时作业工厂
        /// </summary>
        private readonly IJobFactory _jobFactory;
        /// <summary>
        /// 定时作业计划集合，配合dotnet core的ioc注入进来
        /// </summary>
        private readonly IEnumerable<JobSchedule> _jobSchedules;
        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger _logger;
        /// <summary>
        /// quartz scheduler
        /// </summary>
        private IScheduler _scheduler;

        /// <summary>
        /// 构造注入
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="jobFactory"></param>
        /// <param name="jobSchedules"></param>
        /// <param name="logger"></param>
        public QuartzHostedService(
            ISchedulerFactory schedulerFactory,
            IJobFactory jobFactory,
            IEnumerable<JobSchedule> jobSchedules,
            ILogger<QuartzHostedService> logger
            )
        {
            _schedulerFactory = schedulerFactory;
            _jobSchedules = jobSchedules;
            _jobFactory = jobFactory;
            _logger = logger;
        }

        /// <summary>
        /// 批量启动定时任务
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            _scheduler.JobFactory = _jobFactory;

            // 循环遍历startup里注册的作业
            foreach (var jobSchedule in _jobSchedules)
            {
                // 判断数据库中有没有记录过，有的话，quartz会自动从数据库中提取信息创建 schedule
                if (!await _scheduler.CheckExists(new JobKey(GenerateIdentity(jobSchedule, IdentityType.Job))) &&
                !await _scheduler.CheckExists(new TriggerKey(GenerateIdentity(jobSchedule, IdentityType.Trigger))))
                {
                    var job = CreateJob(jobSchedule);
                    var trigger = CreateTrigger(jobSchedule);

                    await _scheduler.ScheduleJob(job, trigger, cancellationToken);
                }
            }

            await _scheduler.Start();
        }

        /// <summary>
        /// 停止
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken)
            => await _scheduler?.Shutdown(cancellationToken);

        /// <summary>
        /// 创建定时作业
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns></returns>
        private static IJobDetail CreateJob(JobSchedule schedule)
        {
            return JobBuilder
                .Create(schedule.JobType)
                .WithIdentity(GenerateIdentity(schedule, IdentityType.Job))
                .WithDescription(schedule.CronExpression)
                .Build();
        }

        /// <summary>
        /// 创建触发器
        /// </summary>
        /// <param name="schedule"></param>
        /// <returns></returns>
        private static ITrigger CreateTrigger(JobSchedule schedule)
        {
            return TriggerBuilder
                .Create()
                .WithIdentity(GenerateIdentity(schedule, IdentityType.Trigger))
                .WithCronSchedule(schedule.CronExpression)
                .WithDescription(schedule.JobType.FullName)
                .Build();
        }

        /// <summary>
        /// 生成一个标识（类似主键的意思）
        /// </summary>
        /// <param name="schedule"></param>
        /// <param name="identityType">标识类型，一个job作业，或者是trigger触发器</param>
        /// <returns></returns>
        private static string GenerateIdentity(JobSchedule schedule, IdentityType identityType)
        {
            switch (identityType)
            {
                case IdentityType.Job:
                    return $"NdcPayInternal_Job_{schedule.JobType.Name}";
                case IdentityType.Trigger:
                    return $"NdcPayInternal_Trigger_{schedule.JobType.Name}";
            }

            return schedule.JobType.FullName;
        }

        /// <summary>
        /// 标识类型
        /// </summary>
        private enum IdentityType
        {
            Job,
            Trigger
        }
    }
}
