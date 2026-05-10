using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StokBarangMAUI.Services
{
    // Upload foto ke Google Drive via API multipart upload.
    // Hanya scope drive.file (file yang dibuat app saja).
    public class DriveUploadService
    {
        private readonly GoogleOAuthService _oauth;
        private readonly HttpClient         _http = new();

        public DriveUploadService(GoogleOAuthService oauth)
        {
            _oauth = oauth;
        }

        // Returns shareable URL on success, null on failure.
        public async Task<string?> UploadPhotoAsync(string filePath, string fileName, string? folderId = null)
        {
            if (!File.Exists(filePath)) return null;
            var token = await _oauth.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;

            try
            {
                // 1. Multipart upload (metadata + bytes)
                var metadata = new Dictionary<string, object> { ["name"] = fileName };
                if (!string.IsNullOrWhiteSpace(folderId))
                    metadata["parents"] = new[] { folderId };

                var boundary = "----MAUIBoundary_" + Guid.NewGuid().ToString("N");
                using var body = new MultipartContent("related", boundary);
                var metaContent = new StringContent(JsonSerializer.Serialize(metadata), Encoding.UTF8, "application/json");
                metaContent.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "UTF-8" };
                body.Add(metaContent);

                var bytes = await File.ReadAllBytesAsync(filePath);
                var fileContent = new ByteArrayContent(bytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(GuessMime(fileName));
                body.Add(fileContent);

                using var req = new HttpRequestMessage(HttpMethod.Post,
                    "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart")
                { Content = body };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;
                var info = JsonSerializer.Deserialize<DriveFile>(await resp.Content.ReadAsStringAsync());
                if (string.IsNullOrEmpty(info?.Id)) return null;

                // 2. Make public-readable supaya link bisa dibuka tanpa login
                using var permReq = new HttpRequestMessage(HttpMethod.Post,
                    $"https://www.googleapis.com/drive/v3/files/{info.Id}/permissions");
                permReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                permReq.Content = new StringContent(
                    "{\"role\":\"reader\",\"type\":\"anyone\"}", Encoding.UTF8, "application/json");
                await _http.SendAsync(permReq); // ignore failure; file ttp ada

                return $"https://drive.google.com/file/d/{info.Id}/view";
            }
            catch { return null; }
        }

        public class DriveFolder
        {
            [JsonPropertyName("id")]   public string Id   { get; set; } = "";
            [JsonPropertyName("name")] public string Name { get; set; } = "";
        }

        public class DriveImage
        {
            [JsonPropertyName("id")]            public string  Id           { get; set; } = "";
            [JsonPropertyName("name")]          public string  Name         { get; set; } = "";
            [JsonPropertyName("description")]   public string? Description  { get; set; }
            [JsonPropertyName("thumbnailLink")] public string? ThumbnailLink { get; set; }
            [JsonPropertyName("createdTime")]   public string? CreatedTime  { get; set; }

            // Build a direct view URL.
            public string ViewUrl  => $"https://drive.google.com/file/d/{Id}/view";
            // Direct image content URL via Drive (works after public read permission set).
            public string ImageUrl => $"https://drive.google.com/uc?export=view&id={Id}";
        }

        // List image files dalam folder, urut dari terbaru.
        public async Task<List<DriveImage>> ListImageFilesAsync(string parentId, int pageSize = 100)
        {
            if (string.IsNullOrWhiteSpace(parentId)) return new();
            var token = await _oauth.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return new();

            try
            {
                var q   = $"'{parentId}' in parents and mimeType contains 'image/' and trashed=false";
                var url = $"https://www.googleapis.com/drive/v3/files" +
                          $"?q={Uri.EscapeDataString(q)}" +
                          $"&fields=files(id,name,description,thumbnailLink,createdTime)" +
                          $"&orderBy=createdTime desc" +
                          $"&pageSize={pageSize}";

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return new();
                var doc = JsonSerializer.Deserialize<DriveImageListResponse>(await resp.Content.ReadAsStringAsync());
                return doc?.Files ?? new();
            }
            catch { return new(); }
        }

        private class DriveImageListResponse
        {
            [JsonPropertyName("files")] public List<DriveImage>? Files { get; set; }
        }

        // Upload foto + set custom description (untuk metadata absensi).
        public async Task<string?> UploadPhotoWithDescriptionAsync(
            string filePath, string fileName, string description, string? folderId = null)
        {
            if (!File.Exists(filePath)) return null;
            var token = await _oauth.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;

            try
            {
                var metadata = new Dictionary<string, object>
                {
                    ["name"] = fileName,
                    ["description"] = description ?? ""
                };
                if (!string.IsNullOrWhiteSpace(folderId))
                    metadata["parents"] = new[] { folderId };

                var boundary = "----MAUIBoundary_" + Guid.NewGuid().ToString("N");
                using var body = new MultipartContent("related", boundary);
                var metaContent = new StringContent(JsonSerializer.Serialize(metadata), Encoding.UTF8, "application/json");
                metaContent.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "UTF-8" };
                body.Add(metaContent);

                var bytes = await File.ReadAllBytesAsync(filePath);
                var fileContent = new ByteArrayContent(bytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(GuessMime(fileName));
                body.Add(fileContent);

                using var req = new HttpRequestMessage(HttpMethod.Post,
                    "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart")
                { Content = body };
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;
                var info = JsonSerializer.Deserialize<DriveFile>(await resp.Content.ReadAsStringAsync());
                if (string.IsNullOrEmpty(info?.Id)) return null;

                // Make public readable
                using var permReq = new HttpRequestMessage(HttpMethod.Post,
                    $"https://www.googleapis.com/drive/v3/files/{info.Id}/permissions");
                permReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                permReq.Content = new StringContent(
                    "{\"role\":\"reader\",\"type\":\"anyone\"}", Encoding.UTF8, "application/json");
                await _http.SendAsync(permReq);

                return info.Id;
            }
            catch { return null; }
        }

        // List subfolder di dalam parentId (folder yang tidak di-trash, urut by name).
        public async Task<List<DriveFolder>> ListFoldersAsync(string parentId)
        {
            if (string.IsNullOrWhiteSpace(parentId)) return new();
            var token = await _oauth.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return new();

            try
            {
                var q   = $"'{parentId}' in parents and mimeType='application/vnd.google-apps.folder' and trashed=false";
                var url = $"https://www.googleapis.com/drive/v3/files" +
                          $"?q={Uri.EscapeDataString(q)}" +
                          $"&fields=files(id,name)" +
                          $"&orderBy=name" +
                          $"&pageSize=200";

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return new();
                var doc = JsonSerializer.Deserialize<DriveListResponse>(await resp.Content.ReadAsStringAsync());
                return doc?.Files ?? new();
            }
            catch { return new(); }
        }

        private class DriveListResponse
        {
            [JsonPropertyName("files")] public List<DriveFolder>? Files { get; set; }
        }

        // Download file content via Drive API (pakai access token, tidak bergantung pada
        // permission public). Returns bytes atau null kalau gagal.
        public async Task<byte[]?> DownloadFileAsync(string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId)) return null;
            var token = await _oauth.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get,
                    $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;
                return await resp.Content.ReadAsByteArrayAsync();
            }
            catch { return null; }
        }

        private static string GuessMime(string fileName) =>
            Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"            => "image/png",
                ".webp"           => "image/webp",
                ".heic"           => "image/heic",
                _                 => "application/octet-stream"
            };

        private class DriveFile
        {
            [JsonPropertyName("id")]   public string? Id   { get; set; }
            [JsonPropertyName("name")] public string? Name { get; set; }
        }
    }
}
