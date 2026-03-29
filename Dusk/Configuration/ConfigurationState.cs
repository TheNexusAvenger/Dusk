using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Dusk.Diagnostic;

namespace Dusk.Configuration;

public class ConfigurationState<T> where T : BaseConfiguration
{
    /// <summary>
    /// Event for the configuration changing.
    /// </summary>
    public event Action<T>? ConfigurationChanged;
    
    /// <summary>
    /// Loaded configuration instance of the state.
    /// </summary>
    public T? CurrentConfiguration { get; private set; }
    
    /// <summary>
    /// Path of the configuration.
    /// </summary>
    private readonly string _configurationPath;

    /// <summary>
    /// JSON type information for the configuration.
    /// </summary>
    private readonly JsonTypeInfo<T> _configurationJsonType;

    /// <summary>
    /// Default configuration to store when no configuration file exists.
    /// </summary>
    private readonly T _defaultConfiguration;

    /// <summary>
    /// Last configuration as JSON.
    /// </summary>
    private string? _lastConfiguration = null;
    
    /// <summary>
    /// Creates a configuration state.
    /// </summary>
    /// <param name="fileName">Name of the configuration file.</param>
    /// <param name="defaultConfiguration">Default configuration to store when no configuration file exists.</param>
    /// <param name="configurationJsonType">JSON type information for the configuration.</param>
    public ConfigurationState(string fileName, T defaultConfiguration, JsonTypeInfo<T> configurationJsonType)
    {
        // Create the configuration directory.
        var configurationDirectory = Environment.GetEnvironmentVariable("DUSK_CONFIGURATION_DIRECTORY") ?? Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dusk");
        if (!Directory.Exists(configurationDirectory))
        {
            Directory.CreateDirectory(configurationDirectory);
        }
        
        // Load the initial configuration.
        this._configurationPath = Path.Combine(configurationDirectory, fileName);;
        this._defaultConfiguration = defaultConfiguration;
        this._configurationJsonType = configurationJsonType;
        this.ReloadAsync().Wait();
        
        // Set up file change notifications.
        var fileSystemWatcher = new FileSystemWatcher(Directory.GetParent(this._configurationPath)!.FullName);
        fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
        fileSystemWatcher.Changed += async (_, _) => await this.TryReloadAsync();
        fileSystemWatcher.EnableRaisingEvents = true;
        
        // Occasionally reload the file in a loop.
        // File change notifications don't seem to work in Docker with volumes.
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(10000);
                await this.TryReloadAsync();
            }
        });
    }
    
    /// <summary>
    /// Reloads the configuration.
    /// </summary>
    public async Task ReloadAsync()
    {
        // Prepare the configuration if it doesn't exist.
        var path = this._configurationPath;
        if (!File.Exists(path))
        {
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(this._defaultConfiguration, this._configurationJsonType));
        }
        
        // Read the configuration.
        var configurationContents = await File.ReadAllTextAsync(path);
        this.CurrentConfiguration = JsonSerializer.Deserialize<T>(configurationContents, this._configurationJsonType)!;
        
        // Invoke the changed event if the contents changed.
        if (this._lastConfiguration != null && this._lastConfiguration != configurationContents)
        {
            Logger.Debug("Configuration updated.");
            ConfigurationChanged?.Invoke(this.CurrentConfiguration);
        }
        this._lastConfiguration = configurationContents;
    }

    /// <summary>
    /// Tries to reload the configuration.
    /// No exception is thrown if it fails.
    /// </summary>
    public async Task TryReloadAsync()
    {
        try
        {
            await this.ReloadAsync();
        }
        catch (Exception e)
        {
            Logger.Debug($"An error occured trying to update the configuration. This might be due to a text editor writing the file.\n{e}");
        }
    }
}