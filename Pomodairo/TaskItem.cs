using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pomodairo
{
    class TaskItem
    {
        public TaskItem()
        {
            TaskTimeEdit = 0;
            TaskTimeUsage = 0;
            TaskTimeUnPlan = 0;
            TaskTimeInterrupt = 0;
            TaskComplete = false;
        }

        public string   NoteBookId { get; set; }
        public string   NoteBookName { get; set; }
        public string   SectionId { get; set; }
        public string   SectionName { get; set; }
        public string   PageId { get; set; }
        public string   PageName { get; set; }
        public string   TaskOneNoteId { get; set; }
        public string   TaskName { get; set; }
        public int      TaskTimeEdit { get; set; }
        public int      TaskTimeUsage { get; set; }
        public int      TaskTimeUnPlan { get; set; }
        public int      TaskTimeInterrupt { get; set; }
        public string   TaskComment { get; set; } 
        public bool     TaskComplete { get; set; }
        public TaskItemClass   TaskClass { get; set; }
    }
}
