using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GoogleDriveApi {
    public class DriveApi {
        /// <summary>
        /// Массив ошибок
        /// </summary>
        public static List<Exception> _exceptions { get; } = new List<Exception>();
        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public static string _messageError { get; set; } = "error";
        /// <summary>
        /// Объект подключения
        /// </summary>
        DriveService Service { get; set; }
        /// <summary>
        /// Подключение по Апи к диску
        /// </summary>
        /// <param name="applicationName">Имя проэкта</param>
        /// <param name="pathToFile">Путь к файлу аунтификации</param>
        public DriveApi(string applicationName, string pathToFile) {
            UserCredential credential;
            using (var stream = new System.IO.FileStream(pathToFile, System.IO.FileMode.Open, System.IO.FileAccess.Read)) {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new List<string> { DriveService.Scope.Drive, DriveService.Scope.DriveAppdata },
                    "user", CancellationToken.None).Result;
            }
            Service = new DriveService(new BaseClientService.Initializer() {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });
        }
        /// <summary>
        /// Получить указаное количество файлов
        /// </summary>
        /// <param name="size">[1-1000]</param>
        /// <returns>Возвращает состояние выполнения и массив файлов</returns>
        public (string, List<File>) GetFile(int size) {
            List<File> files = new List<File>();
            string result = "good";
            try {
                FilesResource.ListRequest listRequest = Service.Files.List();
                listRequest.PageSize = size;
                listRequest.Fields = "nextPageToken, files(id, name)";
                files = listRequest.Execute().Files.ToList();
            }
            catch (Exception ex) { Error(ref result, ex); }
            return (result, files);
        }
        /// <summary>
        /// Найти папку
        /// </summary>
        /// <param name="nameDirectory">Навзвание папки</param>
        /// <returns>Возвращает состояние выполнения и массив папок</returns>
        public (string, List<File>) SearchDirectory(string nameDirectory) {
            List<File> files = new List<File>();
            string result = "good";
            try {
                FilesResource.ListRequest listRequest = Service.Files.List();
                listRequest.Q = $"mimeType = 'application/vnd.google-apps.folder' and name = '{nameDirectory}'";
                listRequest.PageToken = null;
                listRequest.Fields = "nextPageToken, files(id, name)";
                files = listRequest.Execute().Files.ToList();
            }
            catch (Exception ex) { Error(ref result, ex); }
            return (result, files);
        }
        /// <summary>
        /// Создать папку
        /// </summary>
        /// <param name="nameDirectory">Навзвание папки</param>
        /// <returns>Возвращает состояние выполнения и id созданой папки</returns>
        public (string, string) CreateDirectory(string nameDirectory) {
            string result = "good";
            string id = "";
            try {
                File fileMetadata = new File();
                fileMetadata.Name = nameDirectory;
                fileMetadata.MimeType = "application/vnd.google-apps.folder";
                var file = Service.Files.Create(fileMetadata);
                file.Fields = "id";
                id = file.Execute().Id;
            }
            catch (Exception ex) { Error(ref result, ex); }
            return (result, id);
        }
        /// <summary>
        /// Отправить массив фото в гугл диск
        /// </summary>
        /// <param name="idDirectory">Папка для хранения</param>
        /// <param name="urlFiles">Массив ссылок на фото</param>
        /// <returns>Возвращает состояние выполнения</returns>
        public string CreateFile(string idDirectory, List<string> urlFiles) {
            string result = "good";
            for (int photo = 0; photo < urlFiles.Count(); photo++)
                for (int error = 0; error < 5; error++) try {
                        result = CreateFile(idDirectory, $"photo_{photo}.jpg", urlFiles[photo]);
                        if (result.IndexOf(_messageError) == -1)
                            break;
                    }
                    catch (Exception ex) { Error(ref result, ex); }
            return result;
        }
        /// <summary>
        /// Отправить массив фото в гугл диск
        /// </summary>
        /// <param name="idDirectory">Папка для хранения</param>
        /// <param name="photos">Массив фото</param>
        /// <returns>Возвращает состояние выполнения</returns>
        public string CreateFile(string idDirectory, List<System.IO.Stream> photos) {
            string result = "good";
            for (int photo = 0; photo < photos.Count(); photo++)
                for (int error = 0; error < 5; error++) try {
                        result = CreateFile(idDirectory, $"photo_{photo}.jpg", photos[photo]);
                        if (result.IndexOf(_messageError) == -1)
                            break;
                    }
                    catch (Exception ex) { Error(ref result, ex); }
            return result;
        }
        /// <summary>
        /// Отправить фото
        /// </summary>
        /// <param name="idDirectory">Папка для хранения</param>
        /// <param name="nameFile">Название файла</param>
        /// <param name="urlFile">Ссылка на фото</param>
        /// <returns>Возвращает состояние выполнения</returns>
        public string CreateFile(string idDirectory, string nameFile, string urlFile) {
            string result = "good";
            try {
                File fileMetadata = new File();
                fileMetadata.Name = nameFile;
                fileMetadata.Parents = new List<string> { idDirectory };
                var file = Service.Files.Create(fileMetadata, GetStreamFromUrl(urlFile), "application/octet-stream");
                file.Fields = "id, parents";
                file.Upload();
            }
            catch (Exception ex) { Error(ref result, ex); }
            return result;
        }
        /// <summary>
        /// Отправить фото
        /// </summary>
        /// <param name="idDirectory">Папка для хранения</param>
        /// <param name="nameFile">Название файла</param>
        /// <param name="photo">Фото</param>
        /// <returns>Возвращает состояние выполнения</returns>
        public string CreateFile(string idDirectory, string nameFile, System.IO.Stream photo) {
            string result = "good";
            try {
                File fileMetadata = new File();
                fileMetadata.Name = nameFile;
                fileMetadata.Parents = new List<string> { idDirectory };
                var file = Service.Files.Create(fileMetadata, photo, "application/octet-stream");
                file.Fields = "id, parents";
                file.Upload();
            }
            catch (Exception ex) { Error(ref result, ex); }
            return result;
        }
        /// <summary>
        /// Получить stream по ссылке
        /// </summary>
        /// <param name="url">Ссылка</param>
        /// <returns>Возвращает stream</returns>
        private System.IO.Stream GetStreamFromUrl(string url) {
            using (var wc = new System.Net.WebClient())
                return new System.IO.MemoryStream(wc.DownloadData(url));
        }
        /// <summary>
        /// Обработчик ошибок
        /// </summary>
        /// <param name="result">строка состояния</param>
        /// <param name="ex">ошибка</param>
        private void Error(ref string result, Exception ex) {
            _exceptions.Add(ex);
            result = $"{_messageError}\n{ex.Message}\n{ex.StackTrace}";
        }
    }
}
