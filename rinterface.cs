using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wxreader
{
    public interface IProgressReporter
    {
        void ReportProgress(int percent);
    }
}
