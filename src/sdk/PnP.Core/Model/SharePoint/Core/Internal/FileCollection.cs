using PnP.Core.QueryModel;
using PnP.Core.Services;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PnP.Core.Model.SharePoint
{
    internal partial class FileCollection : QueryableDataModelCollection<IFile>, IFileCollection
    {
        private const int maxFileSizeForRegularUpload = 10 * 1024 * 1024;

        #region Construction
        public FileCollection(PnPContext context, IDataModelParent parent, string memberName) : base(context, parent, memberName)
        {
            PnPContext = context;
            Parent = parent;
        }
        #endregion

        #region Add
        public async Task<IFile> AddAsync(string name, Stream content, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var newFile = CreateNewAndAdd() as File;
            newFile.Name = name;

            if (content.Length <= maxFileSizeForRegularUpload)
            {
                newFile = await FileUpload(newFile, content, overwrite).ConfigureAwait(false);
            }
            else
            {
                newFile = await ChunkedFileUpload(newFile, content, overwrite).ConfigureAwait(false);
            }

            return newFile;
        }

        public IFile Add(string name, Stream content, bool overwrite = false)
        {
            return AddAsync(name, content, overwrite).GetAwaiter().GetResult();
        }

        public async Task<IFile> AddTemplateFileAsync(string serverRelativePageName, TemplateFileType templateFileType)
        {
            if (string.IsNullOrEmpty(serverRelativePageName))
            {
                throw new ArgumentNullException(nameof(serverRelativePageName));
            }

            var newFile = CreateNewAndAdd() as File;
            string fileCreateRequest = $"_api/web/getFolderById('{{Parent.Id}}')/files/AddTemplateFile(urlOfFile='{serverRelativePageName}',templateFileType={(int)templateFileType})";
            var api = new ApiCall(fileCreateRequest, ApiType.SPORest);
            await newFile.RequestAsync(api, HttpMethod.Post).ConfigureAwait(false);
            return newFile;
        }

        public IFile AddTemplateFile(string serverRelativePageName, TemplateFileType templateFileType)
        {
            return AddTemplateFileAsync(serverRelativePageName, templateFileType).GetAwaiter().GetResult();
        }
        #endregion

        #region File upload
        private static async Task<File> FileUpload(File newFile, Stream content, bool overwrite)
        {
            string fileCreateRequest = $"_api/web/getFolderById('{{Parent.Id}}')/files/add(url='{newFile.Name}',overwrite={overwrite.ToString().ToLowerInvariant()})";
            var api = new ApiCall(fileCreateRequest, ApiType.SPORest)
            {
                Interactive = true,
                BinaryBody = ToByteArray(content)
            };
            await newFile.RequestAsync(api, HttpMethod.Post).ConfigureAwait(false);
            return newFile;
        }

        private static async Task<File> ChunkedFileUpload(File newFile, Stream content, bool overwrite)
        {
            // 10 MB chunks
            int chunkSizeBytes = 10 * 1024 * 1024;

            // Upload the file in chunks
            var firstChunk = true;
            var uploadId = Guid.NewGuid();
            var offset = 0L;

            var buffer = new byte[chunkSizeBytes];
            int bytesRead;
            while ((bytesRead = content.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (firstChunk)
                {
                    // Add empty file
                    string fileCreateRequest = $"_api/web/getFolderById('{{Parent.Id}}')/files/add(url='{newFile.Name}',overwrite={overwrite.ToString().ToLowerInvariant()})";
                    var api = new ApiCall(fileCreateRequest, ApiType.SPORest)
                    {
                        Interactive = true
                    };
                    await newFile.RequestAsync(api, HttpMethod.Post).ConfigureAwait(false);

                    // Add first chunk
                    var endpointUrl = $"_api/web/getFileById('{{Id}}')/startupload(uploadId=guid'{uploadId}')";
                    api = new ApiCall(endpointUrl, ApiType.SPORest)
                    {
                        Interactive = true,
                        BinaryBody = buffer
                    };
                    await newFile.RequestAsync(api, HttpMethod.Post).ConfigureAwait(false);
                    firstChunk = false;
                }
                else if (content.Position == content.Length)
                {
                    // Finalize upload
                    var finalBuffer = new byte[bytesRead];
                    Array.Copy(buffer, finalBuffer, finalBuffer.Length);

                    var endpointUrl = $"_api/web/getFileById('{{Id}}')/finishupload(uploadId=guid'{uploadId}',fileOffset={offset})";
                    var api = new ApiCall(endpointUrl, ApiType.SPORest)
                    {
                        Interactive = true,
                        BinaryBody = finalBuffer
                    };
                    await newFile.RequestAsync(api, HttpMethod.Post).ConfigureAwait(false);
                }
                else
                {
                    var endpointUrl = $"_api/web/getFileById('{{Id}}')/continueupload(uploadId=guid'{uploadId}',fileOffset={offset})";
                    var api = new ApiCall(endpointUrl, ApiType.SPORest)
                    {
                        Interactive = true,
                        BinaryBody = buffer
                    };
                    await newFile.RequestAsync(api, HttpMethod.Post).ConfigureAwait(false);
                }

                offset += bytesRead;
            }

            return newFile;
        }

        private static byte[] ToByteArray(Stream source)
        {
            using (var memoryStream = new MemoryStream())
            {
                source.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
        #endregion
    }
}
