using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;

namespace Pomodairo
{
    public class TaskForegroundConverter :
        IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Brush))
                return null;

            TaskItem task = value as TaskItem;
            if (task == null)
                return null;

            return (task.TaskTimeUsage > task.TaskTimeEdit) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Color.FromRgb(87, 79, 79));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
