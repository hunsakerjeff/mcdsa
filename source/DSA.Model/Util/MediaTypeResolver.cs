using DSA.Model.Dto;
using System;
using System.Collections.Generic;

namespace DSA.Model.Util
{
    /// <summary>
    /// Media tyes helper
    /// </summary>
    public static class MediaTypeResolver
    {
        public static Dictionary<string, MediaType> MediaDictionary = new Dictionary<string, MediaType>
        {
            {"MP4", MediaType.MP4 },
            {"MOV", MediaType.Video },
            {"MPEG", MediaType.Video },
            {"MPG", MediaType.Video },
            {"AVI", MediaType.Video },
            {"ZIP", MediaType.Html },
            {"AI", MediaType.AI },
            {"MP3", MediaType.Audio },
            {"M4A", MediaType.Audio },
            {"WAV", MediaType.Audio },
            {"CSV", MediaType.Csv },
            {"EPS", MediaType.Esp },
            {"XLS", MediaType.Excel },
            {"XLSX", MediaType.Excel },
            {"HTML", MediaType.HtmlFile },
            {"JPG", MediaType.Image },
            {"JPEG", MediaType.Image },
            {"PNG", MediaType.Image },
            {"GIF", MediaType.Image },
            {"TIFF", MediaType.Image },
            {"KEY", MediaType.Keynote },
            {"KEYNOTE", MediaType.Keynote },
            {"PAGES", MediaType.Pages },
            {"PDF", MediaType.PDF },
            {"PPT", MediaType.PowerPoint },
            {"PPTX", MediaType.PowerPoint },
            {"POWER_POINT_X", MediaType.PowerPoint },// created in ios
            {"PSD", MediaType.PSD },
            {"RTF", MediaType.Rtf },
            {"TXT", MediaType.Txt },
            {"VSD", MediaType.Visio },
            {"VDX", MediaType.Visio },
            {"DOC", MediaType.Word },
            {"DOCX", MediaType.Word },
            {"WORD_X", MediaType.Word },// created in ios
            {"XML", MediaType.Xml },
            {"LINK", MediaType.Url },
        };

        public static MediaType ResolveType(string stringType, string fileName )
        {
            var fileExtension = GetFileExtension(fileName);
            return MediaDictionary.ContainsKey(stringType)
                        ? MediaDictionary[stringType]
                        : MediaDictionary.ContainsKey(fileExtension)
                            ? MediaDictionary[fileExtension]
                            : MediaType.Unknow;
        }

        private static string GetFileExtension(string fileName)
        {
            if( string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }
            var split = fileName.Split( new[]{ '.' }, StringSplitOptions.RemoveEmptyEntries);
            if(split.Length != 2)
            {
                return string.Empty;
            }
            return split[1].ToUpper();
        }
    }
}
