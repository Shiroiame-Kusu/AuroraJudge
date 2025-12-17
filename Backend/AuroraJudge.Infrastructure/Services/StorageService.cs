using AuroraJudge.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace AuroraJudge.Infrastructure.Services;

public class StorageService : IStorageService
{
    private readonly IMinioClient? _minioClient;
    private readonly string _bucketName;
    private readonly string _localBasePath;
    private readonly bool _useLocalStorage;
    
    public StorageService(IConfiguration configuration)
    {
        var storageType = configuration["Storage:Type"] ?? "Local";
        _useLocalStorage = storageType.Equals("Local", StringComparison.OrdinalIgnoreCase);
        _localBasePath = configuration["Storage:LocalPath"] ?? "./storage";
        _bucketName = configuration["Storage:Minio:BucketName"] ?? "aurorajudge";
        
        if (!_useLocalStorage)
        {
            var endpoint = configuration["Storage:Minio:Endpoint"] ?? "localhost:9000";
            var accessKey = configuration["Storage:Minio:AccessKey"] ?? "";
            var secretKey = configuration["Storage:Minio:SecretKey"] ?? "";
            var useSSL = configuration.GetValue<bool>("Storage:Minio:UseSSL", false);
            
            _minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(useSSL)
                .Build();
        }
    }
    
    public async Task UploadAsync(string path, Stream content, CancellationToken cancellationToken = default)
    {
        if (_useLocalStorage)
        {
            var fullPath = Path.Combine(_localBasePath, path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            await using var fileStream = File.Create(fullPath);
            await content.CopyToAsync(fileStream, cancellationToken);
        }
        else if (_minioClient != null)
        {
            await EnsureBucketExistsAsync(cancellationToken);
            
            var args = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path)
                .WithStreamData(content)
                .WithObjectSize(content.Length);
            
            await _minioClient.PutObjectAsync(args, cancellationToken);
        }
    }
    
    public async Task<Stream> DownloadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_useLocalStorage)
        {
            var fullPath = Path.Combine(_localBasePath, path);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("文件不存在", path);
            }
            
            return File.OpenRead(fullPath);
        }
        else if (_minioClient != null)
        {
            var memoryStream = new MemoryStream();
            
            var args = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));
            
            await _minioClient.GetObjectAsync(args, cancellationToken);
            memoryStream.Position = 0;
            return memoryStream;
        }
        
        throw new InvalidOperationException("存储服务未配置");
    }
    
    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_useLocalStorage)
        {
            var fullPath = Path.Combine(_localBasePath, path);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        else if (_minioClient != null)
        {
            var args = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path);
            
            await _minioClient.RemoveObjectAsync(args, cancellationToken);
        }
    }
    
    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_useLocalStorage)
        {
            var fullPath = Path.Combine(_localBasePath, path);
            return File.Exists(fullPath);
        }
        else if (_minioClient != null)
        {
            try
            {
                var args = new StatObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(path);
                
                await _minioClient.StatObjectAsync(args, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        return false;
    }
    
    public Task<string> GetUrlAsync(string path, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        if (_useLocalStorage)
        {
            return Task.FromResult($"/files/{path}");
        }
        else if (_minioClient != null)
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path)
                .WithExpiry((int)(expiration?.TotalSeconds ?? 3600));
            
            return _minioClient.PresignedGetObjectAsync(args);
        }
        
        throw new InvalidOperationException("存储服务未配置");
    }
    
    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        if (_minioClient == null) return;
        
        var existsArgs = new BucketExistsArgs().WithBucket(_bucketName);
        var exists = await _minioClient.BucketExistsAsync(existsArgs, cancellationToken);
        
        if (!exists)
        {
            var makeArgs = new MakeBucketArgs().WithBucket(_bucketName);
            await _minioClient.MakeBucketAsync(makeArgs, cancellationToken);
        }
    }
}
