namespace Party.Shared.Models
{
    public struct ScanLocalFilesProgress
    {
        public Progress Scripts { get; set; }
        public Progress Scenes { get; set; }

        public int Percentage()
        {
            int toAnalyze = Scripts.ToAnalyze + Scenes.ToAnalyze;
            if (toAnalyze == 0)
                return 0;
            int analyzed = Scripts.Analyzed + Scenes.Analyzed;
            return (int)(analyzed / (double)toAnalyze * 100);
        }
    }

    public struct Progress
    {
        public int Analyzed { get; set; }
        public int ToAnalyze { get; set; }

        public Progress(int toAnalyze, int analyzed)
        {
            Analyzed = analyzed;
            ToAnalyze = toAnalyze;
        }
    }
}
