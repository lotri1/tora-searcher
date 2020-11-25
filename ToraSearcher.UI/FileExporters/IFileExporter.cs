using System.Collections.Generic;
using ToraSearcher.UI.ViewModels;

namespace ToraSearcher.UI.FileExporters
{
    public interface IFileExporter
    {
        void ExportSentenceResults(IEnumerable<SentenceResultVM> sentenceResults);
    }
}