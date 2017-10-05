﻿using System;
using System.Linq;
using TranslationAssistant.TranslationServices.Core;
using TranslationAssistant.Business;
using Autofac;
using TestApp.DocumentManagement.Model;
using TestApp.Utils;
using MicrosoftGraph.Services;
using System.Threading.Tasks;

namespace TestApp.DocumentManagement.Services
{
    /// <summary>
    /// Service managing various files locally required for translating and referencing
    /// </summary>
    public class DocumentManagementService : IDocumentManagementService
    {
        private readonly IStorageManagementService _storageManagementService;
        private readonly ISharePointManagementService _sharePointManagementService;
        private readonly IConfigurationService _configurationService;
        private readonly ILoggingService _loggingService;


        /// <summary>
        /// Creates instance <see cref="DocumentManagementService"/>
        /// </summary>
        /// <param name="storageManagementService">Isntance of <see cref="IStorageManagementService"/></param>
        /// <param name="sharePointManagementService">Instance of <see cref="ISharePointManagementService"/></param>
        /// <param name="configurationService">Instance of <see cref="IConfigurationService"/></param>
        /// <param name="loggingService">Instance of <see cref="ILoggingService"/></param>
        public DocumentManagementService(IStorageManagementService storageManagementService, ISharePointManagementService sharePointManagementService, IConfigurationService configurationService, ILoggingService loggingService)
        {
            _storageManagementService = storageManagementService;
            _sharePointManagementService = sharePointManagementService;
            _configurationService = configurationService;
            _loggingService = loggingService;
        }

        /// <summary>
        /// Tranlslates a document and provides links to the original as well as the translated document
        /// </summary>
        /// <param name="storageContainerName">Name of the storage container</param>
        /// <param name="storageFileName">Name of the storage file (original document for translation)</param>
        /// <param name="originalLanguage">The language of the originial file</param>
        /// <param name="translationLanguage">The language for translating the document</param>
        /// <returns></returns>
        public async Task<DocumentLinks> TranslateFile(string storageContainerName, string storageFileName, string originalLanguage, string translationLanguage)
        {
            try
            {
                var localFileName = await _storageManagementService.DownloadBlob(storageContainerName, storageFileName);

                // Translate File
                TranslationServiceFacade.Initialize(_configurationService.GetSettingValue("ApiKey"));

                DocumentTranslationManager.DoTranslation(localFileName, false, originalLanguage, translationLanguage);

                var languageCode = TranslationServiceFacade.AvailableLanguages.Where(p => p.Value == translationLanguage).Select(p => p.Key).FirstOrDefault();

                var extension = Helper.GetExtension(storageFileName);

                var translatedDocumentName = localFileName.Replace(string.Format(".{0}", extension), string.Format(".{0}.{1}", languageCode, extension));

                // Move original file to SharePoint
                var originalFileUrl = _sharePointManagementService.CopyFileToSharePoint(localFileName);

                // Move trnslated file to SharePoint
                var translatedFileUrl = _sharePointManagementService.CopyFileToSharePoint(translatedDocumentName);

                // Delete original file
                if (System.IO.File.Exists(localFileName))
                {
                    System.IO.File.Delete(localFileName);
                }

                // Delete translated file
                if (System.IO.File.Exists(translatedDocumentName))
                {
                    System.IO.File.Delete(translatedDocumentName);
                }

                return new DocumentLinks
                {
                    OriginalDocument = originalFileUrl,
                    TranslatedDocument = translatedFileUrl
                };

            }
            catch (Exception ex)
            {
                _loggingService.Error("Error in DocumentManagementService.TranslateFile", ex);
                throw ex;
            }
        }
    }
}
