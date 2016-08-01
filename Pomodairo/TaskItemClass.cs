using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pomodairo
{
    class TaskItemClass
    {
        public TaskItemClass()
        {
            Index = -1;
        }

        public int Index { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
    }
}
