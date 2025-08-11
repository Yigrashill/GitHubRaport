
using GitHubRaport.Infrastructure;
using GitHubRaport.Models;
using GitHubRaport.DataBase;
using GitHubRaport.Errors;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;


class Program
{
    private string _userName = string.Empty;
    private string _repository = string.Empty;
    private IReadOnlyList<SimpleCommit> _commits = new List<SimpleCommit>();

    private readonly IGitHubService _gitHubService;
    private readonly AppDbContext _dbContext;

    public Program(IGitHubService gitHubService, AppDbContext dbContext)
    {
        _gitHubService = gitHubService;
        _dbContext = dbContext;
    }

    public static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
           .ConfigureServices((context, services) =>
           {
               services.AddScoped<Program>();
               services.AddDbContext<AppDbContext>(opt =>
                    opt.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));


               services.AddHttpClient<IGitHubService, GitHubService>(client =>
               {
                   var token = context.Configuration["GitHub:Token"];
                   client.BaseAddress = new Uri("https://api.github.com/");
                   client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GithubLoaderApp", "1.0"));
                   client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
                   client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
                   client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
               });
           })
           .Build();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        using var runScope = host.Services.CreateScope();
        var app = runScope.ServiceProvider.GetRequiredService<Program>();
        await app.RunAsync();
    }

    public async Task RunAsync()
    {
        Console.WriteLine("-------------------------------------");
        Console.WriteLine("     Github loader START  ");
        Console.WriteLine("-------------------------------------\n\n");
        await ShowIntro();

        Console.WriteLine("\n=====================================");
        Console.WriteLine("   Loadding settings Process    ");
        Console.WriteLine("=====================================");
        await LoadSettingsProcess();


        Console.WriteLine("\n=====================================");
        Console.WriteLine("     Loadd User Data Process         ");
        Console.WriteLine("=====================================");
        await LoadUserDataProcces();

        Console.WriteLine("\n=====================================");
        Console.WriteLine("     Connecnt to Github Process        ");
        Console.WriteLine("=====================================");
        await LoadGithupRepository();

        Console.WriteLine("\n=====================================");
        Console.WriteLine("   Save Commits in Database  Process       ");
        Console.WriteLine("=====================================");
        await SaveDataRepository();


        Console.WriteLine("\n-------------------------------------");
        Console.WriteLine("   END OF PROGRAM   ");
        Console.WriteLine("-------------------------------------\n\n");
    }

    private async Task ShowIntro()
    {
        Console.Clear();
        Console.WriteLine("=====================================");
        Console.WriteLine(" GitHub Report — quick guide");
        Console.WriteLine("=====================================");
        Console.WriteLine("What this app does:");
        Console.WriteLine("  1) Asks for a GitHub username and a public repository.");
        Console.WriteLine("  2) Fetches commits from GitHub.");
        Console.WriteLine("  3) Prints them and saves ONLY new ones to the database.");
        Console.WriteLine();
        Console.WriteLine("Notes:");
        Console.WriteLine("  - You can rerun safely — duplicates are ignored.");
        Console.WriteLine("  - Make sure the repository is public");
        Console.WriteLine();
        Console.Write("Press Enter to continue...");
        Console.ReadLine();
        Console.Clear();
    }

    private async Task LoadSettingsProcess()
    {
        try
        {
            Console.WriteLine("Reading settings and initializing database... [START]");
            Console.WriteLine("1/3 Services injected... [OK]");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            Console.WriteLine("2/3 Configuration files loaded... [OK]");
            Console.WriteLine("3/3 Database created and migrated!... [OK]");
            Console.WriteLine("Process completed successfully [END]");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Warrning -- Cannot crate database on your SQLServer instation");
            Console.WriteLine("Please install on you local machine instance of MS SQL Server");
            Console.WriteLine($"Error while loading configuration: {ex.Message}");
        }
    }

    private async Task LoadUserDataProcces()
    {
        try
        {
            Console.WriteLine("Reading user data.... ");
            while (_userName.IsNullOrEmpty())
            {
                Console.Write("Enter GitHub username: ");
                _userName = Console.ReadLine()?.Trim();
            }

            while (_repository.IsNullOrEmpty())
            {
                Console.Write($"Enter the public repository name for user '{_userName}': ");
                _repository = Console.ReadLine()?.Trim();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private async Task LoadGithupRepository()
    {
        try  
        {
            Console.WriteLine($"Checking if user '{_userName}' exists...");
            var userExists = await _gitHubService.IsUserExist(_userName);

            Console.WriteLine($"Checking if repository '{_repository}' exists...");
            var repoExists = await _gitHubService.IsRepositoryExist(_userName, _repository);

            if (userExists || repoExists)
            {

                _commits = await _gitHubService.GetAllCommitsAsync(_userName, _repository);

                if (_commits.IsNullOrEmpty())
                    throw new Exception("     Dont have any commits");
            }
            else
            {
                throw new NotFoundRepositoryException(_repository);
            }

            foreach (var commit in _commits)
                Console.WriteLine($"{_repository}/{commit.Sha}: {commit.Message} [{commit.Commiter}]");

        }
        catch (Exception ex)
        {

            Console.WriteLine(ex.Message);

        }
    }


    private async Task SaveDataRepository()
    {
        if (_commits == null || _commits.Count == 0)
        {
            Console.WriteLine("     No new commits to save.");
            return;
        }

        var incomingShas = _commits.Select(c => c.Sha).ToList();

        var existingShas = await _dbContext.Commits
      .Where(c => c.UserName == _userName
               && c.Repository == _repository
               && incomingShas.Contains(c.Sha))
      .Select(c => c.Sha)
      .ToListAsync();


        var toInsert = _commits
      .Where(c => !existingShas.Contains(c.Sha))
      .Select(c => new CommitEntity
      {
          UserName = _userName,
          Repository = _repository,
          Sha = c.Sha,
          Message = c.Message,
          Committer = c.Commiter
      })
      .ToList();

        if (toInsert.Count == 0)
        {
            Console.WriteLine("     No new commits to save.");
            return;
        }

        await _dbContext.Commits.AddRangeAsync(toInsert);
        var saved = await _dbContext.SaveChangesAsync();

        Console.WriteLine($"Saved {saved} new commit(s).");
    }
}
