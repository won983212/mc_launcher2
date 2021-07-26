using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Minecraft_Launcher_2.Updater
{
    internal class FileObj
    {
        public FileObj(JProperty p)
        {
            FilePath = p.Name;
            Hash = (string)p.Value["hash"];
            Size = (long)p.Value["size"];
        }

        public string FilePath { get; set; }
        public string Hash { get; set; }
        public long Size { get; set; }

        public string GetActualPath(string parent, bool useHashPath)
        {
            return Path.Combine(parent, useHashPath ? Hash.Substring(0, 2) + "\\" + Hash : FilePath);
        }
    }

    public class HashDownloader
    {
        private const int RetryCount = 3;
        private readonly string _indexesURL;
        private readonly string _resourceUrl;
        private readonly string _savePath;

        private readonly CancellationTokenSource _tknSrc = new CancellationTokenSource();
        private int _count;
        private int _failed;
        private volatile bool _isCanceling;
        private volatile bool _isRunning;
        private int _total;


        public HashDownloader(string savePath, string indexesURL, string resourceUrl)
        {
            _savePath = savePath;
            _indexesURL = indexesURL;
            _resourceUrl = resourceUrl;
        }


        /** 
         * 저장 경로를 Hash 형태로 저장할 건지?
         */
        public bool UseHashPath { get; set; } = false;

        /** 
         * Hash 검사를 통해 업데이트가 필요한 파일만 다운로드할 건지?
         */
        public bool DownloadOnlyNecessary { get; set; } = true;

        /** 
         * 삭제란? 서버의 파일 리스트와 비교하여 서버에 없는 파일은 삭제하는 것.
         * 이 필드는 삭제를 지원하는 폴더 리스트 정의. null일 경우 없는 것으로 판단
         */
        public string[] DetectDeletionFolder { get; set; } = null;

        public event EventHandler<ProgressArgs> OnProgress;


        public void Cancel()
        {
            if (_isRunning)
            {
                _tknSrc.Cancel();
                _isCanceling = true;
                UpdateStatus(_count * 100.0 / _total, "취소하고 있습니다..");
            }
        }

        public async Task<int> DownloadTask()
        {
            if (!_isRunning)
            {
                _isRunning = true;
                _count = 0;
                _failed = 0;
                return await Task.Run(Download);
            }

            return 0;
        }

        private void UpdateStatus(double progress, string status)
        {
            OnProgress?.Invoke(this, new ProgressArgs(progress, status));
        }

        private bool CheckHash(SHA1Managed sha1, string parent, FileObj file)
        {
            var path = file.GetActualPath(parent, UseHashPath);
            if (!File.Exists(path))
                return false;
            if (new FileInfo(path).Length != file.Size)
                return false;

            var sb = new StringBuilder(40);
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var hash = sha1.ComputeHash(fs);
                foreach (var b in hash)
                    sb.AppendFormat("{0:X2}", b);
            }

            return string.Equals(sb.ToString(), file.Hash, StringComparison.OrdinalIgnoreCase);
        }

        private void ProcessFileSyncDelete(JObject objects, string parentFolder, string path)
        {
            foreach (var dir in Directory.EnumerateDirectories(path))
                ProcessFileSyncDelete(objects, parentFolder, dir);

            foreach (var file in Directory.EnumerateFiles(path))
            {
                var key = file.Substring(parentFolder.Length + 1).Replace('\\', '/');
                if (!objects.ContainsKey(key))
                {
                    Logger.Debug("Delete " + file);
                    File.Delete(file);
                }
            }

            if (!Directory.EnumerateFileSystemEntries(path).Any())
            {
                Directory.Delete(path);
                Logger.Debug("Delete " + path);
            }
        }

        private int Download()
        {
            UpdateStatus(0, "Index파일 다운로드 중..");
            var indexData = new WebClient().DownloadString(_indexesURL);
            var parentFolder = _savePath;

            if (UseHashPath)
            {
                var indexesFolder = Path.Combine(_savePath, "indexes");
                parentFolder = Path.Combine(_savePath, "objects");

                if (!Directory.Exists(indexesFolder))
                    Directory.CreateDirectory(indexesFolder);

                File.WriteAllText(Path.Combine(indexesFolder, Path.GetFileName(_indexesURL)), indexData);
            }

            if (!Directory.Exists(parentFolder))
                Directory.CreateDirectory(parentFolder);

            var indexDataJson = JObject.Parse(indexData);
            var sha1 = new SHA1Managed();
            var files = new List<FileObj>();

            UpdateStatus(0, "다운로드해야 할 파일 검색 중..");
            var objects = indexDataJson["objects"];
            double lastProgress = 0;

            Interlocked.Exchange(ref _total, objects.Count());
            foreach (var token in objects)
            {
                if (token is JProperty p)
                {
                    var file = new FileObj(p);
                    if (DownloadOnlyNecessary && CheckHash(sha1, parentFolder, file))
                    {
                        Interlocked.Increment(ref _count);
                        lastProgress = _count * 100.0 / _total;
                        UpdateStatus(lastProgress, "다운로드해야 할 파일 검색 중..");
                    }
                    else
                    {
                        Logger.Debug("Add Download list: " + file.FilePath);
                        files.Add(file);
                    }
                }
            }

            UpdateStatus(lastProgress, "삭제해야 할 파일 검색 중..");
            if (DetectDeletionFolder != null && DetectDeletionFolder.Length > 0)
            {
                foreach (var folder in DetectDeletionFolder)
                {
                    var path = Path.Combine(parentFolder, folder);
                    if (!Directory.Exists(path))
                        continue;
                    ProcessFileSyncDelete((JObject)objects, parentFolder, path);
                }
            }

            sha1.Dispose();
            UpdateStatus(lastProgress, "다운로드 중..");

            try
            {
                Parallel.ForEach(files,
                    new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = _tknSrc.Token },
                    file => DownloadFile(parentFolder, file));
            }
            catch (OperationCanceledException)
            {
                _isRunning = false;
                _isCanceling = false;
            }

            return _failed;
        }

        private void DownloadFile(string path, FileObj file)
        {
            int i;
            for (i = 0; i < RetryCount; i++)
            {
                BinaryReader reader = null;
                BinaryWriter writer = null;
                try
                {
                    var _path = file.GetActualPath(path, UseHashPath);

                    var parent = Directory.GetParent(_path);
                    if (!parent.Exists)
                        Directory.CreateDirectory(parent.FullName);

                    var fileServerPath = file.FilePath;
                    if (UseHashPath)
                        fileServerPath = file.Hash.Substring(0, 2) + "/" + file.Hash;

                    var downloadUrl = Path.Combine(_resourceUrl, fileServerPath);
                    var s = WebRequest.Create(downloadUrl).GetResponse().GetResponseStream();

                    reader = new BinaryReader(s);
                    writer = new BinaryWriter(new FileStream(_path, FileMode.Create));
                    Logger.Debug("Download: " + _path);

                    var buf = new byte[1024];
                    int len;
                    while ((len = reader.Read(buf, 0, buf.Length)) > 0)
                        writer.Write(buf, 0, len);

                    Interlocked.Increment(ref _count);
                    Logger.Debug("Finish: " + _path);

                    UpdateStatus(_count * 100.0 / _total, _isCanceling ? "취소하고 있습니다.." : "다운로드 중..");
                    if (_count == _total)
                        _isRunning = false;
                    break;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    Logger.Info("Retry download(" + (i + 1) + "): " + file.FilePath);
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                    if (writer != null)
                        writer.Close();
                }
            }

            if (i == RetryCount)
                Interlocked.Increment(ref _failed);
        }
    }
}