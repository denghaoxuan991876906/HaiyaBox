using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reflection;
using System.Xml.Linq;
using AEAssist;
using AEAssist.Extension;
using AEAssist.Helper;
using AEAssist.MemoryApi;
using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using HaiyaBox.Settings;

namespace HaiyaBox.Plugin;

public sealed class GitHubAutoUpdater : IDisposable
{
    private const string Owner = "denghaoxuan991876906";
    private const string Repo = "HaiyaBox";

    private const string PackagePrefix = "HaiyaBox-";
    private const string PackageExtension = ".zip";
    private const string ReleasesFeedUrl = $"https://github.com/{Owner}/{Repo}/releases.atom";
    private static readonly XNamespace AtomNs = "http://www.w3.org/2005/Atom";

    private static readonly HttpClient HttpClient = CreateHttpClient();
    private readonly object _lock = new();
    private DateTime _nextCheckAt = DateTime.MinValue;
    private bool _isRunning;
    private bool _disposed;

    public static GitHubAutoUpdater Instance { get; } = new();

    public string Status { get; private set; } = "未检查";
    public string? LatestTag { get; private set; }
    public string? LastError { get; private set; }
    public bool IsRunning => _isRunning;

    private AutoUpdateSettings Settings => FullAutoSettings.Instance.AutoUpdateSettings;

    public void Update()
    {
        if (_disposed || !Settings.Enabled || _isRunning) return;
        if (DateTime.UtcNow < _nextCheckAt) return;

        if (!CanUpdateNow(out var reason))
        {
            Status = $"等待安全窗口: {reason}";
            _nextCheckAt = DateTime.UtcNow.AddSeconds(30);
            return;
        }

        StartCheck(false);
    }

    public void CheckNow()
    {
        _nextCheckAt = DateTime.MinValue;
        if (_isRunning) return;

        if (!CanUpdateNow(out var reason))
        {
            Status = $"当前不能更新: {reason}";
            LogHelper.Print($"[HaiyaBox更新] {Status}");
            return;
        }

        StartCheck(true);
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private void StartCheck(bool manual)
    {
        lock (_lock)
        {
            if (_isRunning) return;
            _isRunning = true;
        }

        Status = manual ? "手动检查中..." : "自动检查中...";
        LastError = null;

        Task.Run(async () =>
        {
            try
            {
                await CheckAndUpdateAsync();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                Status = "更新失败";
                LogHelper.PrintError($"[HaiyaBox更新] {ex}");
            }
            finally
            {
                var interval = Math.Max(Settings.CheckIntervalMinutes, 5);
                _nextCheckAt = DateTime.UtcNow.AddMinutes(interval);
                _isRunning = false;
            }
        });
    }

    private async Task CheckAndUpdateAsync()
    {
        var release = await GetLatestReleaseAsync();
        if (release == null)
        {
            Status = "未找到可用发布包";
            return;
        }

        LatestTag = release.TagName;
        Settings.UpdateLastSeenTag(release.TagName);

        if (string.Equals(Settings.InstalledTag, release.TagName, StringComparison.OrdinalIgnoreCase))
        {
            Status = $"已是最新: {release.TagName}";
            return;
        }

        var asset = release.Assets.FirstOrDefault(IsPluginPackage);
        if (asset == null)
        {
            Status = $"发布 {release.TagName} 没有 zip 包";
            return;
        }

        LogHelper.Print($"[HaiyaBox更新] 发现新版本 {release.TagName}，开始下载 {asset.Name}");
        Status = $"下载中: {release.TagName}";

        var packagePath = await DownloadAssetAsync(release.TagName, asset);
        Status = $"安装中: {release.TagName}";

        var result = InstallPackage(packagePath, release.TagName);
        if (result)
        {
            Settings.UpdateInstalledTag(release.TagName);
            Status = $"已更新: {release.TagName}";
            LogHelper.Print($"[HaiyaBox更新] 已安装 {release.TagName}，建议重载插件确认新版本生效。");
        }
    }

    private async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        using var response = await HttpClient.GetAsync(ReleasesFeedUrl);
        response.EnsureSuccessStatusCode();

        var xml = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(xml);

        foreach (var entry in doc.Descendants(AtomNs + "entry"))
        {
            var id = entry.Element(AtomNs + "id")?.Value;
            if (string.IsNullOrWhiteSpace(id)) continue;

            var tagName = id[(id.LastIndexOf('/') + 1)..];
            if (string.IsNullOrWhiteSpace(tagName)) continue;

            DateTimeOffset.TryParse(entry.Element(AtomNs + "published")?.Value, out var published);

            var asset = new GitHubAsset
            {
                Name = $"{PackagePrefix}{tagName}{PackageExtension}",
                BrowserDownloadUrl = $"https://github.com/{Owner}/{Repo}/releases/download/{tagName}/{PackagePrefix}{tagName}{PackageExtension}"
            };

            return new GitHubRelease
            {
                TagName = tagName,
                Prerelease = true,
                PublishedAt = published,
                Assets = [asset]
            };
        }

        return null;
    }

    private async Task<string> DownloadAssetAsync(string tagName, GitHubAsset asset)
    {
        var updateDir = GetUpdateDirectory(tagName);
        Directory.CreateDirectory(updateDir);

        var packagePath = Path.Combine(updateDir, asset.Name);
        using var response = await HttpClient.GetAsync(asset.BrowserDownloadUrl);
        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync();
        await using var target = File.Create(packagePath);
        await source.CopyToAsync(target);
        return packagePath;
    }

    private bool InstallPackage(string packagePath, string tagName)
    {
        var pluginDir = GetPluginDirectory();
        var updateDir = GetUpdateDirectory(tagName);
        var extractDir = Path.Combine(updateDir, "extract");

        if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);

        ZipFile.ExtractToDirectory(packagePath, extractDir);
        var dllPath = Directory.GetFiles(extractDir, "HaiyaBox.dll", SearchOption.AllDirectories).FirstOrDefault();
        if (dllPath == null)
        {
            Status = "更新包缺少 HaiyaBox.dll";
            LogHelper.PrintError($"[HaiyaBox更新] {Status}: {packagePath}");
            return false;
        }

        try
        {
            foreach (var sourcePath in Directory.GetFiles(Path.GetDirectoryName(dllPath)!, "*",
                         SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(sourcePath);
                var targetPath = Path.Combine(pluginDir, fileName);
                var backupPath = Path.Combine(updateDir, $"{fileName}.bak");

                if (File.Exists(targetPath)) File.Copy(targetPath, backupPath, true);

                File.Copy(sourcePath, targetPath, true);
            }

            return true;
        }
        catch (IOException ex)
        {
            LastError = ex.Message;
            Status = "文件被占用，已保留更新包";
            LogHelper.PrintError($"[HaiyaBox更新] 插件文件被占用，更新包已保存到 {updateDir}，请重启后处理。错误: {ex.Message}");
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            LastError = ex.Message;
            Status = "没有写入权限，已保留更新包";
            LogHelper.PrintError($"[HaiyaBox更新] 无法写入插件目录，更新包已保存到 {updateDir}。错误: {ex.Message}");
            return false;
        }
    }

    private static bool IsPluginPackage(GitHubAsset asset)
    {
        return asset.Name.StartsWith(PackagePrefix, StringComparison.OrdinalIgnoreCase)
               && asset.Name.EndsWith(PackageExtension, StringComparison.OrdinalIgnoreCase)
               && !string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl);
    }

    private static string GetPluginDirectory()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var pluginDir = Path.GetDirectoryName(assemblyLocation);
        if (!string.IsNullOrWhiteSpace(pluginDir)) return pluginDir;

        return Share.CurrentDirectory;
    }

    private static string GetUpdateDirectory(string tagName)
    {
        var safeTag = string.Join("_",
            tagName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return Path.Combine(GetPluginDirectory(), "updates", safeTag);
    }

    private static bool CanUpdateNow(out string reason)
    {
        try
        {
            if (Svc.Condition[ConditionFlag.BetweenAreas])
            {
                reason = "黑屏/切区中";
                return false;
            }

            if (Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent])
            {
                reason = "黑屏事件中";
                return false;
            }

            if (Core.Me.InCombat())
            {
                reason = "战斗中";
                return false;
            }

            if (Core.Resolve<MemApiDuty>().IsBoundByDuty())
            {
                reason = "在副本中";
                return false;
            }

            reason = "可更新";
            return true;
        }
        catch (Exception ex)
        {
            reason = ex.Message;
            return false;
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HaiyaBox", GetCurrentVersion()));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/atom+xml"));
        client.Timeout = TimeSpan.FromSeconds(30);
        return client;
    }

    private static string GetCurrentVersion()
    {
        try
        {
            var location = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrWhiteSpace(location))
            {
                var version = FileVersionInfo.GetVersionInfo(location).FileVersion;
                if (!string.IsNullOrWhiteSpace(version)) return version;
            }
        }
        catch
        {
            // User-Agent still needs a valid product version when assembly metadata is unavailable.
        }

        return "1.0.0";
    }

    private sealed class GitHubRelease
    {
        public string TagName { get; set; } = "";
        public bool Prerelease { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public List<GitHubAsset> Assets { get; set; } = [];
    }

    private sealed class GitHubAsset
    {
        public string Name { get; set; } = "";
        public string BrowserDownloadUrl { get; set; } = "";
    }
}