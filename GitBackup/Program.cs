using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {

        // Read configuration from config.json
        Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

        string gitHubOrganization = config.GitHubOrganization;
        string backupParentDirectory = config.BackupParentDirectory;
        int retentionDays = config.RetentionDays;
        string accessToken = config.AccessToken;

        // Set up HttpClient with authentication headers
        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "My-App");

        // Create a backup directory with the current date and time as the folder name
        string backupDirectory = Path.Combine(backupParentDirectory, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

        // Make API request to get the repositories
        string repositoriesUrl = $"https://api.github.com/orgs/{gitHubOrganization}/repos";
        HttpResponseMessage response = await httpClient.GetAsync(repositoriesUrl);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();

        // Deserialize the JSON response
        dynamic repositories = Newtonsoft.Json.JsonConvert.DeserializeObject(responseBody);

        // Iterate over the repositories and clone each one
        foreach (var repository in repositories)
        {
            string cloneUrl = repository.clone_url;
            string repositoryName = repository.name;

            string repositoryPath = Path.Combine(backupDirectory, repositoryName);

            Console.WriteLine($"Cloning repository: {repositoryName}");

            // Clone the repository using Git command-line (git.exe must be accessible from the command line)
            RunCommand($"git clone {cloneUrl} {repositoryPath}");
        }

        Console.WriteLine($"Backup complete. Saved in: {backupDirectory}");

        // Delete backups older than retentionDays
        CleanupOldBackups(backupParentDirectory, retentionDays);

        //Console.ReadLine();
    }

    static void RunCommand(string command)
    {
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
        {
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            FileName = "cmd.exe",
            Arguments = $"/C {command}"
        };
        process.StartInfo = startInfo;
        process.Start();
        process.WaitForExit();
    }

    static void CleanupOldBackups(string backupParentDirectory, int retentionDays)
    {
        string[] backupDirectories = Directory.GetDirectories(backupParentDirectory);

        foreach (var backupDir in backupDirectories)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(backupDir);
            if ((DateTime.Now - directoryInfo.CreationTime).Days > retentionDays)
            {
                Console.WriteLine($"Deleting old backup: {backupDir}");
                Directory.Delete(backupDir, true);
            }
        }
    }
}