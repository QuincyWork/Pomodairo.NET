using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;

namespace Pomodairo
{
    public class TaskClassConverter :
        IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if (targetType != typeof(string))
            //    return null;

            //TaskItem task = (TaskItem)value;
            //return string.Format("位置: {0}\\{1}\\{2}",task.NoteBookName, task.SectionName, task.PageName);
            if (targetType != typeof(Brush))
                return null;

            TaskItem task = value as TaskItem;
            if (task == null)
                return null;

            if (task.TaskClass.Name == "重要紧急")
            {
                return new SolidColorBrush(Colors.DarkRed);
            }
            else if (task.TaskClass.Name == "重要不紧急")
            {
                return new SolidColorBrush(Colors.DarkBlue);
            }
            else if (task.TaskClass.Name == "紧急不重要")
            {
                return new SolidColorBrush(Colors.DarkOrange);
            }
            else
            {
                return new SolidColorBrush(Colors.DarkSeaGreen);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
