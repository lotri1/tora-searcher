using System.Collections.Generic;
using ToraSearcher.UI.ViewModels;

namespace ToraSearcher.UI.FileExporters
{
    public interface IFileExporter
    {
        void ExportResults(IEnumerable<SentenceResultVM> sentenceResults, IEnumerable<string> selectedWords);
    }
}