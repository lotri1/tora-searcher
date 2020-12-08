using System.Collections.Generic;
using ToraSearcher.UI.ViewModels;

namespace ToraSearcher.UI.FileExporters
{
    public interface IFileExporter
    {
        void ExportResults(string fileName, IEnumerable<SentenceResultVM> sentenceResults, IEnumerable<string> selectedWords);
    }
}