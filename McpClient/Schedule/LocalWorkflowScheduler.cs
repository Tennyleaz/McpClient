using McpClient.Models;
using McpClient.Services;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace McpClient.Schedule;

internal class LocalWorkflowScheduler
{
    private const string GROUP_NAME = "group1";
    private IScheduler scheduler;
    private MyListener myJobListener;

    public LocalWorkflowScheduler()
    {

    }

    public async Task StartAsync()
    {
        if (scheduler != null)
            return;

        string sqliteConnectionString = $"DataSource=\"{GlobalService.QuartzDbFile}\"";
        SchedulerBuilder builder = SchedulerBuilder.Create();
        builder.UsePersistentStore(store =>
        {
            // it's generally recommended to stick with
            // string property keys and values when serializing
            store.UseProperties = true;
            store.UseMicrosoftSQLite(sqliteConnectionString);
            store.UseSystemTextJsonSerializer();
        });

        // Grab the Scheduler instance from the Factory
        StdSchedulerFactory factory = builder.Build();
        scheduler = await factory.GetScheduler();

        // and start it off
        await scheduler.Start();

        // Listen to event about jobs end
        myJobListener = new MyListener();
        scheduler.ListenerManager.AddJobListener(myJobListener, GroupMatcher<JobKey>.GroupEquals(GROUP_NAME));
    }

    public async Task AddJobs(int workflowId, string data)
    {
        // define the job and tie it to our IJob class
        IJobDetail job = JobBuilder.Create<LocalWorkflowJob>()
            .WithIdentity("job1", GROUP_NAME)
            .UsingJobData("workflowId", workflowId)  // Only store primitive data types (including strings) in JobDataMap 
            .UsingJobData("data", data)              // to avoid data serialization issues
            .Build();

        // Trigger the job to run now, and then repeat every 10 seconds
        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity("trigger1", GROUP_NAME)
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(10)
                .RepeatForever())
            .Build();

        // Tell Quartz to schedule the job using our trigger
        await scheduler.ScheduleJob(job, trigger);

    }
}

internal class LocalWorkflowJob : IJob
{
    // We recommend defining a static field that allows easy access
    //public static readonly JobKey Key = new JobKey("job-name", "group-name");

    public async Task Execute(IJobExecutionContext context)
    {
        // Get data from JobDataMap
        JobKey key = context.JobDetail.Key;
        JobDataMap dataMap = context.MergedJobDataMap;

        int workflowId = dataMap.GetIntValue("workflowId");
        string data = dataMap.GetString("dataa");

        // Create service from setting
        var settings = SettingsManager.Local.Load();
        AiNexusService aiNexusService = new AiNexusService(settings.AiNexusToken);

        try
        {
            // Get the workflow
            //OfflineWorkflow workflow = await aiNexusService.GetOfflineGroupById(workflowId);

            // Run the workflow
            IAsyncEnumerable<AutogenStreamResponse> responses = aiNexusService.ExecuteOfflineWorkflow(workflowId, null, data);
            string text = string.Empty;
            await foreach (AutogenStreamResponse response in responses)
            {
                AutogenChoice choice = response.Choices?.FirstOrDefault();
                if (choice == null)
                {
                    continue;
                }

                // Gather streamed text
                text += choice.Delta.Content;
            }

            // Return the result
            context.Result = text;
        }
        catch (Exception ex)
        {
            // do you want the job to refire?
            throw new JobExecutionException(msg: $"Error executing workflow id {workflowId}", refireImmediately: false, cause: ex);
        }
    }
}

internal class MyListener : IJobListener
{
    public string Name => "MyListener";

    public async Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {

    }

    public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        
    }

    public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
    {
        // Performing large amounts of work is discouraged.
    }
}
