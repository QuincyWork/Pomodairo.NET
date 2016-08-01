using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;

namespace Pomodairo
{
    public class TaskToolTipConverter :
        IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Object))
                return null;

            TaskItem task = value as TaskItem;
            if (task == null)
                return null;

            return string.Format("位置: {0}\\{1}\\{2}", task.NoteBookName, task.SectionName, task.PageName);            
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
