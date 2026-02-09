using Microsoft.EntityFrameworkCore;
using MythNote.Web.Models;

namespace MythNote.Web.Git;

public class GitSyncWorker(
    ILogger<GitSyncWorker> logger,
    IServiceScopeFactory serviceScopeFactory,
    GitSyncManager gitManager)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Git 同步服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            // 关键点：在循环内创建 scope
            using (var scope = serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                try
                {
                    // 使用 AsNoTracking() 进一步优化性能，因为这里只是读取
                    var users = await dbContext.Users.ToListAsync(stoppingToken);

                    foreach (var user in users)
                    {
                        if (!string.IsNullOrEmpty(user.GitRepoUrl))
                        {
                            try
                            {
                                // 注意：如果 gitManager 修改了 user 对象并需要保存，
                                // 请确保 gitManager 使用的是当前 scope 里的 dbContext
                                gitManager.FullSync(user);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "用户 {UserId} 同步失败", user.Id);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "同步循环发生异常");
                }

                await dbContext.SaveChangesAsync(stoppingToken);
            } // scope 在这里释放，dbContext 随之销毁

            // 等待 30 分钟
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }
}