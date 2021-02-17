using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoRuns
{
    /// <summary>
    /// 所有记录类的基类
    /// </summary>
    public class Record
    {
        public string Entry { get; set; }
        public string Description { get; set; }

        public string Publisher { get; set; }
        public string ImagePath { get; set; }
        public DateTime Timestamp { get; set; }
        
        
    }
}